using MelonLoader;
using Mono.Cecil;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;

[assembly: MelonInfo(typeof(GithubModUpdater.Updater), "Github Mod Updater", "1.0", "Plague")]
[assembly: MelonGame]

namespace GithubModUpdater
{
    public class Updater : MelonPlugin
    {
        private WebClient client = new WebClient();

        public override void OnApplicationStart()
        {
            var ModFiles = Directory.GetFiles(Environment.CurrentDirectory + "\\Mods", "*.dll").ToList();
            ModFiles.AddRange(Directory.GetFiles(Environment.CurrentDirectory + "\\Plugins", "*.dll"));

            var AllInfos = ModFiles.Select(o => (o, GetMelonInfo(o))).Where(a => a.Item2 != null);

            foreach (var Info in AllInfos)
            {
                if (!string.IsNullOrWhiteSpace(Info.Item2.DownloadLink) && Info.Item2.DownloadLink.ToLower().Contains("github") && (Info.Item2.DownloadLink.ToLower().EndsWith("dll")))
                {
                    var URL = Info.Item2.DownloadLink;
                    var PathToFile = Info.o;
                    var Name = Info.Item2.Name;

                    var OldHash = SHA256CheckSum(PathToFile);

                    client.DownloadFile(URL, PathToFile);

                    if (SHA256CheckSum(PathToFile) != OldHash)
                    {
                        MelonLogger.Warning($"Updated: {Name} From DownloadLink: {URL}");
                    }
                }
            }
        }

        private string SHA256CheckSum(string filePath)
        {
            using (var hash = SHA256.Create())
            {
                using (FileStream fileStream = File.OpenRead(filePath))
                {
                    return Convert.ToBase64String(hash.ComputeHash(fileStream));
                }
            }
        }

        private MelonInfoAttribute GetMelonInfo(string path)
        {
            try
            {
                using (var asm = AssemblyDefinition.ReadAssembly(path, new ReaderParameters {ReadWrite = true}))
                {
                    var Attrib = asm?.CustomAttributes?.FirstOrDefault(a => a != null && (a.AttributeType.Name == "MelonModInfoAttribute" || a.AttributeType.Name == "MelonInfoAttribute"));

                    if (Attrib == null)
                    {
                        return null;
                    }

                    return new MelonInfoAttribute(null /*Attrib.ConstructorArguments[0].Value as Type*/, Attrib.ConstructorArguments[1].Value as string, Attrib.ConstructorArguments[2].Value as string, Attrib.ConstructorArguments[3].Value as string, Attrib.ConstructorArguments[4].Value as string);
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
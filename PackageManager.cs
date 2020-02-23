using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace twasm
{
    public static class PackageManager
    {
        public static async Task Resolve(string name, string version, string targetFramework, string location)
        {
            await Task.Run(async () => {
                try
                {
                    Logger.Write($"Resolving {name} {version} on {targetFramework} ");
                    var package = GetPackage(name, version);
                    var nuspec = package.GetFile(x => x.Name.EndsWith("nuspec"));
                    package.ExtractFiles(x => x.Name.EndsWith(".dll"), location);
                    Logger.Write($"Resolving {name} {version} on {targetFramework} (Done)", ConsoleColor.Green, true);
                    var dependencies = GetDependencies(nuspec, targetFramework);
                    if (dependencies != null)
                    {
                        foreach (var dep in dependencies)
                        {
                            await Resolve(dep.name, dep.version, targetFramework, location);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Write($"Resolving {name} {version} on {targetFramework} (Failed)", ConsoleColor.Red, true);
                    Logger.Write(ex.Message);
                    return;
                }
            });
        }

        public static string GetTempNugetCache()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), "twasm.nuget.cache\\");
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        public static byte[] GetPackage(string name, string version)
        {
            string filename = Path.Combine(GetTempNugetCache(), $"{name}.{version}.cache");
            if (File.Exists(filename))
            {
                var file = File.ReadAllBytes(filename);
                return file;
            }
            var p = new Uri($"https://www.nuget.org/api/v2/package/{name}/{version}");
            try
            {
                byte[] data = new byte[0];
                using (var webClient = new WebClient())
                {
                    data = webClient.DownloadData(p.ToString());
                }
                File.WriteAllBytes(filename, data);
                return data;
            }
            catch (Exception ex)
            {
                Logger.Write($"Unable to fetch package - {p} - {ex.Message.Take(250).ToString()}");
                return null;
            }
        }

        public static void ExtractFiles(this byte[] self, Func<ZipArchiveEntry, bool> predicate, string location)
        {
            using (var zip = new ZipArchive(new MemoryStream(self), ZipArchiveMode.Read))
            {
                foreach (var entry in zip.Entries.Where(predicate))
                {
                    entry.ExtractToFile(Path.Combine(location, entry.Name), true);
                }
            }
        }

        public static MemoryStream GetFile(this byte[] self, Func<ZipArchiveEntry, bool> predicate)
        {
            using (var zip = new ZipArchive(new MemoryStream(self), ZipArchiveMode.Read))
            {
                var output = new MemoryStream();
                zip.Entries.FirstOrDefault(predicate).Open().CopyTo(output);
                output.Position = 0;
                return output;
            }
        }

        public static IEnumerable<(string name, string version)> GetDependencies(Stream nuspec, string targetFramework)
        {
            XDocument xdoc = XDocument.Load(nuspec);
            var dependencies = xdoc.Root.Descendants().FirstOrDefault(x => x.Name.LocalName == "dependencies");
            if (dependencies == null)
                return null;
            var hasFrameworks = dependencies.Descendants()
                .Where(x => x.Name.LocalName == "group" && x.Attribute("targetFramework").Value.ToLower().Contains("netstandard"))
                .OrderByDescending(x => x.Attribute("targetFramework").Value).FirstOrDefault();
            if (hasFrameworks != null)
            {
                var selectedFramework = hasFrameworks.Descendants().Select(x => (name: x.Attribute("id").Value, version: x.Attribute("version").Value));
                return selectedFramework;
            }
            var globalDependency = dependencies.Descendants().Select(x => (name: x.Attribute("id").Value, version: x.Attribute("version").Value));
            return globalDependency;
        }
    }
}

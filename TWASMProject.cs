using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace twasm
{
    public class TWASMProject
    {
        public string Name { get; set; }
        public List<string> Content { get; set; }
        public List<(string name, string version)> Dependencies { get; set; }
        public List<string> Sources { get; set; }
        public List<string> Exposed { get; set; } = new List<string> { "ScriptAccess" };

        internal async Task<List<PackageResolvedInformation>> ResolveDependencies(string dir)
        {
            Logger.Write("Resolving Dependencies ...");
            List<PackageResolvedInformation> Result = new List<PackageResolvedInformation>();
            foreach (var (name, version) in Dependencies)
            {
                Result.AddRange(await PackageManager.Resolve(name, version, "netstandard2.0", dir));
            }
            return Result.Where(x => Dependencies.Any(d => x.Name == d.name && x.Version == d.version)).ToList();
        }
    }
}

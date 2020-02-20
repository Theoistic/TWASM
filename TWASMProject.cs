using System.Collections.Generic;

namespace twasm
{
    public class TWASMProject
    {
        public string Name { get; set; }
        public List<string> Content { get; set; }
        public List<(string name, string version)> Dependencies { get; set; }
        public List<string> Sources { get; set; }
        public List<string> Exposed { get; set; } = new List<string> { "ScriptAccess" };
    }
}

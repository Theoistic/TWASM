using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace twasm
{

    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                await NCMD.Parse(args);
            } catch(Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }

        [CMD]
        public static TWASMProject Convert(string csproj)
        {
            string output = $"{Path.GetFileNameWithoutExtension(csproj)}";
            string projectDirectory = Path.GetFullPath(csproj).Replace(Path.GetFileName(csproj), "");

            List<string> sources = new List<string>();
            List<string> content = new List<string>();
            List<(string, string)> dependencies = new List<(string, string)>();

            XDocument xdoc = XDocument.Load($"{csproj}");
            var Items = xdoc.Descendants("Project").Elements("ItemGroup").SelectMany(x => x.Elements());

            sources = Directory.GetFiles(projectDirectory, "*.cs", SearchOption.AllDirectories).Where(x => !x.Contains("\\bin\\") && !x.Contains("\\obj\\")).ToList();

            foreach (var item in Items)
            {
                switch (item.Name.ToString().ToLower())
                {
                    case "content": content.Add(item.Attribute("Include").Value); break;
                    case "packagereference": dependencies.Add((item.Attribute("Include").Value, item.Attribute("Version").Value)); break;
                    default: break;
                }
            }
            TWASMProject project = new TWASMProject
            {
                Name = output,
                Sources = sources,
                Content = content,
                Dependencies = dependencies
            };
            File.WriteAllText(project.Name+".twasm", JsonConvert.SerializeObject(project, Newtonsoft.Json.Formatting.Indented));
            return project;
        }

        [CMD]
        public static async Task Compile(string proj = "")
        {
            if(string.IsNullOrEmpty(proj))
            {
                proj = Directory.GetFiles(Environment.CurrentDirectory, "*.twasm").FirstOrDefault();
                if(string.IsNullOrEmpty(proj))
                {
                    proj = Directory.GetFiles(Environment.CurrentDirectory, "*.csproj").FirstOrDefault();
                }
                if (string.IsNullOrEmpty(proj))
                {
                    Logger.Error("Unable to find a project in the current directory.");
                }
            }
            TWASMProject project = proj.ToLower().EndsWith(".csproj") ? Convert(proj) : JsonConvert.DeserializeObject<TWASMProject>(File.ReadAllText(proj));
            string directory = Environment.CurrentDirectory;

            var buildOutput = Path.Combine(directory, "bin\\twasm\\");
            if(!Directory.Exists(buildOutput))
            {
                Directory.CreateDirectory(buildOutput);
            }
            foreach(var cnt in project.Content)
            {
                File.Copy(Path.Combine(directory, cnt), Path.Combine(buildOutput, cnt), true);
            }


            var compiler = new Compiler(Path.Combine(buildOutput, "managed\\"));
            var CompilationErrors = await compiler.Compile(project);
            foreach(var err in CompilationErrors)
            {
                Logger.Error(err.Message);
            }

            // expose the classes to JS
            JSExpose JSE = new JSExpose(project, Path.Combine(buildOutput, "managed\\", $"{project.Name}.dll"));

            if (!Directory.Exists(Path.Combine(buildOutput, "managed\\")))
            {
                Directory.CreateDirectory(Path.Combine(buildOutput, "managed\\"));
            }
            using (var stream = Utils.ReadResourceStream("Lib.dotnet.wasm"))
            {
                File.WriteAllBytes(Path.Combine(buildOutput, "dotnet.wasm"), stream.ToArray());
            }
            StringBuilder js = new StringBuilder();
            js.Append(JSE.src.ToString());
            js.Append(Utils.ReadResource("Lib.mono-config.js"));
            js.Append(Utils.ReadResource("Lib.runtime.js"));
            js.Append(Utils.ReadResource("Lib.dotnet.js"));
            File.WriteAllText(Path.Combine(buildOutput, "twasm.js"), js.ToString());

            Serve(Path.Combine(buildOutput, ""));
        }


        [CMD]
        public static void Serve(string dir = "")
        {
            if(string.IsNullOrEmpty(dir))
            {
                string gp = Path.Combine(Environment.CurrentDirectory, "bin\\twasm\\");
                if (Directory.Exists(gp))
                {
                    dir = gp;
                }
            }
            SimpleHTTPServer myServer = new SimpleHTTPServer(dir, 8080);
            var processes = Process.GetProcessesByName("Chrome");
            if (processes != null)
            {
                var path = processes.FirstOrDefault()?.MainModule?.FileName;
                Process.Start(path, "http://localhost:8080/");
            }
            Console.Read();
            myServer.Stop();
        }

        [CMD]
        public static void New(string name = "")
        {
            if (string.IsNullOrEmpty(name))
                name = "Example";
            TWASMProject project = new TWASMProject
            {
                Name = name,
                Sources = new List<string> { "ScriptAccess.cs" },
                Content = new List<string> { "index.html", "app.js" },
                Dependencies = new List<(string name, string version)>()
            };
            File.WriteAllText(project.Name + ".twasm", JsonConvert.SerializeObject(project));
            File.WriteAllText("ScriptAccess.cs", Utils.ReadResource("Template.ScriptAccess.cs"));
            File.WriteAllText("index.html", Utils.ReadResource("Template.index.html"));
            File.WriteAllText("app.js", Utils.ReadResource("Template.app.js"));
        }

        
    }
}

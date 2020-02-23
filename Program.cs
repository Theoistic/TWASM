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
                Logger.Error("Exiting..");
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

            foreach (var (name, version) in project.Dependencies)
            {
                await PackageManager.Resolve(name, version, "netstandard2.0", buildOutput);
            }
            foreach(var cnt in project.Content)
            {
                File.Copy(Path.Combine(directory, cnt), Path.Combine(buildOutput, cnt), true);
            }

            var depFiles = Directory.GetFiles(buildOutput, "*.dll").Where(x => !x.Contains(project.Name));

            // build the main module
            StringBuilder compileScript = new StringBuilder();
            compileScript.AppendLine("$WASM_SDK=\"C:\\mono-wasm-sdk\\\" ");
            compileScript.AppendLine($"$TWASMBuildPath=\"{buildOutput}\" ");
            compileScript.AppendLine($"$TWASMSOURCEROOT=\"{directory}\"");
            compileScript.AppendLine($"csc /target:library -out:{buildOutput}{project.Name}.dll /noconfig /nostdlib " +
                $"{string.Join(" ", depFiles.Select(x => "/r:"+x.Replace(buildOutput, "").Replace("\\", "/")))} " +
                $"/r:$WASM_SDK/wasm-bcl/wasm/mscorlib.dll " +
                $"/r:$WASM_SDK/wasm-bcl/wasm/System.dll " +
                $"/r:$WASM_SDK/wasm-bcl/wasm/System.Core.dll " +
                $"/r:$WASM_SDK/wasm-bcl/wasm/Facades/netstandard.dll " +
                $"/r:$WASM_SDK/wasm-bcl/wasm/System.Net.Http.dll " +
                $"/r:$WASM_SDK/framework/WebAssembly.Bindings.dll " +
                $"/r:$WASM_SDK/framework/WebAssembly.Net.Http.dll " +
                $"{string.Join(" ", project.Sources.Select(x => "$TWASMSOURCEROOT\\" + x.Replace(directory, "")))} ");
            Logger.Write(Utils.RunScript(compileScript.ToString()));

            // expose the classes to JS
            JSExpose JSE = new JSExpose(project, $"{buildOutput}{project.Name}.dll");
            File.WriteAllText($"{buildOutput}twasm.js", JSE.src.ToString());
            project.Content.Add($"twasm.js");

            // build and publish the WASM
            compileScript = new StringBuilder();
            compileScript.AppendLine("$WASM_SDK=\"C:\\mono-wasm-sdk\\\" ");
            compileScript.AppendLine($"$TWASMBuildPath=\"{buildOutput}\" ");
            compileScript.AppendLine($"mono $WASM_SDK/packager.exe --copy=always --out=$TWASMBuildPath/publish {string.Join(" ", project.Content.Select(x => "--asset=$TWASMBuildPath/" + x))} $TWASMBuildPath/{project.Name}.dll");
            Logger.Write(Utils.RunScript(compileScript.ToString()));

            Console.WriteLine($"mono $WASM_SDK/packager.exe --copy=always --out=$TWASMBuildPath/publish {string.Join(" ", project.Content.Select(x => "--asset=$TWASMBuildPath/" + x))} $TWASMBuildPath/{project.Name}.dll");

            StringBuilder bundle = new StringBuilder();
            bundle.Append(File.ReadAllText($"{buildOutput}\\publish\\twasm.js"));
            bundle.Append(File.ReadAllText($"{buildOutput}\\publish\\mono-config.js"));
            bundle.Append(File.ReadAllText($"{buildOutput}\\publish\\runtime.js"));
            bundle.Append(File.ReadAllText($"{buildOutput}\\publish\\dotnet.js"));
            File.WriteAllText($"{buildOutput}\\publish\\twasm.js", bundle.ToString());
            File.Delete($"{buildOutput}\\publish\\mono-config.js");
            File.Delete($"{buildOutput}\\publish\\runtime.js");
            File.Delete($"{buildOutput}\\publish\\dotnet.js");

            if(!project.Content.Any(x => x.ToLower().Contains(".html") && x.ToLower().Contains(".js")))
            {
                File.WriteAllText($"{buildOutput}\\publish\\index.html", Utils.ReadResource("Fallback.index.html"));
                File.WriteAllText($"{buildOutput}\\publish\\app.js", Utils.ReadResource("Fallback.app.js"));
            }

            Serve(Path.Combine(buildOutput, "publish"));
        }

        [CMD]
        public static void Serve(string dir = "")
        {
            if(string.IsNullOrEmpty(dir))
            {
                string gp = Path.Combine(Environment.CurrentDirectory, "bin\\twasm\\publish\\");
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
            File.WriteAllText("ScriptAccess.cs", Utils.ReadResource("ScriptAccess.cs"));
            File.WriteAllText("index.html", Utils.ReadResource("index.html"));
            File.WriteAllText("app.js", Utils.ReadResource("app.js"));
        }

        
    }
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace twasm
{
    public enum CodeGenType { Library, Executable }

    public class CompilationError
    {
        public string Type { get; set; }
        public string Message { get; set; }
    }

    public class Compiler
    {


        public IEnumerable<string> BaseLibraries = new string[]
        {
            "WebAssembly.Net.Http.dll",
            "WebAssembly.Bindings.dll",
            "WebAssembly.Net.WebSockets.dll",
            "mscorlib.dll",
            "netstandard.dll",
            "Mono.Security.dll",
            "System.dll",
            "System.Core.dll",
            "System.Xml.dll",
            "System.Xml.Linq.dll",
            "System.Numerics.dll",
            "System.Memory.dll",
            "System.Data.dll",
            "System.Transactions.dll",
            "System.Data.DataSetExtensions.dll",
            "System.Drawing.Common.dll",
            "System.IO.Compression.dll",
            "System.IO.Compression.FileSystem.dll",
            "System.ComponentModel.Composition.dll",
            "System.Net.Http.dll",
            "System.Runtime.Serialization.dll",
            "System.ServiceModel.Internals.dll",
            
        };

        public SyntaxTree Parse(string filename, CSharpParseOptions options = null)
        {
            var stringText = SourceText.From(File.ReadAllText(filename), Encoding.Default);
            return SyntaxFactory.ParseSyntaxTree(stringText, options, filename);
        }

        public string OutputDirectory { get; set; }

        public Compiler(string output)
        {
            OutputDirectory = output;
        }

        public async Task<IEnumerable<CompilationError>> Compile(TWASMProject proj, bool emitpdb = false)
        {
            Directory.CreateDirectory(OutputDirectory);

            // resolve dependencies
            var resolved = await proj.ResolveDependencies(OutputDirectory);

            foreach (var lib in Utils.Libraries)
            {
                using (var stream = Utils.GetLibrary(lib))
                {
                    File.WriteAllBytes(Path.Combine(OutputDirectory, lib), stream.ToArray());
                }
            }

            var refs = resolved.SelectMany(x => x.Files).Select(x => MetadataReference.CreateFromFile(x)).ToList();
            foreach (var lib in Utils.Libraries)
            {
                refs.Add(MetadataReference.CreateFromFile(Path.Combine(OutputDirectory, lib)));
            }

            CSharpCompilationOptions DefaultCompilationOptions =
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                    .WithGeneralDiagnosticOption(ReportDiagnostic.Suppress)
                    .WithOptimizationLevel(OptimizationLevel.Release);

            List<SyntaxTree> syntaxTrees = proj.Sources.Select(x => Parse(x, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest))).ToList();
            var compilation = CSharpCompilation.Create(proj.Name, syntaxTrees, refs, DefaultCompilationOptions);

            Logger.Write("Compiling Project ...", ConsoleColor.Yellow);
            try
            {
                var result = compilation.Emit(Path.Combine(OutputDirectory, $"{proj.Name}.dll"), emitpdb ? Path.Combine(OutputDirectory, $"{proj.Name}.pdb") : null);
                Logger.Write("Compiling Project (Done)", ConsoleColor.Green, true);
                return result.Diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error).Select(x => new CompilationError { Type = "Error", Message = x.GetMessage() });
            }
            catch (Exception ex)
            {
                return new List<CompilationError> {
                    new CompilationError
                    {
                        Type = "Error",
                        Message = ex.Message
                    },
                    new CompilationError {
                        Type = "Fatal",
                        Message = "Unable to run the compilation. CODE #7"
                    }
                };
            }
        }
    }
}

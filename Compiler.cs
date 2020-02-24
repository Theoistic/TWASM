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

            var refs = resolved.SelectMany(x => x.Files).Select(x => MetadataReference.CreateFromFile(x)).ToList();
            foreach (var lib in new string[] { "Lib.mscorlib.dll", "Lib.netstandard.dll", 
                "Lib.System.Core.dll", "Lib.System.dll", "Lib.System.Net.Http.dll", "Lib.WebAssembly.Bindings.dll", "Lib.WebAssembly.Net.Http.dll" })
            {
                refs.Add(MetadataReference.CreateFromStream(Utils.ReadResourceStream(lib)));
            }

            CSharpCompilationOptions DefaultCompilationOptions =
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                    .WithGeneralDiagnosticOption(ReportDiagnostic.Suppress)
                    .WithOptimizationLevel(OptimizationLevel.Release);

            List<SyntaxTree> syntaxTrees = proj.Sources.Select(x => Parse(x, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest))).ToList();
            var compilation = CSharpCompilation.Create(proj.Name, syntaxTrees, refs, DefaultCompilationOptions);

            try
            {
                var result = compilation.Emit(Path.Combine(OutputDirectory, $"{proj.Name}.dll"), emitpdb ? Path.Combine(OutputDirectory, $"{proj.Name}.pdb") : null);
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Security.Principal;
using System.Management.Automation.Runspaces;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Diagnostics;

namespace twasm
{
    public static class Utils
    {
        public static string ReadResource(string file)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"twasm.Files.{file}";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        public static MemoryStream ReadResourceStream(string file)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"twasm.Files.{file}";
            MemoryStream memoryStream = new MemoryStream();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null) return null;
                byte[] ba = new byte[stream.Length];
                stream.Read(ba, 0, ba.Length);
                MemoryStream ms = new MemoryStream(ba);
                ms.Position = 0;
                return ms;
            }
        }

        public static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public static string RunScript(string scriptText)
        {
            Runspace runspace = RunspaceFactory.CreateRunspace();
            runspace.Open();
            Pipeline pipeline = runspace.CreatePipeline();
            pipeline.Commands.AddScript(scriptText);
            pipeline.Commands.Add("Out-String");
            Collection<PSObject> results = pipeline.Invoke();
            runspace.Close();
            StringBuilder stringBuilder = new StringBuilder();
            foreach (PSObject obj in results)
            {
                stringBuilder.AppendLine(obj.ToString());
            }
            return stringBuilder.ToString();
        }

        public static bool IsUserAdministrator
        {
            get
            {
                bool isAdmin;
                try
                {
                    WindowsIdentity user = WindowsIdentity.GetCurrent();
                    WindowsPrincipal principal = new WindowsPrincipal(user);
                    isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
                catch (UnauthorizedAccessException ex)
                {
                    isAdmin = false;
                }
                catch (Exception ex)
                {
                    isAdmin = false;
                }
                return isAdmin;
            }
        }

        public static void RestartAsAdmin()
        {
            var startInfo = new ProcessStartInfo("twasm.exe") { Verb = "runas", Arguments = string.Join(" ", Environment.GetCommandLineArgs()) };
            Process.Start(startInfo);
            Environment.Exit(0);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

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
    }
}

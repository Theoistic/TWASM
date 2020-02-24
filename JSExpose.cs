using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace twasm
{
    public class JSExpose
    {
        public StringBuilder src = new StringBuilder();

        public JSExpose(TWASMProject project, string asmfile)
        {
            try
            {
                Assembly _asm = Assembly.UnsafeLoadFrom(asmfile);
                List<Type> _types = _asm.GetTypes().Where(x => project.Exposed.Select(t => t.ToLower()).Contains(x.Name.ToLower())).ToList();
                src.AppendLine("var App = {");
                src.AppendLine("    Ready: false,");
                src.AppendLine("    init: function() {");
                foreach (var t in _types)
                {
                    var methods = t.GetMethods().Where(x => x.IsStatic);
                    src.AppendLine($"        {t.Name} = {{}};");
                    foreach (var m in methods)
                    {
                        src.AppendLine($"        {t.Name}.{m.Name} = Module.mono_bind_static_method(\"[{project.Name}] {t.Name}:{m.Name}\");");
                    }
                }
                src.AppendLine("        App.Ready = true;");
                src.AppendLine("        document.dispatchEvent(new CustomEvent(\"TWASMReady\", { }));");
                src.AppendLine("    }");
                src.AppendLine("};");
            } catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }
    }
}

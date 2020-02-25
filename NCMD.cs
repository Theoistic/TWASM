﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace twasm
{
    public static class NCMD
    {
        public static async Task Parse(params string[] args)
        {
            if (args.Length == 0)
                throw new ArgumentException("No parameters given.");

            var methods = typeof(NCMD).GetTypeInfo().Assembly.GetTypes().SelectMany(t => t.GetMethods()).Where(m => m.GetCustomAttributes(typeof(CMD), false).Length > 0).ToArray();
            MethodInfo func = methods.FirstOrDefault(x => x.Name.ToLower() == args[0].ToLower());
            if (func == null) throw new ArgumentException($"Command {args[0]} not found.");
            List<object> newArgs = null;
            if (func.GetParameters().Any())
            {
                newArgs = new List<object>();
                int requiredParameters = func.GetParameters().Where(x => x.HasDefaultValue == false).Count();
                IEnumerable<string> GetArgument(string option) => args.SkipWhile(i => i.Substring(1, i.Length - 1).ToLower() != option.ToLower()).Skip(1).SelectArgChunks();
                foreach (var p in func.GetParameters())
                {
                    var paramValue = GetArgument(p.Name);
                    if (paramValue == null && p.DefaultValue != null)
                    {
                        throw new ArgumentException($"Parameter '{p.Name}' is required.");
                    }
                    else if (p.ParameterType.IsArray)
                    {

                        newArgs.Add(paramValue.ToArray());
                    }
                    else
                    {
                        newArgs.Add(paramValue.FirstOrDefault());
                    }
                }
            }
            if (func.ReturnType == typeof(Task))
            {
                await (Task)func.Invoke(null, newArgs?.ToArray());
            }
            else
            {
                await Task.Run(() => func.Invoke(null, newArgs?.ToArray()));
            }
        }

        public static IEnumerable<string> SelectArgChunks(this IEnumerable<string> source)
        {
            foreach (var item in source)
            {
                if (item.StartsWith("-"))
                {
                    break;
                }
                else
                {
                    yield return item;
                }
            }
        }
    }

    public class CMD : Attribute
    {
        public string Alt { get; set; }

        public CMD(string Alt = "")
        {
            this.Alt = Alt;
        }
    }
}

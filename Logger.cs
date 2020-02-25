using System;

namespace twasm
{
    public static class Logger
    {
        public static void Write(string txt, ConsoleColor color = ConsoleColor.White, bool @override = false)
        {
            var oc = Console.ForegroundColor;
            Console.ForegroundColor = color;
            if (@override)
            {
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                Console.Write(new String(' ', 100));
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.WriteLine(txt);
            }
            else
            {
                Console.WriteLine(txt);
            }
            Console.ForegroundColor = oc;
        }

        public static void Error(string txt) => Write(txt, ConsoleColor.DarkRed);
    }
}

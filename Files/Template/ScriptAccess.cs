using System;

public class ScriptAccess
{
    public static string Hello(string str)
    {
        return $"Hello {str} this message came from .NET at {DateTime.Now} via TWASM.";
    }
}


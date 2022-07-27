using System;
using System.Text.RegularExpressions;

namespace MagicShapeMultiply
{
    public static class TypeUtils
    {
        public static string ReplaceClassName(string str)
        {
            return Regex.Replace(str, "(?<!{){([^{}]+)}(?!})", m =>  GetType(m.Value.Substring(1, m.Value.Length - 2))?.AssemblyQualifiedName);
        }

        public static Type GetType(string name)
        {
            for (int i = AppDomain.CurrentDomain.GetAssemblies().Length - 1; i >= 0; i--)
            {
                Type type = AppDomain.CurrentDomain.GetAssemblies()[i].GetType(name);
                if (type != null)
                    return type;
            }
            return null;
        }
    }
}

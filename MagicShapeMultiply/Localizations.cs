using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace MagicShapeMultiply
{
    public static class Localizations
    {
        private static readonly Dictionary<SystemLanguage, Dictionary<string, string>> localizations = new Dictionary<SystemLanguage, Dictionary<string, string>>();

        public static void Load()
        {
            IEnumerable<string> languages = Enum.GetNames(typeof(SystemLanguage)).Where(l => l != "Unknown").Select(n => n.ToLower());
            localizations.Clear();
            string path = Path.Combine(Main.ModEntry.Path, "localizations");
            string[] files = Directory.GetFiles(path, "*.loc");
            foreach (string file in files)
            {
                string name = Path.GetFileNameWithoutExtension(file);
                if (!Enum.TryParse(name, true, out SystemLanguage language) || language == SystemLanguage.Unknown)
                {
                    Main.Logger.Log($"Unknown language '{name}'! (file:{file})");
                    continue;
                }
                Dictionary<string, string> dict = localizations.TryGetValue(language, out var d) ? d : new Dictionary<string, string>();
                string text = File.ReadAllText(file);
                int i = 0;
                foreach (string line in text.Split('\n'))
                {
                    i++;
                    string trimmedLine = line?.Trim().Replace("\\n", "\n");
                    if (trimmedLine.IsNullOrEmpty())
                        continue;
                    if (trimmedLine.StartsWith("//"))
                        continue;
                    int split = trimmedLine.IndexOf(':');
                    if (split == -1)
                    {
                        Main.Logger.Log($"Wrong line in localization file! (line:{i}, file:{file})");
                        continue;
                    }
                    string key = trimmedLine.Substring(0, split).Trim();
                    if (dict.ContainsKey(key))
                        Main.Logger.Log($"Duplicate key '{key}' in localization file. (line:{i}, file:{file})");
                    string value = "";
                    if (split + 1 >= trimmedLine.Length)
                        Main.Logger.Log($"Empty value in localization file. (line:{i}, file:{file})");
                    else
                        value = trimmedLine.Substring(split + 1);
                    dict.Add(key.ReplaceClassName(), value.Trim().ReplaceClassName());
                }
                localizations.Remove(language);
                localizations.Add(language, dict);
            }
        }

        public static bool GetString(string key, out string result, Dictionary<string, object> parameters = null)
        {
            if ((localizations.TryGetValue(RDString.language, out var dict)
                || localizations.TryGetValue(SystemLanguage.English, out dict)
                || localizations.TryGetValue(localizations.Keys.First(), out dict))
                && dict.TryGetValue(key, out string value))
            {
                result = parameters == null ? value : RDString.ReplaceParameters(value, parameters);
                return true;
            }
            result = key;
            return false;
        }

        public static void SetLocalizedText(this Text text)
        {
            if (GetString(text.text, out string result))
                text.text = result;
        }

        private static string ReplaceClassName(this string str)
            => Regex.Replace(str, "(?<!{){([^{}]+)}(?!})", m => GetType(m.Value.Substring(1, m.Value.Length - 2))?.AssemblyQualifiedName);

        private static Type GetType(string name)
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

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using TinyJson;
using UnityEngine;
using UnityModManagerNet;

namespace MagicShapeMultiply
{
    public static class Startup
    {
        public static void Load(UnityModManager.ModEntry modEntry)
        {
            if (CheckRequirements(modEntry))
                AccessTools.Method($"{typeof(Startup).Namespace}.Main:Setup").Invoke(null, new object[] { modEntry });
        }

        public static void LoadAssembly(string path)
        {
            using (FileStream stream = new FileStream(path, FileMode.Open))
            {
                byte[] data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);
                AppDomain.CurrentDomain.Load(data);
            }
        }

        public static bool CheckRequirements(UnityModManager.ModEntry modEntry)
        {
            Dictionary<string, Version> installeds = new Dictionary<string, Version>();
            foreach (string path in Directory.GetDirectories(UnityModManager.modsPath))
            {
                string info = FindInfo(path);
                if (info == null)
                    continue;
                UnityModManager.ModInfo modInfo = File.ReadAllText(info).FromJson<UnityModManager.ModInfo>();
                installeds.Add(modInfo.Id, UnityModManager.ParseVersion(modInfo.Version));
            }

            string[][] requirements = modEntry.LoadAfter
                .Where(_ => _.StartsWith("R:"))
                .Select(_ => _.Substring(_.IndexOf(':') + 1).Split('/'))
                .Where(_ => !installeds.ContainsKey(_[1]) || (_.Length == 3 && (installeds[_[1]] < UnityModManager.ParseVersion(_[2]))))
                .ToArray();
            if (requirements.Length == 0)
                return true;

            if (!UnityModManager.HasNetworkConnection())
            {
                modEntry.OnGUI = _ =>
                {
                    GUILayout.Label("Some required mods are missing!");
                    GUILayout.Label("Please restart the game in network environment, then they'll be automatically downloaded.");
                    GUILayout.Label("필요한 모드들이 설치되지 않았습니다!");
                    GUILayout.Label("네트워크 환경에서 게임을 재시작하시면 자동으로 설치될겁니다.");
                    GUILayout.Space(20);

                    GUILayout.Label("Those are missing required mods:");
                    GUILayout.Label("다음은 필요한 모드 목록입니다:");

                    GUILayout.BeginHorizontal();
                    GUILayout.Space(10);
                    GUILayout.BeginVertical();
                    foreach (string[] requirement in requirements)
                    {
                        GUILayout.Label($"{requirement[1]} <color=grey>by</color> {requirement[0]}");
                    }
                    GUILayout.EndVertical();
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                };
                modEntry.OnToggle = null;
                modEntry.OnSaveGUI = null;
                modEntry.OnUpdate = null;
                modEntry.OnLateUpdate = null;
                modEntry.OnFixedUpdate = null;
                modEntry.OnFixedGUI = null;
                modEntry.Info.DisplayName = $"<color=red>{modEntry.Info.DisplayName}</color> [ERROR]";
                return false;
            }

            List<UnityModManager.ModEntry> loadeds = new List<UnityModManager.ModEntry>();
            List<string[]> faileds = new List<string[]>();
            bool restart = false;
            for (int i = 0; i < requirements.Length; i++)
            {
                string author = requirements[i][0];
                string id = requirements[i][1];

                try
                {
                    modEntry.Logger.Log($"Begin downloading {id} by {author}");

                    string zipPath;
                    using (var client = new WebClient())
                    {

                        string data = client.DownloadString(new Uri($"https://raw.githubusercontent.com/{author}/{id}/main/Repository.json"));
                        string url = data.FromJson<UnityModManager.Repository>().Releases[0].DownloadUrl;
                        modEntry.Logger.Log($"Found latest release: {url}");

                        zipPath = Path.Combine(UnityModManager.modsPath, id + ".adofaimod");
                        client.DownloadFile(url, zipPath);
                        modEntry.Logger.Log($"Downloaded mod file at: {zipPath}");
                    }

                    string modPath = UnzipMod(zipPath, id);
                    modEntry.Logger.Log($"Extracted mod files at: {modPath}");
                    File.Delete(zipPath);

                    if (installeds.ContainsKey(id))
                    {
                        restart = true;
                        continue;
                    }

                    string infoPath = FindInfo(modPath);
                    if (infoPath == null)
                        throw new Exception($"Wrong mod file!");
                    UnityModManager.ModInfo modInfo = File.ReadAllText(infoPath).FromJson<UnityModManager.ModInfo>();
                    UnityModManager.ModEntry loaded = new UnityModManager.ModEntry(modInfo, modPath + Path.DirectorySeparatorChar.ToString());
                    loaded.Load();
                    modEntry.Logger.Log($"Loaded {loaded.Info.Id} by {loaded.Info.Author}");

                    loadeds.Add(loaded);
                }
                catch (Exception e)
                {
                    modEntry.Logger.Log(e.ToString());
                    faileds.Add(requirements[i]);
                }
            }
            if (faileds.Count != 0)
            {
                modEntry.OnGUI = _ =>
                {
                    GUILayout.Label("Failed to download some required mods!");
                    GUILayout.Label("몇몇 필요한 모드를 설치하는 데 실패했습니다!");
                };
                modEntry.OnToggle = null;
                modEntry.OnSaveGUI = null;
                modEntry.OnUpdate = null;
                modEntry.OnLateUpdate = null;
                modEntry.OnFixedUpdate = null;
                modEntry.OnFixedGUI = null;
                modEntry.Info.DisplayName = $"<color=red>{modEntry.Info.DisplayName}</color> [ERROR]";
                return false;
            }
            if (restart)
            {
                modEntry.OnGUI = _ =>
                {
                    GUILayout.Label("Updated required mods!");
                    GUILayout.Label("Please restart the game in order to apply the change.");
                    GUILayout.Label("필요한 모드들을 업데이트했습니다!");
                    GUILayout.Label("변경사항을 적용하기 위해 게임을 재시작해주세요.");
                };
                modEntry.OnToggle = null;
                modEntry.OnSaveGUI = null;
                modEntry.OnUpdate = null;
                modEntry.OnLateUpdate = null;
                modEntry.OnFixedUpdate = null;
                modEntry.OnFixedGUI = null;
                modEntry.Info.DisplayName = $"<color=yellow>{modEntry.Info.DisplayName}</color> [RESTART NEEDED]";
                return false;
            }

            void add(UnityModManager.ModEntry _, float __)
            {
                UnityModManager.modEntries.InsertRange(UnityModManager.modEntries.IndexOf(modEntry), loadeds);
                modEntry.OnLateUpdate -= add;
            }

            modEntry.OnLateUpdate += add;
            return true;
        }

        public static string FindInfo(string path)
        {
            UnityModManager.GameInfo config = typeof(UnityModManager).Get<UnityModManager.GameInfo>("Config");
            string infoPath = Path.Combine(path, config.ModInfo);
            if (!File.Exists(infoPath))
                infoPath = Path.Combine(path, config.ModInfo.ToLower());
            return File.Exists(infoPath) ? infoPath : null;
        }

        public static string UnzipMod(string path, string id)
        {
            string modPath = Path.Combine(UnityModManager.modsPath, id);

            using (ZipArchive archive = ZipFile.OpenRead(path))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string dest = Path.Combine(modPath, entry.FullName);
                    if (dest.EndsWith("/"))
                        continue;
                    DirectoryInfo parent = Directory.GetParent(dest);
                    if (parent.Name == parent.Parent.Name)
                        dest = Path.Combine(parent.Parent.FullName, entry.Name);
                    Directory.CreateDirectory(Path.GetDirectoryName(dest));
                    entry.ExtractToFile(dest, true);
                }
            }

            return modPath;
        }
    }
}

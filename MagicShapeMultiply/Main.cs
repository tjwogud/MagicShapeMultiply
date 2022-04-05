using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityModManagerNet;

namespace MagicShapeMultiply
{
    public static class Main
    {
        public static UnityModManager.ModEntry.ModLogger Logger;
        public static Harmony harmony;
        public static bool IsEnabled = false;
        public static Settings Settings { get; set; }

        public static void Setup(UnityModManager.ModEntry modEntry)
        {
            Logger = modEntry.Logger;
            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = OnGUI;
            modEntry.OnUpdate = OnUpdate;
            modEntry.OnSaveGUI = OnSaveGUI;
            Logger.Log("Loading Settings...");
            Settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
            Logger.Log("Load Completed!");
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            IsEnabled = value;
            if (value)
            {
                harmony = new Harmony(modEntry.Info.Id);
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
            else
            {
                harmony.UnpatchAll(modEntry.Info.Id);
            }
            return true;
        }

        private static readonly string[] texts1 = new string[] { "내각", "외각", "그대로" };
        private static readonly string[] texts2 = new string[] { "BPM", "승수" };
        private static readonly string[] texts3 = new string[] { "토끼/달팽이", "소용돌이" };
        private static readonly string[] texts4 = new string[] { "체감 BPM", "타일 BPM" };

        private static readonly string[] eng_texts1 = new string[] { "Internal", "External", "Just" };
        private static readonly string[] eng_texts2 = new string[] { "BPM", "Multiplier" };
        private static readonly string[] eng_texts3 = new string[] { "Rabbit/Snail", "Twirl" };
        private static readonly string[] eng_texts4 = new string[] { "Real BPM", "Tile BPM" };

        private static bool changing;

        public static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            switch (GUILayout.Toolbar(Settings.EnableInOrOut ? (Settings.InOrOut ? 0 : 1) : 2, RDString.language == SystemLanguage.Korean ? texts1 : eng_texts1, GUILayout.Width(230)))
            {
                case 0:
                    Settings.EnableInOrOut = true;
                    Settings.InOrOut = true;
                    break;
                case 1:
                    Settings.EnableInOrOut = true;
                    Settings.InOrOut = false;
                    break;
                case 2:
                    Settings.EnableInOrOut = false;
                    break;
            }
            Settings.MultiplyOrBPM = GUILayout.Toolbar(Settings.MultiplyOrBPM ? 1 : 0, RDString.language == SystemLanguage.Korean ? texts2 : eng_texts2, GUILayout.Width(230)) == 1;
            GUILayout.BeginHorizontal();
            Settings.ShowEvent = GUILayout.Toolbar(Settings.ShowEvent ? 0 : 1, RDString.language == SystemLanguage.Korean ? texts3 : eng_texts3, GUILayout.Width(230)) == 0;
            GUILayout.Label(RDString.language == SystemLanguage.Korean ? "(보여지는 이펙트)" : "(Event That You Can See)");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            Settings.RealOrTileBPM = GUILayout.Toolbar(Settings.RealOrTileBPM ? 0 : 1, RDString.language == SystemLanguage.Korean ? texts4 : eng_texts4, GUILayout.Width(230)) == 0; ;
            GUILayout.Label(RDString.language == SystemLanguage.Korean ? "(맞출 기준 BPM)" : "(Multiply BPM)");
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            Settings.ctrl = GUILayout.Toggle(Settings.ctrl, "Ctrl");
            Settings.alt = GUILayout.Toggle(Settings.alt, "Alt");
            Settings.shift = GUILayout.Toggle(Settings.shift, "Shift");
            GUILayout.Space(20);
            if (!changing && GUILayout.Button($"{Settings.key}"))
                changing = true;
            else if (changing)
            {
                GUILayout.Button(RDString.language == SystemLanguage.Korean ? "바꾸는중..." : "Changing...");
                Event e = Event.current;
                if (e.isKey && e.type == EventType.KeyDown)
                {
                    Settings.key = e.keyCode;
                    changing = false;
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        public static void OnUpdate(UnityModManager.ModEntry modEntry, float value)
        {
            if (!IsEnabled)
                return;
            if (scnEditor.instance == null || !scnEditor.instance.isEditingLevel)
                return;
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftMeta) || Input.GetKey(KeyCode.RightMeta);
            bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if ((!Settings.ctrl || ctrl) && (!Settings.alt || alt) && (!Settings.shift || shift) && Input.GetKeyDown(Settings.key))
            {
                MagicShape.Multiply();
            }
        }

        public static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            Logger.Log("Saving Settings...");
            Settings.Save(modEntry);
            Logger.Log("Save Completed!");
        }
    }
}
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

        private static GUIStyle style1;
        private static GUIStyle style2;
        private static GUIStyle style3;
        private static readonly string[] texts1 = new string[] { "내각", "외각", "그대로" };
        private static readonly string[] texts2 = new string[] { "BPM", "승수(권장하지 않음)" };
        private static readonly string[] texts3 = new string[] { "토끼/달팽이", "소용돌이" };

        private static readonly string[] eng_texts1 = new string[] { "Internal", "External", "Just" };
        private static readonly string[] eng_texts2 = new string[] { "BPM", "Multiplier(Not Recommended)" };
        private static readonly string[] eng_texts3 = new string[] { "Rabbit/Snail", "Twirl" };

        public static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            if (style1 == null)
            {
                style1 = new GUIStyle(GUI.skin.button);
                style1.fixedWidth = 70;

                style2 = new GUIStyle(GUI.skin.button);
                style2.fixedWidth = 200;

                style3 = new GUIStyle(GUI.skin.button);
                style3.fixedWidth = 90;
            }
            switch (GUILayout.Toolbar(Settings.EnableInOrOut ? (Settings.InOrOut ? 0 : 1) : 2, RDString.language == SystemLanguage.Korean ? texts1 : eng_texts1, style1))
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
            Settings.MultiplyOrBPM = GUILayout.Toolbar(Settings.MultiplyOrBPM ? 1 : 0, RDString.language == SystemLanguage.Korean ? texts2 : eng_texts2, style2) == 0 ? false : true;
            GUILayout.BeginHorizontal();
            Settings.ShowEvent = GUILayout.Toolbar(Settings.ShowEvent ? 0 : 1, RDString.language == SystemLanguage.Korean ? texts3 : eng_texts3, style3) == 0 ? true : false;
            GUILayout.Label(RDString.language == SystemLanguage.Korean ? "(보여지는 이펙트)" : "(Event That You Can See)");
            GUILayout.EndHorizontal();
            Settings.KeepSelecteds = GUILayout.Toggle(Settings.KeepSelecteds, RDString.language == SystemLanguage.Korean ? "Ctrl + F 시 선택된 타일 유지" : "Keep Selected Tiles When You Press Ctrl + F");
        }

        public static void OnUpdate(UnityModManager.ModEntry modEntry, float value)
        {
            if (!IsEnabled)
                return;
            if (scnEditor.instance == null || !scnEditor.instance.isEditingLevel)
                return;
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.M))
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
using ADOFAI;
using DG.Tweening;
using EditorTabLib;
using EditorTabLib.Properties;
using EditorTabLib.Utils;
using HarmonyLib;
using Localizations;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityModManagerNet;

namespace MagicShapeMultiply
{
    public static class Main
    {
        public static Settings Settings;
        public static UnityModManager.ModEntry ModEntry;
        public static UnityModManager.ModEntry.ModLogger Logger;
        public static Localization Localization;
        public static Harmony harmony;
        public static bool IsEnabled = false;

        public static Sprite Icon { get; private set; }

        private static bool changing = false;

        public static void Setup(UnityModManager.ModEntry modEntry)
        {
            ModEntry = modEntry;
            Logger = modEntry.Logger;
            modEntry.OnUpdate = (_, _) =>
            {
                if (scnEditor.instance == null || scnEditor.instance.playMode)
                    return;
                if ((!Settings.ctrl || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                    && (!Settings.alt || Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                    && (!Settings.shift || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    && Input.GetKeyDown(Settings.key))
                    MagicShape.Multiply();
            };
            modEntry.OnGUI = _ =>
            {
                GUILayout.BeginHorizontal();
                Settings.ctrl = GUILayout.Toggle(Settings.ctrl, "Ctrl");
                GUILayout.Label("+");
                Settings.alt = GUILayout.Toggle(Settings.alt, "Alt");
                GUILayout.Label("+");
                Settings.shift = GUILayout.Toggle(Settings.shift, "Shift");
                GUILayout.Label("+");
                if (!changing && GUILayout.Button($"{Settings.key}"))
                    changing = true;
                else if (changing)
                {
                    GUILayout.Button(Localization["msm.gui.changing"]);
                    Event e = Event.current;
                    if (e.isKey && e.type == EventType.KeyDown)
                    {
                        Settings.key = e.keyCode;
                        changing = false;
                    }
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            };
            modEntry.OnSaveGUI = _ => {
                Logger.Log("Saving Settings...");
                Settings.Save(modEntry);
                Logger.Log("Save Completed!");
            };
            modEntry.OnToggle = OnToggle;
            Logger.Log("Loading Icon...");
            string path = Path.Combine(modEntry.Path, "icon.png");
            if (File.Exists(path))
            {
                byte[] data = File.ReadAllBytes(path);
                Texture2D texture = new Texture2D(0, 0);
                if (texture.LoadImage(data))
                {
                    Icon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    Logger.Log("Load Completed!");
                    goto next;
                }
            }
            Logger.Log("Can't load Icon! try reinstall the mod.");
            return;
        next:
            Logger.Log("Loading Settings...");
            Settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
            Logger.Log("Load Completed!");
            Localization = Localization.Load("1QcrRL6LAs8WxJj_hFsEJa3CLM5g3e8Ya0KQlRKXwdlU", 61572944, modEntry, onLoad: (keyValue) => (TypeUtils.ReplaceClassName(keyValue.Item1), keyValue.Item2));
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            IsEnabled = value;
            if (value)
            {
                harmony = new Harmony(modEntry.Info.Id);
                harmony.PatchAll();
                CustomTabManager.AddTab(
                    icon: Icon,
                    type: 501,
                    name: "MagicShape",
                    title: new Dictionary<SystemLanguage, string>() {
                        { SystemLanguage.Korean, "마법진" },
                        { SystemLanguage.English, "Magic Shape" }
                    },
                    properties: new List<EditorTabLib.Properties.Property>()
                    {
                        new Property_Enum<MagicShape.MultiplyType>(
                            name: "multiplyType",
                            value_default: MagicShape.MultiplyType.Bpm,
                            key: "msm.editor.multiplyType"),
                        new Property_InputField(
                            name: "beatsPerMinute",
                            type: Property_InputField.InputType.Float,
                            value_default: 100f,
                            min: 0.001f,
                            max: 10000f,
                            unit: "bpm",
                            key: "msm.editor.bpm",
                            enableIf: new Dictionary<string, string>() { { "multiplyType", "Bpm" } }),
                        new Property_InputField(
                            name: "bpmMultiplier",
                            type: Property_InputField.InputType.Float,
                            value_default: 1f,
                            min: 1E-7f,
                            max: 128f,
                            unit: "X",
                            key: "msm.editor.multiplier",
                            enableIf: new Dictionary<string, string>() { { "multiplyType", "Multiplier" } }),
                        new Property_Enum<MagicShape.MultiplyType>(
                            name: "setSpeedType",
                            value_default: MagicShape.MultiplyType.Bpm,
                            key: "msm.editor.setSpeedType"),
                        new Property_Enum<MagicShape.Angle>(
                            name: "direction",
                            value_default: MagicShape.Angle.Internal,
                            key: "msm.editor.direction",
                            canBeDisabled: true,
                            startEnabled: true,
                            disableIf: new Dictionary<string, string>() { { "changeShape", "Enabled" } }),
                        new Property_Enum<MagicShape.ShowEvent>(
                            name: "showEvent",
                            value_default: MagicShape.ShowEvent.SetSpeed,
                            key: "msm.editor.showEvent",
                            disableIf: new Dictionary<string, string>() { { "changeShape", "Enabled" } }),
                        new Property_Enum<ToggleBool>(
                            name: "changeShape",
                            value_default: ToggleBool.Disabled,
                            key: "msm.editor.changeShape",
                            enableIf: new Dictionary<string, string>() { { "multiplyType", "Multiplier" } }),
                        new Property_InputField(
                            name: "angleCorrection",
                            type: Property_InputField.InputType.Int,
                            value_default: -1,
                            min: -1,
                            max: 1,
                            key: "msm.editor.angleCorrection",
                            canBeDisabled: true,
                            startEnabled: true,
                            enableIf: new Dictionary<string, string>() { { "changeShape", "Enabled" } }),
                        new Property_Button(
                            name: "multiply",
                            action: () => MagicShape.Multiply(),
                            key: "msm.editor.multiply")
                    },
                    saveSetting: true,
                    onFocused: () => {
                        Patches.SelectedColorsPatch.blue = true;
                        DOTween.Kill("selectedColorTween", false);
                        scrFloor lastSelected = scnEditor.instance.Get<scrFloor>("lastSelectedFloor");
                        if (lastSelected)
                            lastSelected.SetColor(lastSelected.floorRenderer.deselectedColor);
                        scnEditor.instance.selectedFloors.ForEach(f =>
                        {
                            f.SetColor(f.floorRenderer.deselectedColor);
                            scnEditor.instance.Method("ShowSelectedColor", new object[] { f, 1 }, new Type[] { typeof(scrFloor), typeof(float) });
                        });
                    },
                    onUnFocused: () => {
                        Patches.SelectedColorsPatch.blue = false;
                        DOTween.Kill("selectedColorTween", false);
                        scrFloor lastSelected = scnEditor.instance.Get<scrFloor>("lastSelectedFloor");
                        if (lastSelected)
                            lastSelected.SetColor(lastSelected.floorRenderer.deselectedColor);
                        scnEditor.instance.selectedFloors.ForEach(f =>
                        {
                            f.SetColor(f.floorRenderer.deselectedColor);
                            scnEditor.instance.Method("ShowSelectedColor", new object[] { f, 1 }, new Type[] { typeof(scrFloor), typeof(float) });
                        });
                    },
                    onChange: (e, name, prevValue, value) =>
                    {
                        if (name == "multiplyType" && value is MagicShape.MultiplyType.Bpm)
                        {
                            e.data["changeShape"] = ToggleBool.Disabled;
                            e.UpdatePanel();
                        }
                        return true;
                    }
                );
            }
            else
            {
                harmony.UnpatchAll(modEntry.Info.Id);
                CustomTabManager.DeleteTab(501);
            }
            return true;
        }
    }
}
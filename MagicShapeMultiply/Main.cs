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

        public static Sprite Icon_Multiply { get; private set; }
        public static Sprite Icon_Create { get; private set; }

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
            bool CreateSprite(string path, out Sprite result)
            {
                if (File.Exists(path))
                {
                    byte[] data = File.ReadAllBytes(path);
                    Texture2D texture = new Texture2D(0, 0);
                    if (texture.LoadImage(data))
                    {
                        result = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                        return true;
                    }
                }
                result = null;
                return false;
            }
            if (CreateSprite(Path.Combine(modEntry.Path, "icon_multiply.png"), out Sprite spr1) && CreateSprite(Path.Combine(modEntry.Path, "icon_create.png"), out Sprite spr2))
            {
                Icon_Multiply = spr1;
                Icon_Create = spr2;
            }
            else
            {
                Logger.Log("Can't load Icon! try reinstall the mod.");
                return;
            }
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
                    icon: Icon_Multiply,
                    type: 501,
                    name: "MagicShape_Multiply",
                    title: new Dictionary<SystemLanguage, string>() {
                        { SystemLanguage.Korean, "마법진 승수 맞추기" },
                        { SystemLanguage.English, "Multiply Magic Shape" }
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
                        new Property_Bool(
                            name: "changeShape",
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
                        Patches.SelectedColorsPatch.value = 0.5f;
                        Patches.SelectedColorsPatch.Apply();
                    },
                    onUnFocused: () => {
                        if (Patches.SelectedColorsPatch.value == 0.5f)
                            Patches.SelectedColorsPatch.value = null;
                        Patches.SelectedColorsPatch.Apply();
                    },
                    onChange: (e, name, prevValue, value) =>
                    {
                        if (name == "multiplyType" && value is MagicShape.MultiplyType.Bpm)
                        {
                            e.data["changeShape"] = false;
                            e.UpdatePanel();
                        }
                        return true;
                    }
                );
                CustomTabManager.AddTab(
                    icon: Icon_Create,
                    type: 502,
                    name: "MagicShape_Create",
                    title: new Dictionary<SystemLanguage, string>() {
                        { SystemLanguage.Korean, "마법진 만들기" },
                        { SystemLanguage.English, "Create Magic Shape" }
                    },
                    properties: new List<EditorTabLib.Properties.Property>()
                    {
                        new Property_Tile(
                            name: "startTile",
                            value_default: (0, TileRelativeTo.Start),
                            hideButtons: Property_Tile.THIS_TILE,
                            key: "editor.startTile"),
                        new Property_Tile(
                            name: "endTile",
                            value_default: (0, TileRelativeTo.End),
                            hideButtons: Property_Tile.THIS_TILE,
                            key: "editor.endTile"),
                        new Property_Bool(
                            name: "showPreview",
                            value_default: true,
                            key: "msm.editor.showPreview"),
                        new Property_InputField(
                            name: "vertexCount",
                            type: Property_InputField.InputType.Int,
                            value_default: 1,
                            min: 4,
                            key: "msm.editor.vertexCount"),
                        new Property_Button(
                            name: "create",
                            action: () => {
                                if (ADOBase.lm.isOldLevel)
                                {
                                    PopupUtils.Show(Localization["msm.editor.dialog.meshFloorRequired"]);
                                    return;
                                }
                                using(new SaveStateScope(scnEditor.instance)) {
                                    LevelEvent levelEvent = CustomTabManager.GetEvent((LevelEventType)502);
                                    static int GetIndex(object data)
                                    {
                                        Tuple<int, TileRelativeTo> tuple = (Tuple<int, TileRelativeTo>)data;
                                        int length = ADOBase.lm.floorAngles.Length;
                                        return Mathf.Clamp(tuple.Item1 + (tuple.Item2 == TileRelativeTo.End ? length : 0), 0, length);
                                    }

                                    int startIndex = GetIndex(levelEvent.data["startTile"]);
                                    int endIndex = GetIndex(levelEvent.data["endTile"]);
                                    if (startIndex > endIndex)
                                    {
                                        int temp = startIndex;
                                        startIndex = endIndex;
                                        endIndex = temp;
                                    }
                                    int vertex = (int)levelEvent.data["vertexCount"];

                                    List<float> angles = new List<float>();
                                    for (int i = 1; i < vertex; i++)
                                    {
                                        for (int j = startIndex; j <= endIndex; j++)
                                        {
                                            float angle = j == 0 ? 0 : ADOBase.lm.listFloors[j].floatDirection;
                                            angles.Add(angle == 999 ? 999 : (angle - 360f / vertex * i));
                                        }
                                    }
                                    scnEditor.instance.levelData.angleData.InsertRange(endIndex, angles);
                                    levelEvent.data["showPreview"] = false;
                                    levelEvent.UpdatePanel();
                                    scnEditor.instance.RemakePath();
                                }
                            },
                            key: "msm.editor.create")
                    },
                    saveSetting: true,
                    onFocused: () => {
                        Patches.FakeFloorsPatch.enabled = true;
                        scnEditor.instance.RemakePath();
                        Patches.SelectedColorsPatch.value = 0.75f;
                        Patches.SelectedColorsPatch.Apply();
                    },
                    onUnFocused: () => {
                        Patches.FakeFloorsPatch.enabled = false;
                        scnEditor.instance.RemakePath();
                        if (Patches.SelectedColorsPatch.value == 0.75f)
                            Patches.SelectedColorsPatch.value = null;
                        Patches.SelectedColorsPatch.Apply();
                    },
                    onChange: (e, name, prevValue, value) =>
                    {
                        if (ADOBase.lm.isOldLevel)
                        {
                            PopupUtils.Show(Localization["msm.editor.dialog.meshFloorRequired"]);
                            return true;
                        }
                        scnEditor.instance.RemakePath();
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
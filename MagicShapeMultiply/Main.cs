using ADOFAI;
using DG.Tweening;
using EditorTabLib;
using EditorTabLib.Properties;
using EditorTabLib.Utils;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityModManagerNet;

namespace MagicShapeMultiply
{
    public static class Main
    {
        public static UnityModManager.ModEntry.ModLogger Logger;
        public static UnityModManager.ModEntry ModEntry;
        public static Harmony harmony;
        public static bool IsEnabled = false;

        public static Sprite Icon { get; private set; }

        public static float f = 0.25f;

        public static void Setup(UnityModManager.ModEntry modEntry)
        {
            ModEntry = modEntry;
            Logger = modEntry.Logger;
            modEntry.OnToggle = OnToggle;
            Logger.Log("Loading Localizations...");
            Localizations.Load();
            Logger.Log("Load Completed!");
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
                    return;
                }
            }
            Logger.Log("Can't load Icon! try reinstall the mod.");
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
                            action: () => {
                                using (new SaveStateScope(scnEditor.instance, false, false, false))
                                {
                                    LevelEvent levelEvent = scnEditor.instance.settingsPanel.selectedEvent;
                                    MagicShape.MultiplyType multiplyType = (MagicShape.MultiplyType)levelEvent.data["multiplyType"];
                                    float bpm = (float)levelEvent.data["beatsPerMinute"];
                                    float multiplier = (float)levelEvent.data["bpmMultiplier"];
                                    MagicShape.MultiplyType setSpeedType = (MagicShape.MultiplyType)levelEvent.data["setSpeedType"];
                                    MagicShape.Angle? direction = levelEvent.disabled["direction"] ? null : (MagicShape.Angle)levelEvent.data["direction"];
                                    MagicShape.ShowEvent showEvent = (MagicShape.ShowEvent)levelEvent.data["showEvent"];
                                    bool changeShape = (ToggleBool)levelEvent.data["changeShape"] == ToggleBool.Enabled;
                                    int? angleCorrection = levelEvent.disabled["angleCorrection"] ? null : (int)levelEvent.data["angleCorrection"];
                                    Tuple<int, Dictionary<string, object>> result;
                                    switch (multiplyType)
                                    {
                                        case MagicShape.MultiplyType.Bpm: {
                                            result = MagicShape.MultiplyWithBPM(bpm, setSpeedType, showEvent, direction);
                                            break;
                                        }
                                        case MagicShape.MultiplyType.Multiplier: {
                                            result = !changeShape
                                                ? MagicShape.MultiplyWithMultiplier(multiplier, setSpeedType, showEvent, direction)
                                                : MagicShape.MultiplyWithAngle(multiplier, angleCorrection, setSpeedType);
                                            break;
                                        }
                                        default:
                                            return;
                                    }
                                    switch (result.Item1)
                                    {
                                        case -1:
                                            PopupUtils.Show(Localizations.GetString("msm.editor.dialog.error", out string value) ? value : string.Empty);
                                            break;
                                        case -2:
                                            PopupUtils.Show(Localizations.GetString("msm.editor.dialog.selectionIsSingleOrNone", out value) ? value : string.Empty);
                                            break;
                                        case -3:
                                            PopupUtils.ShowParams(Localizations.GetString("msm.editor.dialog.containsExceptionEvents", out value) ? value : string.Empty, (result.Item2["eventTypes"] as List<LevelEventType>).Select(type => RDString.Get("editor." + type.ToString())).ToArray());
                                            break;
                                        case -4:
                                            PopupUtils.Show(Localizations.GetString("msm.editor.dialog.tooBigAngle", out value, result.Item2) ? value : string.Empty);
                                            break;
                                        case -5:
                                            PopupUtils.Show(Localizations.GetString("msm.editor.dialog.invalidAngle", out value, result.Item2) ? value : string.Empty);
                                            break;
                                    }
                                }
                            },
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
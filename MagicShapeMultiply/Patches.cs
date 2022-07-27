using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace MagicShapeMultiply
{
    public static class Patches
    {
        [HarmonyPatch(typeof(RDString), "GetWithCheck")]
        public static class GetWithCheck
        {
            public static void Postfix(ref string __result, ref string key, ref bool exists, ref Dictionary<string, object> parameters)
            {
                //Main.Logger.Log(key);
                if (Main.Localization.Get(key, out string value, parameters))
                {
                    exists = true;
                    __result = value;
                    //Main.Logger.Log("value");
                }
                //Main.Logger.Log("-------------------------");
            }
        }

        [HarmonyPatch(typeof(scnEditor), "Start")]
        public static class StartPatch
        {
            public static void Postfix()
            {
                SelectedColorsPatch.blue = false;
                PopupUtils.popup = Object.Instantiate(scnEditor.instance.okPopupContainer, scnEditor.instance.popupWindow.transform);
                PopupUtils.popup.GetComponentsInAllChildren<scrTextChanger>().ForEach(Object.Destroy);
                PopupUtils.paramsPopup = Object.Instantiate(scnEditor.instance.paramsPopupContainer, scnEditor.instance.popupWindow.transform);
                PopupUtils.paramsPopup.GetComponentsInAllChildren<scrTextChanger>().ForEach(Object.Destroy);
            }
        }
        
        [HarmonyPatch(typeof(scnEditor), "SelectedColors")]
        public static class SelectedColorsPatch
        {
            public static bool blue = false;

            public static void Postfix(Color color, ref Color[] __result)
            {
                Color[] array = new Color[2];
                Color.RGBToHSV(color, out float num, out float num2, out float num3);
                num = (num + (blue ? 0.5f : 0.25f)) % 1f;
                num3 = Mathf.Clamp01(num3 * 1.2f);
                float s = Mathf.Clamp01(num2 + 0.5f);
                float v = (num3 > 0.75f) ? (num3 - 0.25f) : (num3 + 0.25f);
                array[0] = Color.HSVToRGB(num, num2, num3);
                array[1] = Color.HSVToRGB(num, s, v);
                __result = array;
            }
        }
    }
}

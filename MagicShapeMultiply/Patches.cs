using ADOFAI;
using DG.Tweening;
using EditorTabLib;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using TinyJson;
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
                SelectedColorsPatch.value = null;
                PopupUtils.popup = UnityEngine.Object.Instantiate(scnEditor.instance.okPopupContainer, scnEditor.instance.popupWindow.transform);
                PopupUtils.popup.GetComponentsInAllChildren<scrTextChanger>().ForEach(UnityEngine.Object.Destroy);
                PopupUtils.paramsPopup = UnityEngine.Object.Instantiate(scnEditor.instance.paramsPopupContainer, scnEditor.instance.popupWindow.transform);
                PopupUtils.paramsPopup.GetComponentsInAllChildren<scrTextChanger>().ForEach(UnityEngine.Object.Destroy);
            }
        }

        public static class FakeFloorsPatch
        {
            public static List<scrFloor> fakeFloors = new List<scrFloor>();
            public static bool playing = false;
            public static bool enabled = false;

            public static int GetIndex(object data)
            {
                Tuple<int, TileRelativeTo> tuple = (Tuple<int, TileRelativeTo>)data;
                int length = ADOBase.lm.floorAngles.Length;
                return Mathf.Clamp(tuple.Item1 + (tuple.Item2 == TileRelativeTo.End ? length : 0), 0, length);
            }

            [HarmonyPatch(typeof(scrLevelMaker), "MakeLevel")]
            public static class CreatePatch
            {
                public static void Postfix()
                {
                    fakeFloors.Where(f => f).ToList().ForEach(f => UnityEngine.Object.DestroyImmediate(f.gameObject));
                    fakeFloors.Clear();
                    if (scnEditor.instance != null && !playing && enabled && !ADOBase.lm.isOldLevel)
                    {
                        LevelEvent levelEvent = CustomTabManager.GetEvent((LevelEventType)502);
                        if (levelEvent == null)
                            return;
                        bool show = (bool)levelEvent.data["showPreview"];
                        if (!show)
                            return;

                        int startIndex = GetIndex(levelEvent.data["startTile"]);
                        int endIndex = GetIndex(levelEvent.data["endTile"]);
                        if (startIndex > endIndex)
                        {
                            int temp = startIndex;
                            startIndex = endIndex;
                            endIndex = temp;
                        }
                        int vertex = (int)levelEvent.data["vertexCount"];
                        int inverse = (bool)levelEvent.data["inverseAngle"] ? -1 : 1;

                        GameObject fakeFloor = GameObject.Find("FakeFloors") ?? new GameObject("FakeFloors");
                        scrFloor start = ADOBase.lm.listFloors[startIndex];
                        scrFloor end = ADOBase.lm.listFloors[endIndex];

                        int order = 100 + ADOBase.lm.listFloors.Count + (endIndex - startIndex + 1) * (vertex - 1);
                        for (int i = 0; i <= endIndex; i++)
                        {
                            ADOBase.lm.listFloors[i].SetSortingOrder(order * 5);
                            order--;
                        }

                        Vector3 vector = end.transform.position;
                        scrFloor prev = end;
                        double angle;

                        for (int i = 1; i < vertex; i++)
                        {
                            for (int j = startIndex; j <= endIndex; j++)
                            {
                                float n = j == 0 ? 0 : ADOBase.lm.listFloors[j].floatDirection;
                                angle = n == 999 ? prev.entryangle : ((-n + 90 + (360f / vertex * i * inverse)) * Mathf.PI / 180);

                                prev.exitangle = angle;

                                vector += scrMisc.getVectorFromAngle(angle, scrController.instance.startRadius);

                                GameObject obj = UnityEngine.Object.Instantiate(ADOBase.lm.meshFloor, vector, Quaternion.identity);
                                obj.name = $"FakeFloor (copy of #{j})";
                                obj.transform.parent = fakeFloor.transform;
                                scrFloor floor = obj.GetComponent<scrFloor>();
                                floor.entryangle = (angle + Mathf.PI) % (Mathf.PI * 2);
                                prev.nextfloor = floor;
                                prev.midSpin = n == 999;
                                prev.UpdateAngle();
                                prev = floor;

                                floor.floorRenderer.color = new Color(1, 1, 1, 0.5f);
                                floor.editorNumText.letterText.gameObject.SetActive(false);
                                floor.SetSortingOrder(order * 5);
                                order--;
                                fakeFloors.Add(floor);
                            }
                        }
                        if (ADOBase.lm.listFloors.Count > endIndex + 1)
                        {
                            float n = ADOBase.lm.listFloors[endIndex + 1].floatDirection;
                            angle = n == 999 ? prev.entryangle : ((-n + 90) * Mathf.PI / 180);
                            prev.exitangle = angle;
                            prev.nextfloor = end;
                            prev.UpdateAngle();
                            vector -= end.transform.position;
                            prev.offsetPos = vector;
                        }
                    }
                }
            }

            [HarmonyPatch(typeof(scnGame), "ApplyEventsToFloors", typeof(List<scrFloor>), typeof(LevelData), typeof(scrLevelMaker), typeof(List<LevelEvent>))]
            public static class MoveTilePatch
            {
                public static void Postfix()
                {
                    if (scnEditor.instance != null && !playing && enabled && !ADOBase.lm.isOldLevel && fakeFloors.Count > 0)
                    {
                        LevelEvent levelEvent = CustomTabManager.GetEvent((LevelEventType)502);
                        if (levelEvent == null)
                            return;

                        int startIndex = GetIndex(levelEvent.data["startTile"]);
                        int endIndex = GetIndex(levelEvent.data["endTile"]);
                        if (ADOBase.lm.listFloors.Count > Mathf.Max(startIndex, endIndex) + 1)
                        {
                            Vector3 vector = fakeFloors.Last().offsetPos;
                            for (int i = Mathf.Max(startIndex, endIndex) + 1; i < ADOBase.lm.listFloors.Count; i++)
                            {
                                ADOBase.lm.listFloors[i].transform.position += vector;
                            }
                        }
                    }
                }
            }

            [HarmonyPatch(typeof(scnEditor), "Play")]
            public static class PlayPatch
            {
                public static void Prefix()
                {
                    playing = true;
                }
            }

            [HarmonyPatch(typeof(scnEditor), "SwitchToEditMode")]
            public static class SwitchToEditModePatch
            {
                public static void Prefix()
                {
                    playing = false;
                }
            }

            [HarmonyPatch(typeof(scnEditor), "Awake")]
            public static class AwakePatch
            {
                public static void Prefix()
                {
                    playing = false;
                    enabled = false;
                }
            }
        }

        [HarmonyPatch(typeof(scnEditor), "SelectedColors")]
        public static class SelectedColorsPatch
        {
            public static float? value = null;

            public static void Postfix(Color color, ref Color[] __result)
            {
                Color[] array = new Color[2];
                Color.RGBToHSV(color, out float num, out float num2, out float num3);
                num = (num + (value ?? 0.25f)) % 1f;
                num3 = Mathf.Clamp01(num3 * 1.2f);
                float s = Mathf.Clamp01(num2 + 0.5f);
                float v = (num3 > 0.75f) ? (num3 - 0.25f) : (num3 + 0.25f);
                array[0] = Color.HSVToRGB(num, num2, num3);
                array[1] = Color.HSVToRGB(num, s, v);
                __result = array;
            }

            public static void Apply()
            {
                DOTween.Kill("selectedColorTween", false);
                scrFloor lastSelected = scnEditor.instance.Get<scrFloor>("lastSelectedFloor");
                if (lastSelected)
                    lastSelected.SetColor(lastSelected.floorRenderer.deselectedColor);
                scnEditor.instance.selectedFloors.ForEach(f =>
                {
                    f.SetColor(f.floorRenderer.deselectedColor);
                    scnEditor.instance.Method("ShowSelectedColor", new object[] { f, 1 }, new Type[] { typeof(scrFloor), typeof(float) });
                });
            }
        }
    }
}

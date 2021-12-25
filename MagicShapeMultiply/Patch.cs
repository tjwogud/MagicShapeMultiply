using HarmonyLib;
using System.Linq;
using System.Reflection;

namespace MagicShapeMultiply
{
    [HarmonyPatch(typeof(scnEditor), "RemakePath")]
    public static class Patch
    {
        public static int first = -1;
        public static int last = -1;
        public static bool show = false;

        public static bool Prefix()
        {
            if (!Main.IsEnabled || !Main.Settings.KeepSelecteds)
                return true;
            if (scnEditor.instance == null || scnEditor.instance.selectedFloors == null)
                return true;
            bool shownum = (bool)typeof(scnEditor).GetField("showFloorNums", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(scnEditor.instance);
            if (shownum == show)
                return true;
            show = shownum;
            if (scnEditor.instance.selectedFloors.Count <= 1)
                return true;
            first = scnEditor.instance.selectedFloors.First().seqID;
            last = scnEditor.instance.selectedFloors.Last().seqID;
            return true;
        }

        public static void Postfix()
        {
            if (first == -1 || last == -1)
                return;
            MethodInfo method = typeof(scnEditor).GetMethod("MultiSelectFloors", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(scnEditor.instance, new object[] { scnEditor.instance.customLevel.levelMaker.listFloors[first], scnEditor.instance.customLevel.levelMaker.listFloors[last], true });
            first = -1;
            last = -1;
        }
    }
}
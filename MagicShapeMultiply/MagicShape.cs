using ADOFAI;
using EditorTabLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MagicShapeMultiply
{
    public static class MagicShape
    {
        public enum MultiplyType
        {
            Bpm, Multiplier
        }

        public enum Angle
        {
            Internal, External, None
        }

        public enum ShowEvent
        {
            SetSpeed, Twirl
        }

        public static void Multiply()
        {
            using (new SaveStateScope(scnEditor.instance, false, false, false))
            {
                LevelEvent levelEvent = CustomTabManager.GetEvent((LevelEventType)501);
                if (levelEvent == null)
                    return;
                MultiplyType multiplyType = (MultiplyType)levelEvent.data["multiplyType"];
                float bpm = (float)levelEvent.data["beatsPerMinute"];
                float multiplier = (float)levelEvent.data["bpmMultiplier"];
                MultiplyType setSpeedType = (MultiplyType)levelEvent.data["setSpeedType"];
                Angle? direction = levelEvent.disabled["direction"] ? null : (Angle)levelEvent.data["direction"];
                ShowEvent showEvent = (ShowEvent)levelEvent.data["showEvent"];
                bool changeShape = (ToggleBool)levelEvent.data["changeShape"] == ToggleBool.Enabled;
                int? angleCorrection = levelEvent.disabled["angleCorrection"] ? null : (int)levelEvent.data["angleCorrection"];
                Tuple<int, Dictionary<string, object>> result;
                switch (multiplyType)
                {
                    case MultiplyType.Bpm:
                        {
                            result = MultiplyWithBPM(bpm, setSpeedType, showEvent, direction);
                            break;
                        }
                    case MultiplyType.Multiplier:
                        {
                            result = !changeShape
                                ? MultiplyWithMultiplier(multiplier, setSpeedType, showEvent, direction)
                                : MultiplyWithAngle(multiplier, angleCorrection, setSpeedType);
                            break;
                        }
                    default:
                        return;
                }
                switch (result.Item1)
                {
                    case -1:
                        PopupUtils.Show(Main.Localization["msm.editor.dialog.error"]);
                        break;
                    case -2:
                        PopupUtils.Show(Main.Localization["msm.editor.dialog.selectionIsSingleOrNone"]);
                        break;
                    case -3:
                        PopupUtils.ShowParams(Main.Localization["msm.editor.dialog.containsExceptionEvents"], (result.Item2["eventTypes"] as List<LevelEventType>).Select(type => RDString.Get("editor." + type.ToString())).ToArray());
                        break;
                    case -4:
                        PopupUtils.Show(Main.Localization["msm.editor.dialog.tooBigAngle", parameters: result.Item2]);
                        break;
                    case -5:
                        PopupUtils.Show(Main.Localization["msm.editor.dialog.invalidAngle", parameters: result.Item2]);
                        break;
                }
            }
        }

        private static List<LevelEventType> exceptionEventTypes = new List<LevelEventType>() { LevelEventType.FreeRoam, LevelEventType.Pause, LevelEventType.Hold };

        public static Tuple<int, Dictionary<string, object>> MultiplyWithBPM(double bpm, MultiplyType setSpeedType, ShowEvent showEvent, Angle? direction = null, List<scrFloor> floors = null)
        {
            scnEditor editor = scnEditor.instance;
            if (editor == null)
                return Tuple.Create<int, Dictionary<string, object>>(-1, null);
            List<scrFloor> listFloors = scrLevelMaker.instance.listFloors;
            if ((floors ??= editor.selectedFloors)?.Count <= 1)
                return Tuple.Create<int, Dictionary<string, object>>(-2, null);
            List<LevelEvent> removeEvents = new List<LevelEvent>();
            List<LevelEvent> exceptionEvents = new List<LevelEvent>();
            List<LevelEventType> exceptionEventTypes = new List<LevelEventType>();
            foreach (LevelEvent e in editor.events.Where(e => e.floor >= floors[0].seqID && e.floor <= floors.Last().seqID))
                switch (e.eventType)
                {
                    case LevelEventType.SetSpeed:
                        removeEvents.Add(e);
                        break;
                    case LevelEventType.Twirl:
                        if (direction != null)
                            removeEvents.Add(e);
                        break;
                    default:
                        if (MagicShape.exceptionEventTypes.Contains(e.eventType))
                        {
                            exceptionEvents.Add(e);
                            if (!exceptionEventTypes.Contains(e.eventType))
                                exceptionEventTypes.Add(e.eventType);
                        }
                        break;
                }
            if (exceptionEventTypes.Count != 0)
            {
                Dictionary<string, object> dict = new Dictionary<string, object>();
                dict["events"] = exceptionEvents;
                dict["eventTypes"] = exceptionEventTypes;
                return Tuple.Create(-3, dict);
            }
            removeEvents.ForEach(e => editor.events.Remove(e));
            editor.ApplyEventsToFloors();
            if (direction == Angle.Internal || direction == Angle.External)
            {
                bool ccw = floors[0].isCCW;
                for (int i = floors[0].seqID; i <= floors.Last().seqID; i++)
                {
                    scrFloor floor = listFloors[i];
                    if (floor.nextfloor == null)
                        continue;
                    if (floor.midSpin)
                        continue;
                    double angle = floor.GetAngleLength(ccw);
                    if (direction == Angle.Internal == angle > floor.GetAngleLength(!ccw))
                    {
                        int eventFloor = floor.seqID;
                        if (floor.seqID > 0)
                            while ((eventFloor - 1).GetFloor().midSpin)
                                eventFloor--;
                        editor.AddEvent(eventFloor, LevelEventType.Twirl);
                        ccw = !ccw;
                    }
                }
            }
            editor.ApplyEventsToFloors();
            double prevBpm = editor.levelData.bpm * ((floors[0].seqID - 1).GetFloor()?.speed ?? 1);
            for (int i = floors[0].seqID; i <= floors.Last().seqID; i++)
            {
                scrFloor floor = listFloors[i];
                if (floor.nextfloor == null)
                    continue;
                if (floor.midSpin)
                    continue;
                double angle = floor.GetAngleLength();
                double applyBpm = bpm * angle / 180;
                double minus = applyBpm - prevBpm;
                if (Math.Abs(minus) <= 0.001)
                    continue;
                int eventFloor = floor.seqID;
                if (floor.seqID > 0)
                    while ((eventFloor - 1).GetFloor().midSpin)
                        eventFloor--;
                LevelEvent item = new LevelEvent(eventFloor, LevelEventType.SetSpeed);
                if (setSpeedType == MultiplyType.Bpm)
                {
                    item.data["speedType"] = MultiplyType.Bpm;
                    item.data["beatsPerMinute"] = (float)applyBpm;
                }
                else
                {
                    item.data["speedType"] = MultiplyType.Multiplier;
                    item.data["bpmMultiplier"] = (float)(applyBpm / prevBpm);
                }
                if (showEvent == ShowEvent.Twirl)
                    editor.events.Add(item);
                else
                    editor.events.Insert(0, item);
                prevBpm = applyBpm;
            }
            editor.RemakePath(true);
            return Tuple.Create<int, Dictionary<string, object>>(1, null);
        }

        public static Tuple<int, Dictionary<string, object>> MultiplyWithMultiplier(double multiplier, MultiplyType setSpeedType, ShowEvent showEvent, Angle? direction = null, List<scrFloor> floors = null)
        {
            scnEditor editor = scnEditor.instance;
            if (editor == null)
                return Tuple.Create<int, Dictionary<string, object>>(-1, null);
            List<scrFloor> listFloors = scrLevelMaker.instance.listFloors;
            if ((floors ??= editor.selectedFloors)?.Count <= 1)
                return Tuple.Create<int, Dictionary<string, object>>(-2, null);
            LevelEvent[] floorEvents = new LevelEvent[listFloors.Count];
            List<LevelEvent> removeEvents = new List<LevelEvent>();
            List<LevelEvent> exceptionEvents = new List<LevelEvent>();
            List<LevelEventType> exceptionEventTypes = new List<LevelEventType>();
            foreach (LevelEvent e in editor.events.Where(e => e.floor >= floors[0].seqID && e.floor <= floors.Last().seqID))
                switch (e.eventType)
                {
                    case LevelEventType.SetSpeed:
                        floorEvents[e.floor - floors[0].seqID] = e;
                        break;
                    case LevelEventType.Twirl:
                        if (direction != null)
                            removeEvents.Add(e);
                        break;
                    default:
                        if (MagicShape.exceptionEventTypes.Contains(e.eventType))
                        {
                            exceptionEvents.Add(e);
                            if (!exceptionEventTypes.Contains(e.eventType))
                                exceptionEventTypes.Add(e.eventType);
                        }
                        break;
                }
            if (exceptionEventTypes.Count != 0)
            {
                Dictionary<string, object> dict = new Dictionary<string, object>();
                dict["events"] = exceptionEvents;
                dict["eventTypes"] = exceptionEventTypes;
                return Tuple.Create(-3, dict);
            }
            removeEvents.ForEach(e => editor.events.Remove(e));
            editor.ApplyEventsToFloors();
            if (direction == Angle.Internal || direction == Angle.External)
            {
                bool ccw = floors[0].isCCW;
                for (int i = floors[0].seqID; i <= floors.Last().seqID; i++)
                {
                    scrFloor floor = listFloors[i];
                    if (floor.nextfloor == null)
                        continue;
                    if (floor.midSpin)
                        continue;
                    double angle = floor.GetAngleLength(ccw);
                    if (direction == Angle.Internal == angle > floor.GetAngleLength(!ccw))
                    {
                        int eventFloor = floor.seqID;
                        if (floor.seqID > 0)
                            while ((eventFloor - 1).GetFloor().midSpin)
                                eventFloor--;
                        editor.AddEvent(eventFloor, LevelEventType.Twirl);
                        ccw = !ccw;
                    }
                }
            }
            editor.ApplyEventsToFloors();
            double prevBpm = editor.levelData.bpm * ((floors[0].seqID - 1).GetFloor()?.speed ?? 1);
            for (int i = floors[0].seqID; i <= floors.Last().seqID; i++)
            {
                scrFloor floor = listFloors[i];
                if (floor.nextfloor == null)
                    continue;
                if (floor.midSpin)
                    continue;
                double bpm = floor.speed * editor.levelData.bpm;
                double applyBpm = bpm * multiplier;
                if (applyBpm / prevBpm == 1)
                {
                    editor.events.Remove(floorEvents[i - floors[0].seqID]);
                    continue;
                }
                int eventFloor = floor.seqID;
                if (floor.seqID > 0)
                    while ((eventFloor - 1).GetFloor().midSpin)
                        eventFloor--;
                LevelEvent item = floorEvents[i - floors[0].seqID] ?? new LevelEvent(eventFloor, LevelEventType.SetSpeed);
                if (setSpeedType == MultiplyType.Bpm)
                {
                    item.data["speedType"] = MultiplyType.Bpm;
                    item.data["beatsPerMinute"] = (float)applyBpm;
                }
                else
                {
                    item.data["speedType"] = MultiplyType.Multiplier;
                    item.data["bpmMultiplier"] = (float)(applyBpm / prevBpm);
                }
                if (showEvent == ShowEvent.Twirl)
                    editor.events.Add(item);
                else
                    editor.events.Insert(0, item);
                prevBpm = applyBpm;
            }
            editor.RemakePath(true);
            return Tuple.Create<int, Dictionary<string, object>>(1, null);
        }
        
        public static Tuple<int, Dictionary<string, object>> MultiplyWithAngle(double multiplier, int? angleCorrection = null, MultiplyType setSpeedType = MultiplyType.Bpm, List<scrFloor> floors = null)
        {
            scnEditor editor = scnEditor.instance;
            if (editor == null)
                return Tuple.Create<int, Dictionary<string, object>>(-1, null);
            List<scrFloor> listFloors = scrLevelMaker.instance.listFloors;
            if ((floors ??= editor.selectedFloors)?.Count <= 1)
                return Tuple.Create<int, Dictionary<string, object>>(-2, null);
            List<LevelEvent> exceptionEvents = new List<LevelEvent>();
            List<LevelEventType> exceptionEventTypes = new List<LevelEventType>();
            foreach (LevelEvent e in editor.events.Where(e => e.floor >= floors[0].seqID && e.floor <= floors.Last().seqID))
                if (MagicShape.exceptionEventTypes.Contains(e.eventType))
                {
                    exceptionEvents.Add(e);
                    if (!exceptionEventTypes.Contains(e.eventType))
                        exceptionEventTypes.Add(e.eventType);
                }
            if (exceptionEventTypes.Count != 0)
            {
                Dictionary<string, object> dict = new Dictionary<string, object>();
                dict["events"] = exceptionEvents;
                dict["eventTypes"] = exceptionEventTypes;
                return Tuple.Create(-3, dict);
            }
            double totalValue = 0;
            StringBuilder pathDataCopy = new StringBuilder(editor.levelData.pathData);
            List<float> angleDataCopy = editor.levelData.angleData.ToList();
            for (int i = floors[0].seqID; i <= floors.Last().seqID - 1; i++)
            {
                scrFloor floor = listFloors[i];
                if (floor.nextfloor == null)
                    continue;
                if (floor.midSpin)
                    continue;
                if (floor.seqID == 0)
                    continue;
                double nextAngle = (floor.seqID + 1).GetFloor().GetAngle();
                double angleLength = floor.GetAngleLength();
                double value = ((angleLength / multiplier - angleLength) * (floor.isCCW ? -1 : 1) * (editor.levelData.isOldLevel ? 1 : -1)).FixAngle();
                totalValue = (totalValue + value).FixAngle();
                float applyAngle = (float)(nextAngle + totalValue).FixAngle();
                if (angleLength / multiplier > 360)
                    if (angleCorrection == null)
                    {
                        Dictionary<string, object> dict = new Dictionary<string, object>();
                        dict["floor"] = floor.seqID;
                        return Tuple.Create(-4, dict);
                    }
                    else
                    {
                        LevelEvent ev = new LevelEvent(floor.seqID, LevelEventType.Pause);
                        ev.data["duration"] = (float)((int)(angleLength / multiplier / 360) + 1);
                        ev.data["angleCorrectionDir"] = angleCorrection.Value;
                        scnEditor.instance.events.Add(ev);
                    }
                if (editor.levelData.isOldLevel)
                {
                    if (applyAngle % 15 != 0)
                    {
                        Dictionary<string, object> dict = new Dictionary<string, object>();
                        dict["floor"] = floor.seqID + 1;
                        dict["angle"] = nextAngle;
                        dict["changedAngle"] = applyAngle;
                        return Tuple.Create(-5, dict);
                    }
                    pathDataCopy.Remove(floor.seqID, 1);
                    pathDataCopy.Insert(floor.seqID, pathData[(int)applyAngle / 15]);
                }
                else
                {
                    angleDataCopy.RemoveAt(floor.seqID);
                    angleDataCopy.Insert(floor.seqID, applyAngle);
                }
            }
            editor.levelData.pathData = pathDataCopy.ToString();
            editor.levelData.angleData = angleDataCopy;
            editor.RemakePath(true);
            return Tuple.Create<int, Dictionary<string, object>>(1, null);
        }

        public static double GetAngleLength(this scrFloor floor, bool? ccw = null)
        {
            double angle = floor.GetAngle();
            if (CustomLevel.instance.levelData.isOldLevel)
            {
                angle = ccw ?? floor.isCCW
                    ? angle - floor.nextfloor.GetAngle()
                    : -angle + floor.nextfloor.GetAngle();
            }
            else
            {
                angle = !(ccw ?? floor.isCCW)
                    ? angle - floor.nextfloor.GetAngle()
                    : -angle + floor.nextfloor.GetAngle();
            }
            angle += 180;
            if (floor.numPlanets > 2 && floor.prevfloor && !floor.prevfloor.midSpin)
                angle -= 180f * (floor.numPlanets - 2) / floor.numPlanets;
            angle = angle.FixAngle();
            angle = angle == 0 ? 360 : angle;
            return angle;
        }

        public static double GetAngle(this scrFloor floor)
        {
            double angle;
            if (scnEditor.instance.levelData.isOldLevel)
            {
                if (floor.seqID == 0)
                    return 90;
                if ((floor.seqID - 1).PathData() == '!')
                    return ((floor.seqID - 1).GetFloor().GetAngle() + 180).FixAngle();
                angle = floor.DefaultAngle();
                if (angle == -1)
                {
                    scrFloor prev = (floor.seqID - 1).GetFloor();
                    angle = (floor.seqID - 1).PathData() == '7'
                        ? prev.GetAngle() - 180 + 900 / 7
                        : prev.GetAngle() - 180 + 108;
                }
            }
            else
            {
                if (floor.seqID == 0)
                    return 0;
                if ((floor.seqID - 1).GetFloor().midSpin)
                    return ((floor.seqID - 1).GetFloor().GetAngle() + 180).FixAngle();
                angle = (floor.seqID - 1).AngleData();
            }
            return angle.FixAngle();
        }

        public static char PathData(this int id)
        {
            return scnEditor.instance.levelData.pathData[id];
        }

        public static double AngleData(this int id)
        {
            return scnEditor.instance.levelData.angleData[id].FixAngle();
        }

        public static double FixAngle(this float angle)
        {
            if (angle == 999)
                return angle;
            while (angle >= 360)
                angle -= 360;
            while (angle < 0)
                angle += 360;
            return angle;
        }

        public static double FixAngle(this double angle)
        {
            if (angle == 999)
                return angle;
            while (angle >= 360)
                angle -= 360;
            while (angle < 0)
                angle += 360;
            return angle;
        }

        public static scrFloor GetFloor(this int id)
        {
            return id < 0 || id >= scrLevelMaker.instance.listFloors.Count ? null : scrLevelMaker.instance.listFloors[id];
        }

        private static string pathData = "UoTEJpRAMCBYDVFZNxLWHQGq";

        public static double DefaultAngle(this scrFloor floor)
        {
            return pathData.IndexOf((floor.seqID - 1).PathData()) * 15;
        }
    }
}
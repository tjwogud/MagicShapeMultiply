using ADOFAI;
using System.Collections.Generic;
using System.Linq;

namespace MagicShapeMultiply
{
    public static class MagicShape
    {
        public static void Multiply()
        {
            List<scrFloor> floors = scnEditor.instance.customLevel.levelMaker.listFloors;
            List<LevelEvent> events = scnEditor.instance.events;
            List<scrFloor> selected = scnEditor.instance.selectedFloors;
            if (selected == null || selected.Count <= 1)
                return;
            float bpm = scnEditor.instance.customLevel.levelData.bpm;
            float prevBpm = bpm;
            for (int i = 0; i <= selected[0].seqID; i++)
            {
                scrFloor floor = floors[i];
                if (floor.nextfloor == null)
                    continue;
                LevelEvent speed = events.Find(e => e.floor == i && e.eventType == LevelEventType.SetSpeed);
                if (speed != null)
                    if ((SpeedType)speed.data["speedType"] == SpeedType.Bpm)
                        bpm = speed.GetFloat("beatsPerMinute");
                    else
                        bpm *= speed.GetFloat("bpmMultiplier");
                if (i != selected[0].seqID)
                    prevBpm = bpm;
            }
            scnEditor.instance.events.RemoveAll(e
                => e.floor >= selected[0].seqID
                && e.floor <= selected.Last().seqID
                && (e.eventType == LevelEventType.SetSpeed
                || e.eventType == LevelEventType.Twirl));
            scnEditor.instance.ApplyEventsToFloors();
            bool ccw = selected[0].isCCW;
            for (int i = selected[0].seqID; i <= selected.Last().seqID; i++)
            {
                scrFloor floor = floors[i];
                if (floor.nextfloor == null)
                    continue;
                if (floor.midSpin)
                {
                    Main.Logger.Log($"{floor.seqID} is midspin");
                    continue;
                }
                double angle = GetAngleLength(floor, ccw);
                if (Main.Settings.EnableInOrOut && angle % 180 != 0)
                    if (Main.Settings.InOrOut == angle > 180)
                    {
                        int eventFloor = floor.seqID;
                        if (floor.seqID > 0)
                            while (Midspin(eventFloor - 1))
                                eventFloor--;
                        scnEditor.instance.AddEvent(eventFloor, LevelEventType.Twirl);
                        ccw = !ccw;
                    }
            }
            scnEditor.instance.ApplyEventsToFloors();
            for (int i = selected[0].seqID; i <= selected.Last().seqID; i++)
            {
                scrFloor floor = floors[i];
                if (floor.nextfloor == null)
                    continue;
                if (floor.midSpin)
                    continue;
                float angle = GetAngleLength(floor, floor.isCCW);
                float applyBpm = bpm * angle / 180;
                decimal minus = (decimal)applyBpm - (decimal)prevBpm;
                if (!(minus >= 0 && minus <= (decimal)0.001))
                {
                    int eventFloor = floor.seqID;
                    if (floor.seqID > 0)
                        while (Midspin(eventFloor - 1))
                            eventFloor--;
                    LevelEvent item = new LevelEvent(eventFloor, LevelEventType.SetSpeed);
                    if (Main.Settings.MultiplyOrBPM)
                    {
                        item.data["speedType"] = SpeedType.Multiplier;
                        item.data["bpmMultiplier"] = (float)(applyBpm / prevBpm);
                    }
                    else
                    {
                        item.data["speedType"] = SpeedType.Bpm;
                        item.data["beatsPerMinute"] = (float)applyBpm;
                    }
                    if (Main.Settings.ShowEvent)
                        scnEditor.instance.events.Insert(0, item);
                    else
                        scnEditor.instance.events.Add(item);
                    prevBpm = applyBpm;
                }
            }
            Patch.first = selected[0].seqID;
            Patch.last = selected.Last().seqID;
            scnEditor.instance.RemakePath(true);
        }

        public static float GetAngleLength(scrFloor floor, bool ccw)
        {
            float angle;
            if (floor.customLevel.levelData.isOldLevel)
            {
                angle = GetAngle(floor);
                angle = ccw
                    ? angle - GetAngle(floor.nextfloor)
                    : -angle + GetAngle(floor.nextfloor);
            }
            else
            {
                angle = GetFreeAngle(floor);
                angle = !ccw
                    ? angle - GetFreeAngle(floor.nextfloor)
                    : -angle + GetFreeAngle(floor.nextfloor);
                angle = angle.FixAngle();
            }
            angle = (angle + 540) % 360;
            angle = angle == 0 ? 360 : angle;
            return angle;
        }

        public static float GetFreeAngle(scrFloor floor)
        {
            if (floor.seqID == 0)
                return 0;
            if (Midspin(floor.seqID - 1))
                return (GetFreeAngle(floor.GetFloor(floor.seqID - 1)) + 180) % 360;
            float angle = AngleData(floor.seqID - 1);
            return angle % 360;
        }

        public static float GetAngle(scrFloor floor)
        {
            if (floor.seqID == 0)
                return 90;
            if (PathData(floor.seqID - 1) == '!')
                return (GetAngle(floor.GetFloor(floor.seqID - 1)) + 180) % 360;
            float angle = DefaultAngle(floor);
            scrFloor prev = floor.GetFloor(floor.seqID - 1);
            if (angle == -1)
            {
                angle = PathData(floor.seqID - 1) == '7'
                    ? GetAngle(prev) - 180 + seven
                    : GetAngle(prev) - 180 + five;
            }
            return angle % 360;
        }

        public static float seven = 900 / 7;
        public static float five = 108;

        public static bool Midspin(int id)
        {
            if (scnEditor.instance.customLevel.levelMaker.isOldLevel)
                return PathData(id) == '!';
            else
                return AngleData(id) == 999;
        }

        public static char PathData(int id)
        {
            return scnEditor.instance.customLevel.levelData.pathData[id];
        }

        public static float AngleData(int id)
        {
            return scnEditor.instance.customLevel.levelData.angleData[id].FixAngle();
        }

        public static float FixAngle(this float angle)
        {
            if (angle == 999)
                return angle;
            while (angle >= 360)
                angle -= 360;
            while (angle < 0)
                angle += 360;
            return angle;
        }

        public static scrFloor GetFloor(this scrFloor floor, int id)
        {
            return floor.customLevel.levelMaker.listFloors[id];
        }

        public static float DefaultAngle(scrFloor floor)
        {
            switch (PathData(floor.seqID - 1))
            {
                case 'U':
                    return 0;
                case 'o':
                    return 15;
                case 'T':
                    return 30;
                case 'E':
                    return 45;
                case 'J':
                    return 60;
                case 'p':
                    return 75;
                case 'R':
                    return 90;
                case 'A':
                    return 105;
                case 'M':
                    return 120;
                case 'C':
                    return 135;
                case 'B':
                    return 150;
                case 'Y':
                    return 165;
                case 'D':
                    return 180;
                case 'V':
                    return 195;
                case 'F':
                    return 210;
                case 'Z':
                    return 225;
                case 'N':
                    return 240;
                case 'x':
                    return 255;
                case 'L':
                    return 270;
                case 'W':
                    return 285;
                case 'H':
                    return 300;
                case 'Q':
                    return 315;
                case 'G':
                    return 330;
                case 'q':
                    return 345;
            }
            return -1;
        }
    }
}
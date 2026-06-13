using System;
using UnityEngine;

namespace LasDetox.Schedule
{
    [Serializable]
    public class DailyScheduleEntry
    {
        public int Hour;
        public int Minute;
        public string DisplayName;
        public string Description;
        public ScheduleEventType EventType;
        public int DurationMinutes;

        public bool IsPointEvent => DurationMinutes == 0;
        public bool IsActivity => DurationMinutes > 0;

        public int StartMinutes => Hour * 60 + Minute;

        public string TimeLabel => $"{Hour:00}:{Minute:00}";
    }
}

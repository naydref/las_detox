using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LasDetox.Schedule
{
    [CreateAssetMenu(fileName = "DailySchedule", menuName = "Las Detox/Schedule/Daily Schedule")]
    public class DailySchedule : ScriptableObject
    {
        public List<DailyScheduleEntry> Entries = new();

        public IReadOnlyList<DailyScheduleEntry> GetEntriesSorted()
        {
            return Entries
                .OrderBy(e => e.StartMinutes)
                .ToList();
        }

        private void OnValidate()
        {
            if (Entries == null)
                return;

            var seen = new HashSet<(int, int)>();

            for (var i = 0; i < Entries.Count; i++)
            {
                var entry = Entries[i];
                if (entry == null)
                    continue;

                if (entry.Hour is < 0 or > 23 || entry.Minute is < 0 or > 59)
                {
                    Debug.LogWarning(
                        $"[{name}] Entry {i} ({entry.DisplayName}) has invalid time {entry.Hour:00}:{entry.Minute:00}.",
                        this);
                }

                if (entry.DurationMinutes < 0)
                {
                    Debug.LogWarning(
                        $"[{name}] Entry {i} ({entry.DisplayName}) has negative duration.",
                        this);
                }

                var key = (entry.Hour, entry.Minute);
                if (!seen.Add(key))
                {
                    Debug.LogWarning(
                        $"[{name}] Duplicate schedule time {entry.Hour:00}:{entry.Minute:00} at entry {i}.",
                        this);
                }
            }

            WarnOverlappingActivities();
        }

        private void WarnOverlappingActivities()
        {
            var activities = Entries
                .Where(e => e != null && e.IsActivity)
                .OrderBy(e => e.StartMinutes)
                .ToList();

            for (var i = 0; i < activities.Count; i++)
            {
                for (var j = i + 1; j < activities.Count; j++)
                {
                    if (ActivitiesOverlap(activities[i], activities[j]))
                    {
                        Debug.LogWarning(
                            $"[{name}] Overlapping activities: {activities[i].DisplayName} and {activities[j].DisplayName}.",
                            this);
                    }
                }
            }
        }

        private static bool ActivitiesOverlap(DailyScheduleEntry a, DailyScheduleEntry b)
        {
            return IsMinuteWithinActivity(a.StartMinutes, b)
                || IsMinuteWithinActivity(b.StartMinutes, a);
        }

        internal static bool IsMinuteWithinActivity(int minuteOfDay, DailyScheduleEntry entry)
        {
            if (entry == null || !entry.IsActivity)
                return false;

            var start = entry.StartMinutes;
            var end = start + entry.DurationMinutes;

            if (end <= 1440)
                return minuteOfDay >= start && minuteOfDay < end;

            var wrappedEnd = end % 1440;
            return minuteOfDay >= start || minuteOfDay < wrappedEnd;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using LasDetox.Time;
using UnityEngine;

namespace LasDetox.Schedule
{
    public class ScheduleRunner : MonoBehaviour
    {
        [SerializeField] private GameClock _gameClock;
        [SerializeField] private DailySchedule _schedule;
        [SerializeField] private bool _logTransitions = true;

        private IReadOnlyList<DailyScheduleEntry> _sortedEntries = Array.Empty<DailyScheduleEntry>();

        private int _lastEmitDay = -1;
        private int _lastEmitHour = -1;
        private int _lastEmitMinute = -1;
        private int _lastEmitEntryIndex = -1;

        public DailyScheduleEntry CurrentActivity { get; private set; }
        public DailyScheduleEntry NextEntry { get; private set; }
        public DailyScheduleEntry LastPointEvent { get; private set; }

        public event Action<DailyScheduleEntry> OnScheduleEntryStarted;

        private void OnEnable()
        {
            if (_gameClock != null)
                _gameClock.OnMinuteChanged += HandleMinuteChanged;
        }

        private void OnDisable()
        {
            if (_gameClock != null)
                _gameClock.OnMinuteChanged -= HandleMinuteChanged;
        }

        private void Start()
        {
            RefreshSortedEntries();
            EvaluateAtTime(_gameClock.Day, _gameClock.Hour, _gameClock.Minute, emitIfExactStart: true);
        }

        private void HandleMinuteChanged(int day, int hour, int minute)
        {
            EvaluateAtTime(day, hour, minute, emitIfExactStart: true);
        }

        private void RefreshSortedEntries()
        {
            _sortedEntries = _schedule != null
                ? _schedule.GetEntriesSorted()
                : Array.Empty<DailyScheduleEntry>();
        }

        private void EvaluateAtTime(int day, int hour, int minute, bool emitIfExactStart)
        {
            if (_schedule == null || _gameClock == null)
                return;

            RefreshSortedEntries();

            var minuteOfDay = hour * 60 + minute;

            if (emitIfExactStart)
            {
                for (var i = 0; i < _sortedEntries.Count; i++)
                {
                    var entry = _sortedEntries[i];
                    if (entry.IsPointEvent && entry.Hour == hour && entry.Minute == minute)
                    {
                        if (TryEmitEntry(entry, day, hour, minute, i))
                            LastPointEvent = entry;
                    }
                }
            }

            CurrentActivity = ResolveCurrentActivity(minuteOfDay);

            if (emitIfExactStart)
            {
                for (var i = 0; i < _sortedEntries.Count; i++)
                {
                    var entry = _sortedEntries[i];
                    if (entry.IsActivity && entry.Hour == hour && entry.Minute == minute)
                        TryEmitEntry(entry, day, hour, minute, i);
                }
            }

            NextEntry = ResolveNextEntry(minuteOfDay);
        }

        private DailyScheduleEntry ResolveCurrentActivity(int minuteOfDay)
        {
            var overlapping = _sortedEntries
                .Where(e => e.IsActivity && DailySchedule.IsMinuteWithinActivity(minuteOfDay, e))
                .ToList();

            if (overlapping.Count == 0)
                return null;

            if (overlapping.Count > 1)
            {
                Debug.LogWarning(
                    $"[{nameof(ScheduleRunner)}] Multiple overlapping activities at {minuteOfDay / 60:00}:{minuteOfDay % 60:00}. Using latest start.");
            }

            return overlapping.OrderByDescending(e => e.StartMinutes).First();
        }

        private DailyScheduleEntry ResolveNextEntry(int minuteOfDay)
        {
            DailyScheduleEntry best = null;
            var bestDelta = int.MaxValue;

            foreach (var entry in _sortedEntries)
            {
                var delta = entry.StartMinutes - minuteOfDay;
                if (delta <= 0)
                    delta += 1440;

                if (delta < bestDelta)
                {
                    bestDelta = delta;
                    best = entry;
                }
            }

            return best;
        }

        private bool TryEmitEntry(DailyScheduleEntry entry, int day, int hour, int minute, int entryIndex)
        {
            if (_lastEmitDay == day
                && _lastEmitHour == hour
                && _lastEmitMinute == minute
                && _lastEmitEntryIndex == entryIndex)
            {
                return false;
            }

            _lastEmitDay = day;
            _lastEmitHour = hour;
            _lastEmitMinute = minute;
            _lastEmitEntryIndex = entryIndex;

            if (_logTransitions)
            {
                var kind = entry.IsActivity ? "Activity" : "Point";
                Debug.Log($"[ScheduleRunner] {kind} started: {entry.DisplayName} ({entry.TimeLabel})");
            }

            OnScheduleEntryStarted?.Invoke(entry);
            return true;
        }
    }
}

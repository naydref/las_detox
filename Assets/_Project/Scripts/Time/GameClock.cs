using System;
using UnityEngine;

namespace LasDetox.Time
{
    public class GameClock : MonoBehaviour
    {
        public const float RealSecondsPerGameMinute = 1f;

        [SerializeField] private int _day = 1;
        [SerializeField] private int _hour = 6;
        [SerializeField] private int _minute;
        [SerializeField] private bool _isPaused;
        [SerializeField] private int _timeScale = 1;

        private float _accumulator;

        public int Day => _day;
        public int Hour => _hour;
        public int Minute => _minute;
        public bool IsPaused => _isPaused;
        public int TimeScale => _timeScale;

        public event Action<int, int, int> OnMinuteChanged;
        public event Action<int, int> OnHourChanged;

        private void Awake()
        {
            ClampTimeScale();
        }

        private void Update()
        {
            if (_isPaused || _timeScale <= 0)
                return;

            _accumulator += UnityEngine.Time.deltaTime;
            var threshold = RealSecondsPerGameMinute / _timeScale;

            while (_accumulator >= threshold)
            {
                _accumulator -= threshold;
                AdvanceMinute();
            }
        }

        public void SetPaused(bool paused)
        {
            _isPaused = paused;
        }

        public void TogglePaused()
        {
            _isPaused = !_isPaused;
        }

        public void SetTimeScale(int scale)
        {
            _timeScale = Mathf.Clamp(scale, 1, 4);
            ClampTimeScale();
        }

        public void SetTime(int day, int hour, int minute)
        {
            var previousHour = _hour;

            _day = Mathf.Max(1, day);
            _hour = Mathf.Clamp(hour, 0, 23);
            _minute = Mathf.Clamp(minute, 0, 59);
            _accumulator = 0f;

            OnMinuteChanged?.Invoke(_day, _hour, _minute);

            if (_hour != previousHour)
                OnHourChanged?.Invoke(_day, _hour);
        }

        public void AdvanceHour()
        {
            var totalMinutes = _hour * 60 + _minute + 60;
            var dayOffset = totalMinutes / 1440;
            var minuteOfDay = totalMinutes % 1440;

            _day += dayOffset;
            _hour = minuteOfDay / 60;
            _minute = minuteOfDay % 60;
            _accumulator = 0f;

            OnMinuteChanged?.Invoke(_day, _hour, _minute);
            OnHourChanged?.Invoke(_day, _hour);
        }

        private void AdvanceMinute()
        {
            var previousHour = _hour;

            _minute++;
            if (_minute >= 60)
            {
                _minute = 0;
                _hour++;
            }

            if (_hour >= 24)
            {
                _hour = 0;
                _day++;
            }

            OnMinuteChanged?.Invoke(_day, _hour, _minute);

            if (_hour != previousHour)
                OnHourChanged?.Invoke(_day, _hour);
        }

        private void ClampTimeScale()
        {
            if (_timeScale != 1 && _timeScale != 2 && _timeScale != 4)
                _timeScale = 1;
        }
    }
}

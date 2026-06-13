using LasDetox.Schedule;
using LasDetox.Time;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace LasDetox.Debugging
{
    public class ScheduleDebugPanel : MonoBehaviour
    {
        [SerializeField] private GameClock _gameClock;
        [SerializeField] private ScheduleRunner _scheduleRunner;

        private Text _dayTimeText;
        private Text _currentActivityText;
        private Text _nextEntryText;
        private Text _lastPointEventText;
        private Button _pauseButton;

        private void Awake()
        {
            EnsureEventSystem();
            BuildUiIfNeeded();
        }

        private void OnEnable()
        {
            if (_gameClock != null)
                _gameClock.OnMinuteChanged += HandleClockChanged;

            if (_scheduleRunner != null)
                _scheduleRunner.OnScheduleEntryStarted += HandleEntryStarted;
        }

        private void OnDisable()
        {
            if (_gameClock != null)
                _gameClock.OnMinuteChanged -= HandleClockChanged;

            if (_scheduleRunner != null)
                _scheduleRunner.OnScheduleEntryStarted -= HandleEntryStarted;
        }

        private void Start()
        {
            WireButtons();
            Refresh();
        }

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
                return;

            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<InputSystemUIInputModule>();
        }

        private void BuildUiIfNeeded()
        {
            if (_dayTimeText != null)
                return;

            var canvasGo = new GameObject("DebugCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            var panelGo = CreateUiObject("Panel", canvasGo.transform);
            var panelImage = panelGo.AddComponent<Image>();
            panelImage.color = new Color(0.08f, 0.08f, 0.1f, 0.92f);

            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(16f, -16f);
            panelRect.sizeDelta = new Vector2(420f, 360f);

            var layout = panelGo.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            _dayTimeText = CreateLabel(panelGo.transform, "Day / Time");
            _currentActivityText = CreateLabel(panelGo.transform, "Current Activity");
            _nextEntryText = CreateLabel(panelGo.transform, "Next Entry");
            _lastPointEventText = CreateLabel(panelGo.transform, "Last Point Event");

            var buttonRow = CreateUiObject("Buttons", panelGo.transform);
            var buttonLayout = buttonRow.AddComponent<HorizontalLayoutGroup>();
            buttonLayout.spacing = 6f;
            buttonLayout.childAlignment = TextAnchor.MiddleLeft;
            buttonLayout.childControlWidth = true;
            buttonLayout.childForceExpandWidth = true;

            _pauseButton = CreateButton(buttonRow.transform, "Pause");
            CreateButton(buttonRow.transform, "x1").onClick.AddListener(() => _gameClock.SetTimeScale(1));
            CreateButton(buttonRow.transform, "x2").onClick.AddListener(() => _gameClock.SetTimeScale(2));
            CreateButton(buttonRow.transform, "x4").onClick.AddListener(() => _gameClock.SetTimeScale(4));
            CreateButton(buttonRow.transform, "+1h").onClick.AddListener(() => _gameClock.AdvanceHour());
        }

        private void WireButtons()
        {
            if (_pauseButton != null)
            {
                _pauseButton.onClick.RemoveAllListeners();
                _pauseButton.onClick.AddListener(() =>
                {
                    _gameClock.TogglePaused();
                    Refresh();
                });
            }
        }

        private void HandleClockChanged(int day, int hour, int minute)
        {
            Refresh();
        }

        private void HandleEntryStarted(DailyScheduleEntry entry)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (_gameClock == null || _scheduleRunner == null)
                return;

            _dayTimeText.text =
                $"Day {_gameClock.Day}  |  {_gameClock.Hour:00}:{_gameClock.Minute:00}  |  x{_gameClock.TimeScale}  |  {(_gameClock.IsPaused ? "PAUSED" : "RUNNING")}";

            if (_pauseButton != null)
                _pauseButton.GetComponentInChildren<Text>().text = _gameClock.IsPaused ? "Resume" : "Pause";

            var activity = _scheduleRunner.CurrentActivity;
            _currentActivityText.text = activity == null
                ? "Current Activity: (none)"
                : $"Current Activity: {activity.DisplayName}\n{activity.Description}";

            var next = _scheduleRunner.NextEntry;
            if (next == null)
            {
                _nextEntryText.text = "Next Entry: (none)";
            }
            else
            {
                var kind = next.IsActivity ? "activity" : "point";
                _nextEntryText.text = $"Next Entry: {next.DisplayName} at {next.TimeLabel} ({kind})\n{next.Description}";
            }

            var lastPoint = _scheduleRunner.LastPointEvent;
            _lastPointEventText.text = lastPoint == null
                ? "Last Point Event: (none)"
                : $"Last Point Event: {lastPoint.DisplayName} at {lastPoint.TimeLabel}";
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static Text CreateLabel(Transform parent, string title)
        {
            var go = CreateUiObject(title.Replace(" ", string.Empty), parent);
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.color = Color.white;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = title;
            var layout = go.AddComponent<LayoutElement>();
            layout.minHeight = 48f;
            return text;
        }

        private static Button CreateButton(Transform parent, string label)
        {
            var go = CreateUiObject($"Btn_{label}", parent);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.2f, 0.24f, 0.3f, 1f);

            var button = go.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.3f, 0.36f, 0.45f, 1f);
            colors.pressedColor = new Color(0.15f, 0.18f, 0.24f, 1f);
            button.colors = colors;

            var textGo = CreateUiObject("Text", go.transform);
            var text = textGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = label;

            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var layout = go.AddComponent<LayoutElement>();
            layout.minHeight = 32f;
            layout.flexibleWidth = 1f;

            return button;
        }
    }
}

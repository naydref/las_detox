using System.Collections.Generic;
using LasDetox.CameraSystem;
using LasDetox.Patients;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace LasDetox.UI
{
    public class PatientRosterPanel : MonoBehaviour
    {
        private const string MapName = "GameUI";
        private const string ToggleActionName = "TogglePatientRoster";
        private const string CloseActionName = "ClosePatientRoster";

        [SerializeField] private PatientDataLoader _patientDataLoader;
        [SerializeField] private ManagementCameraController _managementCameraController;
        [SerializeField] private InputActionAsset _inputActions;

        private InputActionMap _gameUiMap;
        private InputAction _toggleAction;
        private InputAction _closeAction;

        private GameObject _rootPanel;
        private Text _warningBanner;
        private Transform _listContent;
        private Text _detailsText;
        private readonly List<Button> _listButtons = new();
        private readonly List<Image> _listRowImages = new();

        private int _selectedIndex = -1;
        private bool _isOpen;

        private void Awake()
        {
            EnsureEventSystem();
            BuildUi();
            SetPanelVisible(false);
        }

        private void Start()
        {
            if (_patientDataLoader == null)
            {
                Debug.LogError(
                    $"{nameof(PatientRosterPanel)} on '{name}': PatientDataLoader reference is not assigned.",
                    this);
                ShowEmptyState();
                return;
            }

            BuildPatientList();
            RefreshWarningBanner();

            if (_patientDataLoader.ValidPatients.Count > 0)
                SelectPatient(0);
            else
                ShowEmptyState();
        }

        private void OnEnable()
        {
            ResolveInputActions();
            SubscribeInput();
            _gameUiMap?.Enable();
        }

        private void OnDisable()
        {
            UnsubscribeInput();
            _gameUiMap?.Disable();

            if (_isOpen)
                ClosePanel();
        }

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
                return;

            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<InputSystemUIInputModule>();
        }

        private void ResolveInputActions()
        {
            if (_inputActions == null)
            {
                Debug.LogWarning(
                    $"{nameof(PatientRosterPanel)} on '{name}': InputActionAsset is not assigned.",
                    this);
                return;
            }

            _gameUiMap = _inputActions.FindActionMap(MapName, throwIfNotFound: false);
            if (_gameUiMap == null)
            {
                Debug.LogWarning(
                    $"{nameof(PatientRosterPanel)} on '{name}': action map '{MapName}' was not found.",
                    this);
                return;
            }

            _toggleAction = _gameUiMap.FindAction(ToggleActionName, throwIfNotFound: false);
            _closeAction = _gameUiMap.FindAction(CloseActionName, throwIfNotFound: false);

            if (_toggleAction == null)
            {
                Debug.LogWarning(
                    $"{nameof(PatientRosterPanel)} on '{name}': action '{ToggleActionName}' was not found.",
                    this);
            }

            if (_closeAction == null)
            {
                Debug.LogWarning(
                    $"{nameof(PatientRosterPanel)} on '{name}': action '{CloseActionName}' was not found.",
                    this);
            }
        }

        private void SubscribeInput()
        {
            if (_toggleAction != null)
                _toggleAction.performed += HandleTogglePerformed;

            if (_closeAction != null)
                _closeAction.performed += HandleClosePerformed;
        }

        private void UnsubscribeInput()
        {
            if (_toggleAction != null)
                _toggleAction.performed -= HandleTogglePerformed;

            if (_closeAction != null)
                _closeAction.performed -= HandleClosePerformed;
        }

        private void HandleTogglePerformed(InputAction.CallbackContext context)
        {
            TogglePanel();
        }

        private void HandleClosePerformed(InputAction.CallbackContext context)
        {
            ClosePanel();
        }

        private void TogglePanel()
        {
            if (_isOpen)
                ClosePanel();
            else
                OpenPanel();
        }

        private void OpenPanel()
        {
            if (_isOpen)
                return;

            _isOpen = true;
            SetPanelVisible(true);

            if (_managementCameraController != null)
                _managementCameraController.SetInputEnabled(false);
        }

        private void ClosePanel()
        {
            if (!_isOpen)
                return;

            _isOpen = false;
            SetPanelVisible(false);

            if (_managementCameraController != null)
                _managementCameraController.SetInputEnabled(true);
        }

        private void SetPanelVisible(bool visible)
        {
            if (_rootPanel != null)
                _rootPanel.SetActive(visible);
        }

        private void BuildPatientList()
        {
            ClearListUi();

            if (_patientDataLoader == null || _patientDataLoader.ValidPatients.Count == 0)
            {
                ShowEmptyState();
                return;
            }

            for (var i = 0; i < _patientDataLoader.ValidPatients.Count; i++)
            {
                var index = i;
                var patient = _patientDataLoader.ValidPatients[i];
                var rowButton = CreateListRowButton(_listContent, PatientDisplayFormatting.FormatListRow(patient));
                rowButton.onClick.AddListener(() => SelectPatient(index));
                _listButtons.Add(rowButton);
                _listRowImages.Add(rowButton.GetComponent<Image>());
            }
        }

        private void SelectPatient(int index)
        {
            if (_patientDataLoader == null ||
                index < 0 ||
                index >= _patientDataLoader.ValidPatients.Count)
            {
                return;
            }

            _selectedIndex = index;
            UpdateRowSelectionVisuals();
            RefreshDetails(_patientDataLoader.ValidPatients[index]);
        }

        private void RefreshDetails(PatientDefinitionDto patient)
        {
            if (_detailsText == null)
                return;

            _detailsText.text = PatientDisplayFormatting.FormatDetails(patient);
        }

        private void RefreshWarningBanner()
        {
            if (_warningBanner == null)
                return;

            var errorCount = _patientDataLoader?.LoadErrors.Count ?? 0;
            if (errorCount <= 0)
            {
                _warningBanner.gameObject.SetActive(false);
                return;
            }

            _warningBanner.gameObject.SetActive(true);
            _warningBanner.text =
                $"Uwaga: nie udało się wczytać części danych pacjentów ({errorCount} błędów). Poprawnie załadowane wpisy są dostępne poniżej.";
        }

        private void ShowEmptyState()
        {
            _selectedIndex = -1;
            ClearListUi();
            CreateEmptyListMessage(_listContent, "Brak pacjentów do wyświetlenia.");

            if (_detailsText != null)
                _detailsText.text = "Brak danych pacjenta.";
        }

        private void ClearListUi()
        {
            _listButtons.Clear();
            _listRowImages.Clear();

            if (_listContent == null)
                return;

            for (var i = _listContent.childCount - 1; i >= 0; i--)
                Destroy(_listContent.GetChild(i).gameObject);
        }

        private void UpdateRowSelectionVisuals()
        {
            for (var i = 0; i < _listRowImages.Count; i++)
            {
                _listRowImages[i].color = i == _selectedIndex
                    ? new Color(0.28f, 0.34f, 0.42f, 1f)
                    : new Color(0.2f, 0.24f, 0.3f, 1f);
            }
        }

        private void BuildUi()
        {
            var canvasGo = new GameObject("PatientRosterCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGo.AddComponent<GraphicRaycaster>();

            _rootPanel = CreateUiObject("RootPanel", canvasGo.transform);
            var panelImage = _rootPanel.AddComponent<Image>();
            panelImage.color = new Color(0.08f, 0.08f, 0.1f, 0.92f);

            var panelRect = _rootPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(900f, 600f);

            var rootLayout = _rootPanel.AddComponent<VerticalLayoutGroup>();
            rootLayout.padding = new RectOffset(16, 16, 16, 16);
            rootLayout.spacing = 10f;
            rootLayout.childAlignment = TextAnchor.UpperLeft;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childForceExpandHeight = false;

            BuildHeader(_rootPanel.transform);
            BuildWarningBanner(_rootPanel.transform);
            BuildBody(_rootPanel.transform);
        }

        private void BuildHeader(Transform parent)
        {
            var headerGo = CreateUiObject("Header", parent);
            var headerLayout = headerGo.AddComponent<HorizontalLayoutGroup>();
            headerLayout.spacing = 8f;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = true;
            headerLayout.childForceExpandHeight = false;

            var titleGo = CreateUiObject("Title", headerGo.transform);
            var titleText = titleGo.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 22;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.text = "Lista pacjentów";

            var titleLayout = titleGo.AddComponent<LayoutElement>();
            titleLayout.flexibleWidth = 1f;
            titleLayout.minHeight = 36f;

            var closeButton = CreateButton(headerGo.transform, "Zamknij");
            closeButton.onClick.AddListener(ClosePanel);
        }

        private void BuildWarningBanner(Transform parent)
        {
            var bannerGo = CreateUiObject("WarningBanner", parent);
            _warningBanner = bannerGo.AddComponent<Text>();
            _warningBanner.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _warningBanner.fontSize = 14;
            _warningBanner.color = new Color(1f, 0.82f, 0.45f, 1f);
            _warningBanner.alignment = TextAnchor.UpperLeft;
            _warningBanner.horizontalOverflow = HorizontalWrapMode.Wrap;
            _warningBanner.verticalOverflow = VerticalWrapMode.Overflow;
            _warningBanner.text = string.Empty;

            var bannerLayout = bannerGo.AddComponent<LayoutElement>();
            bannerLayout.minHeight = 24f;
            bannerGo.SetActive(false);
        }

        private void BuildBody(Transform parent)
        {
            var bodyGo = CreateUiObject("Body", parent);
            var bodyLayout = bodyGo.AddComponent<HorizontalLayoutGroup>();
            bodyLayout.spacing = 12f;
            bodyLayout.childAlignment = TextAnchor.UpperLeft;
            bodyLayout.childControlWidth = true;
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandWidth = true;
            bodyLayout.childForceExpandHeight = true;

            var bodyElement = bodyGo.AddComponent<LayoutElement>();
            bodyElement.flexibleHeight = 1f;
            bodyElement.minHeight = 480f;

            _listContent = CreateListScrollArea(bodyGo.transform);
            _detailsText = CreateDetailsScrollArea(bodyGo.transform);
        }

        private Transform CreateListScrollArea(Transform parent)
        {
            CreateScrollAreaShell(parent, "PatientList", 0.35f, out _, out var contentGo);
            var listLayout = contentGo.AddComponent<VerticalLayoutGroup>();
            listLayout.padding = new RectOffset(4, 4, 4, 4);
            listLayout.spacing = 6f;
            listLayout.childAlignment = TextAnchor.UpperLeft;
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = true;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;

            var listFitter = contentGo.AddComponent<ContentSizeFitter>();
            listFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return contentGo.transform;
        }

        private Text CreateDetailsScrollArea(Transform parent)
        {
            CreateScrollAreaShell(parent, "PatientDetails", 0.65f, out _, out var contentGo);

            var detailsTextGo = CreateUiObject("DetailsText", contentGo.transform);
            var detailsRect = detailsTextGo.GetComponent<RectTransform>();
            detailsRect.anchorMin = new Vector2(0f, 1f);
            detailsRect.anchorMax = new Vector2(1f, 1f);
            detailsRect.pivot = new Vector2(0.5f, 1f);
            detailsRect.anchoredPosition = Vector2.zero;
            detailsRect.sizeDelta = new Vector2(0f, 0f);

            var detailsText = detailsTextGo.AddComponent<Text>();
            detailsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            detailsText.fontSize = 15;
            detailsText.color = Color.white;
            detailsText.alignment = TextAnchor.UpperLeft;
            detailsText.horizontalOverflow = HorizontalWrapMode.Wrap;
            detailsText.verticalOverflow = VerticalWrapMode.Overflow;
            detailsText.text = "Brak danych pacjenta.";

            var detailsFitter = detailsTextGo.AddComponent<ContentSizeFitter>();
            detailsFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var contentFitter = contentGo.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return detailsText;
        }

        private GameObject CreateScrollAreaShell(
            Transform parent,
            string name,
            float widthWeight,
            out ScrollRect scrollRect,
            out GameObject contentGo)
        {
            var scrollGo = CreateUiObject(name, parent);
            var scrollLayout = scrollGo.AddComponent<LayoutElement>();
            scrollLayout.flexibleWidth = widthWeight;
            scrollLayout.minWidth = 200f;

            var scrollBackground = scrollGo.AddComponent<Image>();
            scrollBackground.color = new Color(0.12f, 0.14f, 0.18f, 0.95f);

            scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 24f;

            var viewportGo = CreateUiObject("Viewport", scrollGo.transform);
            var viewportRect = viewportGo.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(8f, 8f);
            viewportRect.offsetMax = new Vector2(-8f, -8f);

            var viewportImage = viewportGo.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
            viewportGo.AddComponent<Mask>().showMaskGraphic = false;

            contentGo = CreateUiObject("Content", viewportGo.transform);
            var contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;

            return scrollGo;
        }

        private Button CreateListRowButton(Transform parent, string label)
        {
            var button = CreateButton(parent, label);
            var text = button.GetComponentInChildren<Text>();
            text.alignment = TextAnchor.UpperLeft;
            text.fontSize = 15;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 8f);
            textRect.offsetMax = new Vector2(-10f, -8f);

            var layout = button.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = 72f;

            return button;
        }

        private static void CreateEmptyListMessage(Transform parent, string message)
        {
            var go = CreateUiObject("EmptyMessage", parent);
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 15;
            text.color = Color.white;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = message;

            var layout = go.AddComponent<LayoutElement>();
            layout.minHeight = 48f;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
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

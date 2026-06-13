using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace LasDetox.CameraSystem
{
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class ManagementCameraController : MonoBehaviour
    {
        private const string MapName = "ManagementCamera";
        private const string PanActionName = "Pan";
        private const string ZoomActionName = "Zoom";
        private const string ResetActionName = "Reset";

        [Header("Initial State")]
        [SerializeField] private Vector3 _initialFocusPoint = new(-7.59f, 0f, 4.42f);
        [SerializeField] private float _initialZoomDistance = 25f;

        [Header("View")]
        [SerializeField] private float _pitchDegrees = 50f;
        [SerializeField] private float _yawDegrees = 0f;
        [SerializeField] private float _fieldOfView = 60f;

        [Header("Movement")]
        [SerializeField] private float _panSpeed = 12f;
        [SerializeField] private float _panSmoothTime = 0.12f;
        [SerializeField] private float _zoomSpeed = 2.5f;
        [SerializeField] private float _zoomSmoothTime = 0.1f;
        [SerializeField] private float _minZoomDistance = 12f;
        [SerializeField] private float _maxZoomDistance = 40f;

        [Header("Bounds")]
        [SerializeField] private Vector2 _boundsMin = new(-22f, -10f);
        [SerializeField] private Vector2 _boundsMax = new(7f, 19f);
        [SerializeField] private float _boundsPadding = 2f;

        [Header("Input")]
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private bool _blockZoomOverUi = true;

        [Header("Debug")]
        [SerializeField] private Vector3 _focusPoint;

        private UnityEngine.Camera _camera;
        private InputActionMap _managementCameraMap;
        private InputAction _panAction;
        private InputAction _zoomAction;
        private InputAction _resetAction;

        private Vector3 _targetFocus;
        private float _zoomDistance;
        private float _targetZoomDistance;
        private Vector3 _panVelocity;
        private float _zoomVelocity;
        private Quaternion _viewRotation;
        private bool _inputEnabled = true;

        private void Awake()
        {
            _camera = GetComponent<UnityEngine.Camera>();
            _camera.orthographic = false;
            _camera.fieldOfView = _fieldOfView;

            _focusPoint = _initialFocusPoint;
            _targetFocus = _initialFocusPoint;
            _zoomDistance = _initialZoomDistance;
            _targetZoomDistance = _initialZoomDistance;

            CacheViewRotation();
            ApplyView();
            ResolveInputActions();
        }

        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
            ApplyInputMapState();
        }

        private void OnEnable()
        {
            if (_resetAction != null)
                _resetAction.performed += HandleResetPerformed;

            ApplyInputMapState();
        }

        private void OnDisable()
        {
            if (_resetAction != null)
                _resetAction.performed -= HandleResetPerformed;

            _managementCameraMap?.Disable();
        }

        private void ApplyInputMapState()
        {
            if (_managementCameraMap == null)
                return;

            if (_inputEnabled && isActiveAndEnabled)
                _managementCameraMap.Enable();
            else
                _managementCameraMap.Disable();
        }

        private void Update()
        {
            if (_panAction == null || _zoomAction == null)
                return;

            HandlePan();
            HandleZoom();
            SmoothTowardTargets();
            ApplyView();
        }

        private void ResolveInputActions()
        {
            if (_inputActions == null)
            {
                Debug.LogWarning(
                    $"{nameof(ManagementCameraController)} on '{name}': InputActionAsset is not assigned.",
                    this);
                return;
            }

            _managementCameraMap = _inputActions.FindActionMap(MapName, throwIfNotFound: false);
            if (_managementCameraMap == null)
            {
                Debug.LogWarning(
                    $"{nameof(ManagementCameraController)} on '{name}': action map '{MapName}' was not found.",
                    this);
                return;
            }

            _panAction = _managementCameraMap.FindAction(PanActionName, throwIfNotFound: false);
            _zoomAction = _managementCameraMap.FindAction(ZoomActionName, throwIfNotFound: false);
            _resetAction = _managementCameraMap.FindAction(ResetActionName, throwIfNotFound: false);

            if (_panAction == null)
                Debug.LogWarning($"{nameof(ManagementCameraController)} on '{name}': action '{PanActionName}' was not found.", this);
            if (_zoomAction == null)
                Debug.LogWarning($"{nameof(ManagementCameraController)} on '{name}': action '{ZoomActionName}' was not found.", this);
            if (_resetAction == null)
                Debug.LogWarning($"{nameof(ManagementCameraController)} on '{name}': action '{ResetActionName}' was not found.", this);
        }

        private void HandlePan()
        {
            var input = _panAction.ReadValue<Vector2>();
            if (input.sqrMagnitude < 0.0001f)
                return;

            var panDirection = MapPanInputToWorld(input);
            _targetFocus += panDirection * (_panSpeed * UnityEngine.Time.deltaTime);
            _targetFocus = ClampFocus(_targetFocus);
        }

        private void HandleZoom()
        {
            if (_blockZoomOverUi && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            var scroll = _zoomAction.ReadValue<float>();
            if (Mathf.Approximately(scroll, 0f))
                return;

            _targetZoomDistance -= scroll * _zoomSpeed;
            _targetZoomDistance = Mathf.Clamp(_targetZoomDistance, _minZoomDistance, _maxZoomDistance);
        }

        private void HandleResetPerformed(InputAction.CallbackContext context)
        {
            _targetFocus = _initialFocusPoint;
            _targetZoomDistance = _initialZoomDistance;
        }

        private void SmoothTowardTargets()
        {
            _focusPoint = Vector3.SmoothDamp(_focusPoint, _targetFocus, ref _panVelocity, _panSmoothTime);
            _zoomDistance = Mathf.SmoothDamp(_zoomDistance, _targetZoomDistance, ref _zoomVelocity, _zoomSmoothTime);
        }

        private Vector3 MapPanInputToWorld(Vector2 input)
        {
            var yawRotation = Quaternion.Euler(0f, _yawDegrees, 0f);
            var forward = yawRotation * Vector3.forward;
            var right = yawRotation * Vector3.right;
            return (forward * input.y + right * input.x).normalized;
        }

        private Vector3 ClampFocus(Vector3 focus)
        {
            var minX = _boundsMin.x + _boundsPadding;
            var maxX = _boundsMax.x - _boundsPadding;
            var minZ = _boundsMin.y + _boundsPadding;
            var maxZ = _boundsMax.y - _boundsPadding;

            focus.x = Mathf.Clamp(focus.x, minX, maxX);
            focus.z = Mathf.Clamp(focus.z, minZ, maxZ);
            focus.y = _initialFocusPoint.y;
            return focus;
        }

        private void CacheViewRotation()
        {
            _viewRotation = Quaternion.Euler(_pitchDegrees, _yawDegrees, 0f);
        }

        private void ApplyView()
        {
            CacheViewRotation();
            transform.rotation = _viewRotation;
            transform.position = _focusPoint + _viewRotation * Vector3.back * _zoomDistance;
        }

        private void OnDrawGizmosSelected()
        {
            var minX = _boundsMin.x + _boundsPadding;
            var maxX = _boundsMax.x - _boundsPadding;
            var minZ = _boundsMin.y + _boundsPadding;
            var maxZ = _boundsMax.y - _boundsPadding;
            var y = _initialFocusPoint.y;

            var corners = new[]
            {
                new Vector3(minX, y, minZ),
                new Vector3(maxX, y, minZ),
                new Vector3(maxX, y, maxZ),
                new Vector3(minX, y, maxZ),
            };

            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
            for (var i = 0; i < corners.Length; i++)
                Gizmos.DrawLine(corners[i], corners[(i + 1) % corners.Length]);

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(Application.isPlaying ? _focusPoint : _initialFocusPoint, 0.35f);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using TMPro;
using TutorialFramework;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
#endif

public class SmoothCameraMovement : MonoBehaviour
{
    [Header("Camera Positions")]
    [SerializeField] private List<Transform> cameraPositions = new List<Transform>();

    [Header("Camera Buttons")]
    [SerializeField] private List<ButtonSelection> cameraButtons = new List<ButtonSelection>();

    [System.Serializable]
    private struct CatalogPositionOverride
    {
        public int catalogIndex;
        public int positionIndex;
    }

    [SerializeField] private List<CatalogPositionOverride> catalogPositionOverrides = new List<CatalogPositionOverride>();

    [Header("Camera Number")]
    [SerializeField] private TMP_Text cameraNumberText;

    [Header("Movement Settings")]
    [SerializeField] private float moveDuration = 1f;

    [Header("Mouse navigation")]
    [SerializeField, Range(1f, 179f)] private float minimumFieldOfView = 20f;
    [SerializeField, Range(1f, 179f)] private float maximumFieldOfView = 80f;
    [SerializeField, Min(.1f)] private float wheelZoomSpeed = 6f;
    [SerializeField, Min(0f)] private float maximumPanDistance = 25f;

    [SerializeField, Range(.05f, 2f)] private float panSensitivity = .5f;
    [SerializeField, Min(0f)] private float dragThresholdPixels = 8f;
    private Vector2 dragStartPosition;
    private bool dragActivated;

    private Camera viewCamera;
    private float defaultFieldOfView, panDepth;
    private Vector3 panOrigin;
    private Vector2 panOffset, lastMousePosition;
    private bool dragging, resetManualView, leftPanAllowed;
    private PointerEventData pointerEvent;
    private EventSystem pointerEventSystem;
    private readonly List<RaycastResult> pointerHits = new List<RaycastResult>();
#if ENABLE_INPUT_SYSTEM
    private int mouseGestureButton = -1, mouseGestureStartButton;
    private Vector2 mouseGestureStart, mouseGesturePosition;
    private bool mouseGestureBegan, mouseGestureEnded;
#endif

    private bool CanPan => InteractionManager.Instance != null && InteractionManager.Instance.Free;
    private bool CanZoom => CanPan;

    private bool isMoving;
    private Transform movementTarget;
    private Vector3 startPosition, targetPosition;
    private Quaternion startRotation, targetRotation;
    private float elapsedTime;

    internal IReadOnlyList<Transform> CameraPoses => cameraPositions;

    private void Awake()
    {
        viewCamera = GetComponent<Camera>();
        if (viewCamera != null) defaultFieldOfView = viewCamera.fieldOfView;
        panOrigin = transform.position;
    }

    private void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM
        InputSystem.onEvent += CaptureMouseGesture;
#endif
    }

    public void MoveToPosition(int index) => MoveToPosition(index, index);

    public void MoveToCatalogPosition(int catalogIndex)
    {
        foreach (var entry in catalogPositionOverrides)
        {
            if (entry.catalogIndex != catalogIndex) continue;
            MoveToPosition(entry.positionIndex, catalogIndex);
            return;
        }

        MoveToPosition(catalogIndex);
    }

    private void MoveToPosition(int index, int displayIndex)
    {
        if (index < 0 || index >= cameraPositions.Count)
        {
            Debug.LogWarning("Camera position index is out of range: " + index);
            return;
        }

        if (cameraPositions[index] == null)
        {
            Debug.LogWarning("Camera position at index " + index + " is not assigned.");
            return;
        }

        // Requests can come from buttons, keyboard input or storyboard events.
        // Only LateUpdate applies the camera pose, after normal frame updates.
        movementTarget = cameraPositions[index];
        startPosition = transform.position;
        startRotation = transform.rotation;
        targetPosition = movementTarget.position;
        targetRotation = movementTarget.rotation;
        elapsedTime = 0f;
        isMoving = true;
        resetManualView = true;
        CancelMouseGesture();

        // Highlight correct camera button
        if (displayIndex < cameraButtons.Count &&
            cameraButtons[displayIndex] != null)
        {
            ButtonSelection.SelectSpecific(
                cameraButtons[displayIndex]
            );
        }

        // Update camera number text (01, 02, 03...)
        if (cameraNumberText != null)
        {
            cameraNumberText.text = (displayIndex + 1).ToString("D2");
        }
    }

    private void LateUpdate()
    {
        if (resetManualView)
        {
            resetManualView = false;
            panOffset = Vector2.zero;
            panOrigin = transform.position;
            if (viewCamera != null) viewCamera.fieldOfView = defaultFieldOfView;
        }

        if (!isMoving)
        {
            UpdateMouseNavigation();
            return;
        }

        if (movementTarget == null)
        {
            isMoving = false;
            return;
        }

        if (moveDuration <= 0f)
        {
            transform.SetPositionAndRotation(movementTarget.position, movementTarget.rotation);
            panOrigin = transform.position;
            isMoving = false;
            movementTarget = null;
            return;
        }

        elapsedTime += Time.deltaTime;
        float t = Mathf.Clamp01(elapsedTime / moveDuration);
        t = Mathf.SmoothStep(0f, 1f, t);
        transform.SetPositionAndRotation(
            Vector3.Lerp(startPosition, targetPosition, t),
            Quaternion.Slerp(startRotation, targetRotation, t)
        );

        if (elapsedTime >= moveDuration)
        {
            transform.SetPositionAndRotation(movementTarget.position, movementTarget.rotation);
            panOrigin = transform.position;
            isMoving = false;
            movementTarget = null;
        }
    }

    private void OnDisable()
    {
        isMoving = false;
#if ENABLE_INPUT_SYSTEM
        InputSystem.onEvent -= CaptureMouseGesture;
#endif
        CancelMouseGesture();
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused)
        {
            CancelMouseGesture();
        }
    }

    private void UpdateMouseNavigation()
    {
        if (viewCamera == null || !Application.isFocused || (!CanPan && !CanZoom))
        {
            dragging = false;
            leftPanAllowed = false;
            return;
        }

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            ProcessBufferedMouse();
            return;
        }
#endif

        Vector2 mouse = Input.mousePosition;
        bool auxiliaryPressed = Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2);
        bool auxiliaryHeld = Input.GetMouseButton(1) || Input.GetMouseButton(2);
        bool leftPressed = Input.GetMouseButtonDown(0);
        bool leftHeld = Input.GetMouseButton(0);
        float wheel = Input.mouseScrollDelta.y;
        if (!leftHeld) leftPanAllowed = false;
        if (!auxiliaryHeld && !leftHeld && wheel == 0f) { dragging = false; return; }
        bool overUI = IsPointerOverUI(mouse);
        // A left gesture that starts on a wire, device or UI control belongs
        // entirely to that control. Empty-space dragging can pan the scene.
        if (leftPressed)
            leftPanAllowed = CanPan && !overUI && !IsPointerOverWorldInteraction(mouse);
        bool held = auxiliaryHeld || (leftHeld && leftPanAllowed);
        bool pressed = auxiliaryPressed || (leftPressed && leftPanAllowed);
        ProcessMouseNavigation(mouse, pressed, held, wheel, overUI);
    }

    private void CancelMouseGesture()
    {
        dragging = false;
        leftPanAllowed = false;
#if ENABLE_INPUT_SYSTEM
        mouseGestureButton = -1;
        mouseGestureBegan = mouseGestureEnded = false;
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private void CaptureMouseGesture(InputEventPtr inputEvent, InputDevice device)
    {
#if UNITY_EDITOR
        if (InputState.currentUpdateType == InputUpdateType.Editor) return;
#endif
        var mouse = device as Mouse;
        if (mouse == null || mouse != Mouse.current || !Application.isFocused || !CanPan || isMoving ||
            (!inputEvent.IsA<StateEvent>() && !inputEvent.IsA<DeltaStateEvent>())) return;
        Vector2 position = mouse.position.ReadValueFromEvent(inputEvent, out var p) ? p : mouse.position.ReadValue();
        bool left = mouse.leftButton.ReadValueFromEvent(inputEvent, out var l) ? l > .5f : mouse.leftButton.isPressed;
        bool right = mouse.rightButton.ReadValueFromEvent(inputEvent, out var r) ? r > .5f : mouse.rightButton.isPressed;
        bool middle = mouse.middleButton.ReadValueFromEvent(inputEvent, out var m) ? m > .5f : mouse.middleButton.isPressed;
        if (mouseGestureButton < 0)
        {
            int button = right ? 1 : middle ? 2 : left ? 0 : -1;
            if (button < 0) return;
            mouseGestureButton = mouseGestureStartButton = button;
            mouseGestureStart = mouseGesturePosition = position;
            mouseGestureBegan = true;
            mouseGestureEnded = false;
        }
        else
        {
            mouseGesturePosition = position;
            if (!(mouseGestureButton == 0 ? left : mouseGestureButton == 1 ? right : middle))
            {
                mouseGestureButton = -1;
                mouseGestureEnded = true;
            }
        }
    }

    private void ProcessBufferedMouse()
    {
        // Preserve press coordinates even if press, movement and release all
        // arrive before one rendered frame. Input callbacks never move the camera.
        Vector2 cursor = Mouse.current.position.ReadValue();
        float wheel = Mouse.current.scroll.ReadValue().y;
        if (InputSystem.settings.scrollDeltaBehavior == InputSettings.ScrollDeltaBehavior.KeepPlatformSpecificInputRange &&
            (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer))
            wheel /= 120f;
        bool gesture = mouseGestureBegan || mouseGestureEnded || mouseGestureButton >= 0;
        if (mouseGestureBegan)
        {
            bool overUI = IsPointerOverUI(mouseGestureStart);
            leftPanAllowed = mouseGestureStartButton != 0 ||
                (!overUI && !IsPointerOverWorldInteraction(mouseGestureStart));
            if (leftPanAllowed)
                ProcessMouseNavigation(mouseGestureStart, true, true, 0f, overUI);
        }
        if (gesture && leftPanAllowed)
        {
            ProcessMouseNavigation(mouseGesturePosition, false, true, wheel, IsPointerOverUI(mouseGesturePosition));
            wheel = 0f;
        }
        if (wheel != 0f)
            ProcessMouseNavigation(cursor, false, false, wheel, IsPointerOverUI(cursor));
        if (!gesture || mouseGestureEnded)
        {
            dragging = false;
            leftPanAllowed = false;
        }
        mouseGestureBegan = mouseGestureEnded = false;
    }
#endif

    private bool IsPointerOverUI(Vector2 mouse)
    {
        var events = EventSystem.current;
        if (events == null) return false;
        if (pointerEvent == null || pointerEventSystem != events)
        {
            pointerEventSystem = events;
            pointerEvent = new PointerEventData(events);
        }
        pointerEvent.position = mouse;
        pointerHits.Clear();
        events.RaycastAll(pointerEvent, pointerHits);
        // Static labels, monitor artwork and the non-scrollable chart are
        // scenery. Only actual controls/lists own the mouse gesture.
        foreach (var hit in pointerHits)
            if (hit.module is GraphicRaycaster &&
                (hit.gameObject.GetComponentInParent<Selectable>() != null ||
                 hit.gameObject.GetComponentInParent<ScrollRect>() != null)) return true;
        return false;
    }

    private bool IsPointerOverWorldInteraction(Vector2 mouse)
    {
        if (!Physics.Raycast(viewCamera.ScreenPointToRay(mouse), out var hit, 1000f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return false;
        return hit.collider.GetComponentInParent<Interaction>() != null ||
            hit.collider.GetComponentInParent<OnMouseDownEvent>() != null ||
            hit.collider.GetComponentInParent<FreeLabWire>() != null ||
            hit.collider.GetComponentInParent<FreeLabControl>() != null;
    }

    private void ProcessMouseNavigation(Vector2 mouse, bool pressed, bool held, float wheel, bool overUI)
    {
        if (viewCamera == null || isMoving || (!CanPan && !CanZoom) ||
            overUI || !viewCamera.pixelRect.Contains(mouse))
        {
            dragging = false;
            return;
        }

        if (CanZoom && wheel != 0f)
            viewCamera.fieldOfView = Mathf.Clamp(viewCamera.fieldOfView - wheel * wheelZoomSpeed,
                minimumFieldOfView, Mathf.Max(minimumFieldOfView, maximumFieldOfView));

        if (!CanPan || !held) { dragging = false; return; }
        // Focus, a transition or UI may consume the initial down frame. A
        // held navigation button starts from its current position, without a jump.
        if (pressed || !dragging)
        {
            dragging = true;
            dragActivated = false;
            dragStartPosition = mouse;
            lastMousePosition = mouse;
            panDepth = Physics.Raycast(transform.position, transform.forward, out var hit, 1000f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                ? Mathf.Clamp(hit.distance, 5f, 500f) : 25f;
            return;
        }
        if (!dragging) return;

        // Ignore tiny movements until the mouse travels past the threshold
        if (!dragActivated)
        {
            if ((mouse - dragStartPosition).sqrMagnitude < dragThresholdPixels * dragThresholdPixels) return;
            dragActivated = true;
            lastMousePosition = mouse; // start panning from here, no jump
            return;
        }

        Vector2 delta = mouse - lastMousePosition;
        lastMousePosition = mouse;
        float unitsPerPixel = 2f * panDepth * Mathf.Tan(viewCamera.fieldOfView * Mathf.Deg2Rad * .5f)
            / Mathf.Max(1f, viewCamera.pixelHeight);
        panOffset = Vector2.ClampMagnitude(panOffset - delta * unitsPerPixel, maximumPanDistance);
        transform.position = panOrigin + transform.right * panOffset.x + transform.up * panOffset.y;
    }
}

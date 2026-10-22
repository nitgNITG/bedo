using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Michsky.UI.Shift
{
    public class GamepadChecker : MonoBehaviour
    {
        [Header("Resources")]
        public VirtualCursor virtualCursor;
        public GameObject virtualCursorHelper;
        public GameObject eventSystem;

        [Header("Settings")]
        [Tooltip("Always update input device. If you turn off this feature, you won't able to change the input device after start, but it might increase the performance.")]
        public bool alwaysSearch = true;

        [Header("Objects")]
        [Tooltip("Objects in this list will be active when gamepad is un-plugged.")]
        public List<GameObject> keyboardObjects = new List<GameObject>();
        [Tooltip("Objects in this list will be active when gamepad is plugged.")]
        public List<GameObject> gamepadObjects = new List<GameObject>();

#if ENABLE_INPUT_SYSTEM
        Vector2 cursorPos;
#else
        Vector3 cursorPos;
#endif

        [HideInInspector] public bool gamepadConnected;
        [HideInInspector] public bool gamepadEnabled;
        [HideInInspector] public bool keyboardEnabled;

        void Start()
        {
            if (virtualCursor == null)
            {
                try
                {
#if UNITY_2023_2_OR_NEWER
                    var vCursor = FindObjectsByType<VirtualCursor>(FindObjectsSortMode.None)[0];
#else
                    var vCursor = (VirtualCursor)GameObject.FindObjectsOfType(typeof(VirtualCursor))[0];
#endif
                    virtualCursor = vCursor;
                }
                catch
                {
                    enabled = false;
                    Debug.LogWarning("<b>[Gamepad Checker]</b> There is no Virtual Cursor component attached.", this);
                    return;
                }
            }

            virtualCursorHelper = virtualCursor.gameObject;

            if (!alwaysSearch)
            {
                enabled = false;
                Debug.Log("<b>[Gamepad Checker]</b> Always Search is off. Input device won't be updated in case of disconnecting/connecting.");
            }
            else
            {
                enabled = true;
                Debug.Log("<b>[Gamepad Checker]</b> Always Search is on. Input device will be updated in case of disconnecting/connecting.");
            }

            RefreshGamepadConnection();

            if (gamepadConnected)
                SwitchToGamepad();
            else
                SwitchToKeyboard();
        }

        void Update()
        {
            RefreshGamepadConnection();

            // If we're currently in gamepad mode but mouse moved => switch to keyboard.
            if (gamepadConnected && gamepadEnabled && !keyboardEnabled && MouseMovedSinceLastStore())
            {
                SwitchToKeyboard();
            }
            // If we're currently in keyboard mode and gamepad "confirm" pressed => switch to gamepad.
            else if (gamepadConnected && !gamepadEnabled && keyboardEnabled && GamepadConfirmPressedThisFrame())
            {
                SwitchToGamepad();
            }
            // If gamepad disconnected while not already on keyboard => switch to keyboard.
            else if (!gamepadConnected && !keyboardEnabled)
            {
                SwitchToKeyboard();
            }
        }

        void RefreshGamepadConnection()
        {
#if ENABLE_INPUT_SYSTEM
            gamepadConnected = Gamepad.all.Count > 0;
#else
            // Legacy: consider connected if ANY joystick name is non-empty.
            gamepadConnected = false;
            string[] names = Input.GetJoystickNames();
            for (int i = 0; i < names.Length; i++)
            {
                if (!string.IsNullOrEmpty(names[i]))
                {
                    gamepadConnected = true;
                    break;
                }
            }
#endif
        }

        bool MouseMovedSinceLastStore()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current == null) return false;
            return Mouse.current.position.ReadValue() != cursorPos;
#else
            return Input.mousePosition != cursorPos;
#endif
        }

        bool GamepadConfirmPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            // "A / Cross" across controllers
            if (Gamepad.all.Count == 0) return false;
            for (int i = 0; i < Gamepad.all.Count; i++)
            {
                if (Gamepad.all[i] != null && Gamepad.all[i].buttonSouth.wasPressedThisFrame)
                    return true;
            }
            return false;
#else
            return Input.GetKeyDown(KeyCode.Joystick1Button0);
#endif
        }

        void StoreCursorPos()
        {
#if ENABLE_INPUT_SYSTEM
            cursorPos = Mouse.current != null ? Mouse.current.position.ReadValue() : cursorPos;
#else
            cursorPos = Input.mousePosition;
#endif
        }

        public void SwitchToGamepad()
        {
            if (virtualCursor == null)
                return;

            for (int i = 0; i < keyboardObjects.Count; i++)
            {
                if (keyboardObjects[i] != null)
                    keyboardObjects[i].SetActive(false);
            }

            for (int i = 0; i < gamepadObjects.Count; i++)
            {
                if (gamepadObjects[i] == null) continue;

                gamepadObjects[i].SetActive(true);

                var rt = gamepadObjects[i].GetComponentInParent<RectTransform>();
                if (rt != null)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            }

            gamepadEnabled = true;
            keyboardEnabled = false;

            virtualCursor.enabled = true;
            if (virtualCursorHelper != null) virtualCursorHelper.SetActive(true);
            if (eventSystem != null) eventSystem.SetActive(false);

            Cursor.visible = false;
            StoreCursorPos();
        }

        public void SwitchToKeyboard()
        {
            if (virtualCursor == null)
                return;

            for (int i = 0; i < gamepadObjects.Count; i++)
            {
                if (gamepadObjects[i] != null)
                    gamepadObjects[i].SetActive(false);
            }

            for (int i = 0; i < keyboardObjects.Count; i++)
            {
                if (keyboardObjects[i] == null) continue;

                keyboardObjects[i].SetActive(true);

                var rt = keyboardObjects[i].GetComponentInParent<RectTransform>();
                if (rt != null)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            }

            gamepadEnabled = false;
            keyboardEnabled = true;

            virtualCursor.enabled = false;
            if (virtualCursorHelper != null) virtualCursorHelper.SetActive(false);
            if (eventSystem != null) eventSystem.SetActive(true);

            Cursor.visible = true;
        }
    }
}
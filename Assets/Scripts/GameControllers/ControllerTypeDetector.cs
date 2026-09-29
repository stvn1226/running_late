using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.XInput;

public enum ControllerType
{
    Keyboard,
    Xbox,
    Playstation,
    Switch,
    Generic,
    Mobile
}

public class ControllerTypeDetector : MonoBehaviour
{
    private static ControllerTypeDetector instance;
    public static ControllerTypeDetector Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ControllerTypeDetector>();
                if (instance == null)
                {
                    GameObject go = new GameObject("ControllerTypeDetector");
                    instance = go.AddComponent<ControllerTypeDetector>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
        private set => instance = value;
    }
    
    public event Action<ControllerType> OnControllerChanged;
    
    [Header("Stability Settings")]
    [SerializeField] private float gracePeriod = 0.4f;
    [SerializeField] private bool enableGracePeriod = true;
    
    private ControllerType currentControllerType = ControllerType.Keyboard;
    private InputDevice lastUsedDevice;
    private PlayerInput playerInput;
    private string lastControlScheme = "Keyboard";
    private float timeSinceLastSwitch = 999f;
    
    public ControllerType CurrentControllerType => currentControllerType;
    public bool IsUsingGamepad => currentControllerType != ControllerType.Keyboard;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        GameLogger.LogInput("ControllerTypeDetector initialized");
    }

    private void OnEnable()
    {
        InputSystem.onActionChange += OnActionChange;
    }

    private void OnDisable()
    {
        InputSystem.onActionChange -= OnActionChange;
    }

    private void Start()
    {
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
        SetControllerType(ControllerType.Mobile);
#endif
        DetectCurrentDevice();
    }

    private void Update()
    {
        timeSinceLastSwitch += Time.unscaledDeltaTime;
        
        if (playerInput == null)
        {
            DetectInputWithoutPlayerInput();
        }
    }

    public void NotifyControlSchemeChanged(string newScheme)
    {
        if (string.IsNullOrEmpty(newScheme) || newScheme == lastControlScheme)
            return;
        
        if (!enableGracePeriod || timeSinceLastSwitch >= gracePeriod)
        {
            GameLogger.LogInput($"Control scheme change accepted: {lastControlScheme} → {newScheme}");
            lastControlScheme = newScheme;
            timeSinceLastSwitch = 0f;
            UpdateControllerTypeFromScheme(newScheme);
        }
        else
        {
            GameLogger.LogInput($"Control scheme change IGNORED: {lastControlScheme} → {newScheme} (cooldown: {timeSinceLastSwitch:F2}s < {gracePeriod}s)");
        }
    }

    private void DetectInputWithoutPlayerInput()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            DetectDeviceFromControl(Keyboard.current);
        }
        else if (Mouse.current != null && (Mouse.current.delta.ReadValue().sqrMagnitude > 0.01f || Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame))
        {
            DetectDeviceFromControl(Mouse.current);
        }
        else if (Gamepad.current != null && IsGamepadActive(Gamepad.current))
        {
            DetectDeviceFromControl(Gamepad.current);
        }
    }

    private static bool IsGamepadActive(Gamepad gamepad)
    {
        if (gamepad == null) return false;
        if (gamepad.leftStick.ReadValue().sqrMagnitude > 0.04f || 
            gamepad.rightStick.ReadValue().sqrMagnitude > 0.04f || 
            gamepad.dpad.ReadValue().sqrMagnitude > 0.04f)
        {
            return true;
        }

        return gamepad.buttonSouth.isPressed || gamepad.buttonNorth.isPressed ||
               gamepad.buttonEast.isPressed || gamepad.buttonWest.isPressed ||
               gamepad.startButton.isPressed || gamepad.selectButton.isPressed ||
               gamepad.leftShoulder.isPressed || gamepad.rightShoulder.isPressed ||
               gamepad.leftTrigger.isPressed || gamepad.rightTrigger.isPressed;
    }

    private void UpdateControllerTypeFromScheme(string scheme)
    {
        GameLogger.LogInput($"Updating controller type from scheme: {scheme}");
        
        if (scheme == "Keyboard")
        {
            SetControllerType(ControllerType.Keyboard);
        }
        else if (scheme == "Gamepad")
        {
            if (Gamepad.current != null)
            {
                DetectDeviceFromControl(Gamepad.current);
            }
            else
            {
                SetControllerType(ControllerType.Generic);
            }
        }
    }

    private void OnActionChange(object obj, InputActionChange change)
    {
        if (change == InputActionChange.ActionPerformed)
        {
            if (obj is InputAction action && action.activeControl != null)
            {
                DetectDeviceFromControl(action.activeControl.device);
            }
        }
    }

    private void DetectCurrentDevice()
    {
        if (playerInput != null)
        {
            UpdateControllerTypeFromScheme(playerInput.currentControlScheme);
        }
        else if (Gamepad.current != null && IsGamepadActive(Gamepad.current))
        {
            DetectDeviceFromControl(Gamepad.current);
            GameLogger.LogInput("Detected gamepad with active input");
        }
        else if (Keyboard.current != null)
        {
            DetectDeviceFromControl(Keyboard.current);
            GameLogger.LogInput("Detected keyboard as default/current device");
        }
    }

    private void DetectDeviceFromControl(InputDevice device)
    {
        if (device == null) return;
        ControllerType newType = GetControllerTypeFromDevice(device);
        if (newType == currentControllerType)
        {
            lastUsedDevice = device;
            return;
        }

        if (enableGracePeriod && timeSinceLastSwitch < gracePeriod)
        {
            return;
        }

        lastUsedDevice = device;
        timeSinceLastSwitch = 0f;
        SetControllerType(newType);
    }

    private void SetControllerType(ControllerType newType)
    {
        if (newType != currentControllerType)
        {
            currentControllerType = newType;
            GameLogger.LogInput($"Controller type changed to: {currentControllerType}");
            OnControllerChanged?.Invoke(currentControllerType);
        }
    }

    private ControllerType GetControllerTypeFromDevice(InputDevice device)
    {
        if (device is Touchscreen)
        {
            return ControllerType.Mobile;
        }
        else if (device is Keyboard || device is Mouse)
        {
            return ControllerType.Keyboard;
        }
        else if (device is DualShockGamepad)
        {
            return ControllerType.Playstation;
        }
        else if (device is XInputController)
        {
            return ControllerType.Xbox;
        }
        else if (device is Gamepad)
        {
            return ControllerType.Generic;
        }
        
        return ControllerType.Keyboard;
    }

    /// <summary>
    /// Forces the controller type to Mobile. Used by MobileInputHandler on mobile builds.
    /// </summary>
    public void SetMobileMode()
    {
        SetControllerType(ControllerType.Mobile);
    }

    public string GetButtonPrompt(string action)
    {
        switch (currentControllerType)
        {
            case ControllerType.Keyboard:
                return GetKeyboardPrompt(action);
            case ControllerType.Xbox:
                return GetXboxPrompt(action);
            case ControllerType.Playstation:
                return GetPlaystationPrompt(action);
            case ControllerType.Switch:
                return GetSwitchPrompt(action);
            default:
                return GetGenericPrompt(action);
        }
    }

    private string GetKeyboardPrompt(string action)
    {
        switch (action.ToLower())
        {
            case "confirm": return "Enter";
            case "cancel": return "Esc";
            case "back": return "Esc";
            default: return action;
        }
    }

    private string GetXboxPrompt(string action)
    {
        switch (action.ToLower())
        {
            case "confirm": return "A";
            case "cancel": return "B";
            case "back": return "B";
            default: return action;
        }
    }

    private string GetPlaystationPrompt(string action)
    {
        switch (action.ToLower())
        {
            case "confirm": return "Cross";
            case "cancel": return "Circle";
            case "back": return "Circle";
            default: return action;
        }
    }

    private string GetSwitchPrompt(string action)
    {
        switch (action.ToLower())
        {
            case "confirm": return "B";
            case "cancel": return "A";
            case "back": return "A";
            default: return action;
        }
    }

    private string GetGenericPrompt(string action)
    {
        switch (action.ToLower())
        {
            case "confirm": return "Button South";
            case "cancel": return "Button East";
            case "back": return "Button East";
            default: return action;
        }
    }

    public void SetPlayerInput(PlayerInput input)
    {
        playerInput = input;
        if (playerInput != null)
        {
            lastControlScheme = playerInput.currentControlScheme;
            GameLogger.LogInput($"PlayerInput assigned: {lastControlScheme}");
            UpdateControllerTypeFromScheme(lastControlScheme);
        }
    }
}

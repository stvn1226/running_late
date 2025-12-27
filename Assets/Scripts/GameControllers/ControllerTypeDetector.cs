using System;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.XInput;
using UnityEngine.InputSystem.Switch;

public enum ControllerType
{
    Keyboard,
    Xbox,
    Playstation,
    Switch,
    Generic
}

public class ControllerTypeDetector : MonoBehaviour
{
    public static ControllerTypeDetector Instance { get; private set; }
    
    public event Action<ControllerType> OnControllerChanged;
    
    [Header("Stability Settings")]
    [SerializeField] private float switchCooldown = 0.8f;
    [SerializeField] private bool enableStabilization = true;
    
    private ControllerType currentControllerType = ControllerType.Keyboard;
    private InputDevice lastUsedDevice;
    private PlayerInput playerInput;
    private string lastControlScheme = "Keyboard";
    private float timeSinceLastSwitch = 999f;
    
    public ControllerType CurrentControllerType => currentControllerType;
    public bool IsUsingGamepad => currentControllerType != ControllerType.Keyboard;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        playerInput = FindObjectOfType<PlayerInput>();
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
        DetectCurrentDevice();
    }

    private void Update()
    {
        timeSinceLastSwitch += Time.unscaledDeltaTime;
        
        if (playerInput != null)
        {
            string currentScheme = playerInput.currentControlScheme;
            
            if (currentScheme != lastControlScheme)
            {
                if (!enableStabilization || timeSinceLastSwitch >= switchCooldown)
                {
                    lastControlScheme = currentScheme;
                    UpdateControllerTypeFromScheme(currentScheme);
                }
            }
        }
    }

    private void UpdateControllerTypeFromScheme(string scheme)
    {
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
        if (!enableStabilization || timeSinceLastSwitch >= switchCooldown)
        {
            if (change == InputActionChange.ActionPerformed)
            {
                if (obj is InputAction action && action.activeControl != null)
                {
                    DetectDeviceFromControl(action.activeControl.device);
                }
            }
        }
    }

    private void DetectCurrentDevice()
    {
        if (playerInput != null)
        {
            UpdateControllerTypeFromScheme(playerInput.currentControlScheme);
        }
        else if (Gamepad.current != null)
        {
            DetectDeviceFromControl(Gamepad.current);
        }
        else if (Keyboard.current != null)
        {
            DetectDeviceFromControl(Keyboard.current);
        }
    }

    private void DetectDeviceFromControl(InputDevice device)
    {
        if (device == lastUsedDevice)
            return;

        lastUsedDevice = device;
        ControllerType newType = GetControllerTypeFromDevice(device);
        SetControllerType(newType);
    }

    private void SetControllerType(ControllerType newType)
    {
        if (newType != currentControllerType)
        {
            currentControllerType = newType;
            timeSinceLastSwitch = 0f;
            GameLogger.LogInput($"Controller type changed to: {currentControllerType}");
            OnControllerChanged?.Invoke(currentControllerType);
        }
    }

    private ControllerType GetControllerTypeFromDevice(InputDevice device)
    {
        if (device is Keyboard || device is Mouse)
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
        else if (device is SwitchProControllerHID)
        {
            return ControllerType.Switch;
        }
        else if (device is Gamepad)
        {
            return ControllerType.Generic;
        }
        
        return ControllerType.Keyboard;
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
            UpdateControllerTypeFromScheme(lastControlScheme);
        }
    }
}


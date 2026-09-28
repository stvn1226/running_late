using UnityEngine;
using UnityEngine.InputSystem;

public class InputModeSwitcher : MonoBehaviour
{
    public PlayerInput playerInput;

    private string lastControlScheme = "Keyboard";
    private Mouse mouse;
    private Keyboard keyboard;

    void Start()
    {
        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>();

        mouse = Mouse.current;
        keyboard = Keyboard.current;

        UpdateCursorState(playerInput.currentControlScheme);
        lastControlScheme = playerInput.currentControlScheme;
        
        if (ControllerTypeDetector.Instance != null)
        {
            ControllerTypeDetector.Instance.SetPlayerInput(playerInput);
            GameLogger.LogInput($"InputModeSwitcher registered PlayerInput with ControllerTypeDetector");
        }
    }

    void Update()
    {
        if (playerInput == null)
            return;

        DetectInputDeviceActivity();

        string currentScheme = playerInput.currentControlScheme;

        if (currentScheme != lastControlScheme)
        {
            UpdateCursorState(currentScheme);
            lastControlScheme = currentScheme;
            
            if (ControllerTypeDetector.Instance != null)
            {
                ControllerTypeDetector.Instance.NotifyControlSchemeChanged(currentScheme);
            }
        }
    }

    private void DetectInputDeviceActivity()
    {
        if (mouse != null && keyboard != null)
        {
            if (mouse.delta.ReadValue().sqrMagnitude > 0.01f || 
                keyboard.anyKey.wasPressedThisFrame)
            {
                if (lastControlScheme == "Gamepad")
                {
                    playerInput.SwitchCurrentControlScheme("Keyboard", mouse, keyboard);
                }
            }
        }

        if (Gamepad.current != null && IsGamepadActive(Gamepad.current))
        {
            if (lastControlScheme != "Gamepad")
            {
                playerInput.SwitchCurrentControlScheme("Gamepad", Gamepad.current);
            }
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

        if (gamepad.buttonSouth.isPressed || gamepad.buttonNorth.isPressed ||
            gamepad.buttonEast.isPressed || gamepad.buttonWest.isPressed ||
            gamepad.startButton.isPressed || gamepad.selectButton.isPressed ||
            gamepad.leftShoulder.isPressed || gamepad.rightShoulder.isPressed ||
            gamepad.leftTrigger.isPressed || gamepad.rightTrigger.isPressed)
        {
            return true;
        }

        return false;
    }

    private void UpdateCursorState(string scheme)
    {
        if (scheme == "Gamepad")
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
}

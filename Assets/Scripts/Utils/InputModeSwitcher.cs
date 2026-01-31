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

        if (Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame)
        {
            if (lastControlScheme != "Gamepad")
            {
                playerInput.SwitchCurrentControlScheme("Gamepad", Gamepad.current);
            }
        }
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

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.XInput;


public enum ControllerType
{
    Xbox,
    Playstation,
    Switch,
    Keyboard
}


public class ControllerTypeDetector : MonoBehaviour
{
    
    
    private void Start()
    {
        var gamepad = Gamepad.current;

        if (gamepad != null)
        {
            if (gamepad is Keyboard)
            {
                GameLogger.LogInput("Keyboard detected.");
            }
            else if (gamepad is DualShockGamepad)
            {
                GameLogger.LogInput("PlayStation gamepad detected.");
            }
            else if (gamepad is XInputController)
            {
                GameLogger.LogInput("Xbox gamepad detected.");
            }
            else
            {
                GameLogger.LogInput($"Generic gamepad detected: {gamepad.displayName}");
            }
        }
        else
        {
            GameLogger.LogInput("No controller connected");
        }
    }
}
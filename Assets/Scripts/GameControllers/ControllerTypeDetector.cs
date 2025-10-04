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
                Debug.Log("Keyboard detected.");
            }
            else if (gamepad is DualShockGamepad)
            {
                Debug.Log("PlayStation gamepad detected.");
            }
            else if (gamepad is XInputController)
            {
                Debug.Log("Xbox gamepad detected.");
            }
            else
            {
                Debug.Log($"Generic gamepad detected: {gamepad.displayName}");
            }
        }
        else
        {
            Debug.Log("No controller connected");
        }
    }
}

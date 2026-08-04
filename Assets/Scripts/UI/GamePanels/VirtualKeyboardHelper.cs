using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public enum KeyboardLayoutType { Qwerty, Numpad }

[RequireComponent(typeof(TMP_InputField))]
public class VirtualKeyboardHelper : MonoBehaviour, ISubmitHandler, IPointerClickHandler, IMoveHandler
{
    [SerializeField] private KeyboardLayoutType layoutType = KeyboardLayoutType.Numpad;
    private TMP_InputField inputField;

    private void Awake()
    {
        inputField = GetComponent<TMP_InputField>();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (ControllerTypeDetector.Instance != null && !ControllerTypeDetector.Instance.IsUsingGamepad)
            return;
            
        ShowVirtualKeyboard();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // A direct pointer click (mouse/touch) implies the user wants to use 
        // the native physical keyboard or native touch keyboard.
        // We only show the custom virtual keyboard on Gamepad 'Submit' (A button).
    }

    public void OnMove(AxisEventData eventData)
    {
        // Bypass TMP_InputField's internal caret navigation and force standard UI navigation
        if (VirtualKeyboardPanel.Instance != null && VirtualKeyboardPanel.Instance.IsOpen)
            return;

        Selectable next = null;
        switch (eventData.moveDir)
        {
            case MoveDirection.Up: next = inputField.FindSelectableOnUp(); break;
            case MoveDirection.Down: next = inputField.FindSelectableOnDown(); break;
            case MoveDirection.Left: next = inputField.FindSelectableOnLeft(); break;
            case MoveDirection.Right: next = inputField.FindSelectableOnRight(); break;
        }

        if (next != null)
        {
            eventData.Use();
            next.Select();
        }
    }

    private void ShowVirtualKeyboard()
    {
        if (VirtualKeyboardPanel.Instance != null && !VirtualKeyboardPanel.Instance.IsOpen)
        {
            VirtualKeyboardPanel.Instance.Show(inputField.text, layoutType, OnKeyboardSubmitted);
        }
    }

    private void OnKeyboardSubmitted(string newText)
    {
        inputField.text = newText;
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class VirtualKeyboardButton : MonoBehaviour
{
    [Header("Key Configuration")]
    [SerializeField] private string character;

    [SerializeField] private bool isBackspace;
    [SerializeField] private bool isConfirm;
    [SerializeField] private bool isCancel;
    [SerializeField] private bool isSpace;
    [SerializeField] private bool isCopy;
    [SerializeField] private bool isPaste;
    [SerializeField] private bool isShift;

    public bool IsShiftKey => isShift;

    [SerializeField] private TMP_Text buttonText;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnKeyClicked);
        if (buttonText == null) buttonText = GetComponentInChildren<TMP_Text>();
    }

    public void SetKey(string keyValue)
    {
        isBackspace = isConfirm = isCancel = isSpace = isCopy = isPaste = isShift = false;
        character = "";

        string displayText = keyValue;

        switch (keyValue.ToUpperInvariant())
        {
            case "BACKSPACE":
            case "DEL":
                isBackspace = true;
                displayText = "DEL";
                break;
            case "ENTER":
            case "OK":
            case "CONFIRM":
                isConfirm = true;
                displayText = "OK";
                break;
            case "CANCEL":
            case "ESC":
                isCancel = true;
                displayText = "ESC";
                break;
            case "SPACE":
                isSpace = true;
                character = " ";
                displayText = "SPACE";
                break;
            case "COPY":
                isCopy = true;
                displayText = "COPY";
                break;
            case "PASTE":
                isPaste = true;
                displayText = "PASTE";
                break;
            case "SHIFT":
                isShift = true;
                displayText = "SHIFT";
                break;
            default:
                character = keyValue;
                displayText = keyValue;
                break;
        }

        if (buttonText == null) buttonText = GetComponentInChildren<TMP_Text>();
        if (buttonText != null)
            buttonText.text = displayText;
    }

    private void OnKeyClicked()
    {
        var panel = VirtualKeyboardPanel.Instance;
        if (panel == null) return;

        if (isBackspace)    panel.Backspace();
        else if (isConfirm) panel.Confirm();
        else if (isCancel)  panel.Cancel();
        else if (isCopy)    panel.CopyToClipboard();
        else if (isPaste)   panel.PasteFromClipboard();
        else if (isShift)   panel.ToggleShift();
        else                panel.AddCharacter(character);
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InputPromptUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image promptImage;
    [SerializeField] private TextMeshProUGUI promptText;
    
    [Header("Action to Display")]
    [SerializeField] private string actionName = "confirm";
    
    [Header("Keyboard/Mouse Sprites")]
    [SerializeField] private Sprite keyboardSprite;
    
    [Header("Xbox Sprites")]
    [SerializeField] private Sprite xboxSprite;
    
    [Header("PlayStation Sprites")]
    [SerializeField] private Sprite playstationSprite;
    
    [Header("Switch Sprites")]
    [SerializeField] private Sprite switchSprite;
    
    [Header("Generic Gamepad Sprites")]
    [SerializeField] private Sprite genericSprite;

    private void OnEnable()
    {
        if (ControllerTypeDetector.Instance != null)
        {
            ControllerTypeDetector.Instance.OnControllerChanged += UpdatePrompt;
            UpdatePrompt(ControllerTypeDetector.Instance.CurrentControllerType);
        }
    }

    private void Start()
    {
        if (ControllerTypeDetector.Instance != null)
        {
            ControllerTypeDetector.Instance.OnControllerChanged -= UpdatePrompt;
            ControllerTypeDetector.Instance.OnControllerChanged += UpdatePrompt;
            UpdatePrompt(ControllerTypeDetector.Instance.CurrentControllerType);
        }
    }

    private void OnDisable()
    {
        if (ControllerTypeDetector.Instance != null)
        {
            ControllerTypeDetector.Instance.OnControllerChanged -= UpdatePrompt;
        }
    }

    public void HidePromptImage()
    {
        if (promptImage != null)
        {
            promptImage.enabled = false;
            promptImage.gameObject.SetActive(false);
        }
    }

    private void UpdatePrompt(ControllerType controllerType)
    {
        if (controllerType == ControllerType.Mobile || ControllerTypeDetector.IsMobilePlatform())
        {
            HidePromptImage();
            if (promptText != null)
            {
                promptText.enabled = true;
            }
            return;
        }

        Sprite selectedSprite = null;

        switch (controllerType)
        {
            case ControllerType.Keyboard:
                selectedSprite = keyboardSprite;
                break;
            case ControllerType.Xbox:
                selectedSprite = xboxSprite;
                break;
            case ControllerType.Playstation:
                selectedSprite = playstationSprite;
                break;
            case ControllerType.Switch:
                selectedSprite = switchSprite;
                break;
            case ControllerType.Generic:
                selectedSprite = genericSprite;
                break;
        }

        if (selectedSprite == null)
        {
            if (promptImage != null)
            {
                promptImage.enabled = false;
            }

            if (promptText != null)
            {
                promptText.enabled = false;
            }
            return;
        }

        if (promptImage != null)
        {
            promptImage.gameObject.SetActive(true);
            promptImage.sprite = selectedSprite;
            promptImage.enabled = true;
        }

        if (promptText != null)
        {
            promptText.enabled = true;
        }
    }
}

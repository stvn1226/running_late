using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI.GamePanels
{
    /// <summary>
    /// Represents a single prompt entry row in the Controls HUD (e.g. "Jump [A]" or "Drop Down [↓] + [A]").
    /// Formatted with text on the left and input icon(s) on the right, matching modern action-game HUDs.
    /// </summary>
    public class ControlPromptRowUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private string actionKey;
        [SerializeField] private TextMeshProUGUI actionText;
        [SerializeField] private Image primaryIcon;
        [SerializeField] private Image secondaryIcon;
        [SerializeField] private TextMeshProUGUI separatorText;
        [SerializeField] private Image rowBackground;

        public string ActionKey
        {
            get => actionKey;
            set => actionKey = value;
        }

        public void Setup(string actionName, Sprite primarySprite, Sprite secondarySprite)
        {
            if (string.IsNullOrEmpty(actionKey)) actionKey = actionName;

            if (actionText != null)
            {
                actionText.text = actionName;
            }

            if (primaryIcon != null)
            {
                if (primarySprite != null)
                {
                    primaryIcon.sprite = primarySprite;
                    primaryIcon.preserveAspect = true;
                    primaryIcon.gameObject.SetActive(true);
                }
                else
                {
                    primaryIcon.gameObject.SetActive(false);
                }
            }

            bool hasSecondary = secondarySprite != null;

            if (secondaryIcon != null)
            {
                if (hasSecondary)
                {
                    secondaryIcon.sprite = secondarySprite;
                    secondaryIcon.preserveAspect = true;
                    secondaryIcon.gameObject.SetActive(true);
                }
                else
                {
                    secondaryIcon.gameObject.SetActive(false);
                }
            }

            if (separatorText != null)
            {
                separatorText.gameObject.SetActive(hasSecondary);
            }
        }
    }
}

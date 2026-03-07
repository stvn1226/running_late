using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Febucci.TextAnimatorForUnity.TextMeshPro;
using Febucci.TextAnimatorCore;

public class CustomButtonEffects : MonoBehaviour, 
    IPointerEnterHandler, 
    IPointerExitHandler, 
    ISelectHandler, 
    IDeselectHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("Perfil de Efectos")]
    [SerializeField] private ButtonEffectProfile effectProfile;
    
    [Header("Referencias")]
    [SerializeField] private TextMeshProUGUI textComponent;
    [SerializeField] private TextMeshProUGUI jpTextComponent;
    [SerializeField] private TextAnimator_TMP textAnimator;
    [SerializeField] private TextAnimator_TMP jpTextAnimator;
    [SerializeField] private GameObject effectMask;
    [SerializeField] private Image buttonBackground;
    
    private Button button;
    private float baseFontSize;
    private float jpBaseFontSize;

    private string originalText;
    private string jpOriginalText;

    private void Awake()
    {
        button = GetComponent<Button>();
        
        if (textComponent != null)
        {
            baseFontSize = textComponent.fontSize;
            originalText = textComponent.text;
        }
        
        if (jpTextComponent != null)
        {
            jpBaseFontSize = jpTextComponent.fontSize;
            jpOriginalText = jpTextComponent.text;
        }
    }
    
    private void OnEnable()
    {
        ApplyNormalState();
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button != null && !button.interactable) return;
        ApplyHighlightedState();
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        ApplyNormalState();
    }
    
    public void OnSelect(BaseEventData eventData)
    {
        if (button != null && !button.interactable) return;
        ApplyHighlightedState();
    }
    
    public void OnDeselect(BaseEventData eventData)
    {
        ApplyNormalState();
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        if (button != null && !button.interactable) return;
        ApplyNormalState();
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        ApplyNormalState();
    }
    
    private void ApplyNormalState()
    {
        if (effectProfile == null) return;
        
        ApplyFont(effectProfile.normalFont, effectProfile.jpNormalFont);
        ApplyFontSize(baseFontSize, jpBaseFontSize);
        ApplyTextColor(effectProfile.normalTextColor);
        ApplyMaskState(effectProfile.showMaskOnNormal);
        ApplyBackgroundSprite(effectProfile.normalBackgroundSprite);

        if (textAnimator != null) textAnimator.SetText(originalText);
        if (jpTextAnimator != null) jpTextAnimator.SetText(jpOriginalText);
    }
    
    private void ApplyHighlightedState()
    {
        if (effectProfile == null) return;
        
        ApplyFont(effectProfile.highlightedFont, effectProfile.jpHighlightedFont);
        ApplyFontSize(baseFontSize + effectProfile.fontSizeIncrease, jpBaseFontSize + effectProfile.jpFontSizeIncrease);
        ApplyTextColor(effectProfile.highlightedTextColor);
        ApplyMaskState(effectProfile.showMaskOnHighlighted);
        ApplyBackgroundSprite(effectProfile.highlightedBackgroundSprite);

        if (textAnimator != null) textAnimator.SetText($"<bounce>{originalText}</bounce>");
        if (jpTextAnimator != null) jpTextAnimator.SetText($"<bounce>{jpOriginalText}</bounce>");
    }
    
    private void ApplyFont(TMP_FontAsset font, TMP_FontAsset jpFont)
    {
        if (textComponent != null && font != null)
        {
            textComponent.font = font;
        }
        
        if (jpTextComponent != null && jpFont != null)
        {
            jpTextComponent.font = jpFont;
        }
    }
    
    private void ApplyFontSize(float size, float jpSize)
    {
        if (!effectProfile.changeFontSize) return;
        
        if (textComponent != null)
        {
            textComponent.fontSize = size;
        }
        
        if (jpTextComponent != null)
        {
            jpTextComponent.fontSize = jpSize;
        }
    }
    
    private void ApplyTextColor(Color color)
    {
        if (!effectProfile.changeTextColor) return;
        
        if (textComponent != null)
        {
            textComponent.color = color;
        }
        
        if (jpTextComponent != null)
        {
            jpTextComponent.color = color;
        }
    }
    
    private void ApplyMaskState(bool show)
    {
        if (effectMask != null)
        {
            effectMask.SetActive(show);
        }
    }
    
    private void ApplyBackgroundSprite(Sprite sprite)
    {
        if (!effectProfile.changeBackgroundSprite) return;
        
        if (buttonBackground != null && sprite != null)
        {
            buttonBackground.sprite = sprite;
        }
    }
}


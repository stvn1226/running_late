using UnityEngine;
using TMPro;

[CreateAssetMenu(fileName = "ButtonEffectProfile", menuName = "UI/Button Effect Profile", order = 1)]
public class ButtonEffectProfile : ScriptableObject
{
    [Header("Fuentes - Texto Principal")]
    public TMP_FontAsset normalFont;
    public TMP_FontAsset highlightedFont;
    
    [Header("Fuentes - Texto Japonés")]
    public TMP_FontAsset jpNormalFont;
    public TMP_FontAsset jpHighlightedFont;
    
    [Header("Tamaño del Texto")]
    public bool changeFontSize = false;
    [Range(0f, 10f)]
    public float fontSizeIncrease = 2f;
    public float jpFontSizeIncrease = 2f;
    
    [Header("Colores del Texto")]
    public bool changeTextColor = false;
    public Color normalTextColor = Color.white;
    public Color highlightedTextColor = Color.yellow;
    
    [Header("Máscara de Efectos")]
    public bool showMaskOnNormal = false;
    public bool showMaskOnHighlighted = true;
    
    [Header("Background del Botón")]
    public bool changeBackgroundSprite = false;
    public Sprite normalBackgroundSprite;
    public Sprite highlightedBackgroundSprite;
}

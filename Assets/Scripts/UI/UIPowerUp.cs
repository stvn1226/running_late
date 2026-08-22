using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class UIPowerUp : MonoBehaviour
{
    public string powerUpName;
    private Sprite _icon;
    private Color _theme;
    private string _description;
    private float _duration;

    private Image _timerCircle;
    [SerializeField] private Image _powerIcon;
    [SerializeField] private TextMeshProUGUI _powerText;

    private float _targetAlpha = 1f;

    private float _currentTime = 0f;

    private CanvasGroup _canvasGroup;
    [SerializeField] private float _fadeSpeed = 5f;

    private static UIPowerUpHandler _UIPowerUpHandler;

    public void AssignHandler(UIPowerUpHandler handler)
    {
        _UIPowerUpHandler = handler;
    }

    public void Initialize(PowerUpContract powerUpContract)
    {
        if (powerUpContract == null) return;

        powerUpName = powerUpContract.powerUpName;
        _icon = powerUpContract.image;
        _theme = powerUpContract.color;
        _description = powerUpContract.description;
        _duration = powerUpContract.duration;

        _currentTime = 0f;

        if (_timerCircle == null)
        {
            _timerCircle = GetComponent<Image>();
        }

        if (_timerCircle != null)
        {
            _timerCircle.color = _theme;
        }

        if (_powerIcon != null)
        {
            _powerIcon.sprite = _icon;
        }

        if (_powerText != null)
        {
            _powerText.text = _description;
            if (powerUpName != "Shield") _powerText.color = _theme;
        }

        if (_canvasGroup == null)
        {
            // Busca CanvasGroup en el contenedor padre (PowerUpContainer) o en sí mismo
            Transform rootTarget = (transform.parent != null && transform.parent.name.Contains("Container")) ? transform.parent : transform;
            _canvasGroup = rootTarget.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = rootTarget.gameObject.AddComponent<CanvasGroup>();
            }
        }
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
        }
    }

    private void Update()
    {
        ProcessFill();
    }

    private void ProcessFill()
    {
        if (_duration <= 0f) return;

        _targetAlpha = 1f;

        _currentTime += Time.deltaTime;

        if (_currentTime <= _duration)
        {
            float remainingTime = _duration - _currentTime;
            float fillAmount = remainingTime / _duration;
            if (_timerCircle != null)
            {
                _timerCircle.fillAmount = fillAmount;
            }
        }
        else
        {
            _targetAlpha = 0f;
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = Mathf.Lerp(_canvasGroup.alpha, _targetAlpha, _fadeSpeed * Time.deltaTime);

            if (_canvasGroup.alpha <= 0.05f && _targetAlpha == 0f)
            {
                if (_UIPowerUpHandler != null)
                {
                    _UIPowerUpHandler.UnregisterPowerUp(powerUpName);
                }
            }
        }
        else if (_targetAlpha == 0f)
        {
            if (_UIPowerUpHandler != null)
            {
                _UIPowerUpHandler.UnregisterPowerUp(powerUpName);
            }
        }
    }

    public static void RemoveShield()
    {
        _UIPowerUpHandler.UnregisterPowerUp("Shield");
    }
}

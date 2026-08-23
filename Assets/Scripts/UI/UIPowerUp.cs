using TMPro;
using UnityEngine;
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

        Transform rootTarget = (transform.parent != null && transform.parent.name.Contains("Container")) ? transform.parent : transform;
        if (_canvasGroup == null)
        {
            // Busca CanvasGroup en el contenedor padre (PowerUpContainer) o en sí mismo
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

        if (_popAnimationCoroutine != null) StopCoroutine(_popAnimationCoroutine);
        _popAnimationCoroutine = StartCoroutine(AnimatePopIn(rootTarget));
    }

    private Coroutine _popAnimationCoroutine;

    private System.Collections.IEnumerator AnimatePopIn(Transform target)
    {
        if (target == null) yield break;

        float elapsed = 0f;
        float duration = 0.35f;
        target.localScale = Vector3.zero;

        while (elapsed < duration)
        {
            if (target == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // EaseOutBack formula for juicy punch bounce
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            float scale = 1f + c3 * Mathf.Pow(t - 1f, 3) + c1 * Mathf.Pow(t - 1f, 2);
            target.localScale = Vector3.one * Mathf.Max(0f, scale);
            yield return null;
        }

        if (target != null)
        {
            target.localScale = Vector3.one;
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

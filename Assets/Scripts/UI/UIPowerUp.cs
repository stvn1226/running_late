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

    [Header("Fade Settings")]
    [Tooltip("Normalized time [0..1] when the power-up card starts fading out. Default 0.75 means 100% visible for the first 75% of duration.")]
    [Range(0f, 1f)]
    [SerializeField] private float _fadeStartNormalized = 0.75f;

    [Tooltip("Enable to use a custom curve for alpha over lifetime (0.0 to 1.0 progress).")]
    [SerializeField] private bool _useCustomCurve = false;
    [SerializeField] private AnimationCurve _fadeCurve = new AnimationCurve(
        new Keyframe(0f, 1f),
        new Keyframe(0.75f, 1f),
        new Keyframe(1f, 0f)
    );

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
            _powerText.color = new Color(0.12f, 0.14f, 0.22f, 1f);
        }

        Transform rootTarget = (transform.parent != null && transform.parent.name.Contains("Container")) ? transform.parent : transform;
        if (_canvasGroup == null)
        {
            _canvasGroup = rootTarget.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponentInParent<CanvasGroup>();
            }
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
        if (_duration <= 0f)
        {
            if (_canvasGroup != null) _canvasGroup.alpha = 1f;
            return;
        }

        _currentTime += Time.deltaTime;
        float progress = Mathf.Clamp01(_currentTime / _duration);

        // Update timer circle fill
        float remainingTime = _duration - _currentTime;
        float fillAmount = Mathf.Clamp01(remainingTime / _duration);
        if (_timerCircle != null)
        {
            _timerCircle.fillAmount = fillAmount;
        }

        // Alpha fade logic: configurable threshold or custom curve
        float targetAlpha = 1f;
        if (_useCustomCurve && _fadeCurve != null)
        {
            targetAlpha = Mathf.Clamp01(_fadeCurve.Evaluate(progress));
        }
        else
        {
            if (progress > _fadeStartNormalized)
            {
                float fadeRange = Mathf.Max(0.001f, 1f - _fadeStartNormalized);
                float fadeProgress = (progress - _fadeStartNormalized) / fadeRange;
                targetAlpha = Mathf.Clamp01(1f - fadeProgress);
            }
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = targetAlpha;
        }

        // Expired -> unregister and destroy card
        if (_currentTime >= _duration)
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

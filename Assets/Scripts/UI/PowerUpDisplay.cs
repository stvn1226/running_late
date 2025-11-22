using UnityEngine;
using UnityEngine.UI;

public class PowerUpDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MonoBehaviour powerUpSource;
    [SerializeField] private Image fillImage;
    [SerializeField] private CanvasGroup canvasGroup;
    
    [Header("Display Settings")]
    [SerializeField] private bool hideWhenInactive = true;
    [SerializeField] private float fadeSpeed = 5f;
    [SerializeField] private float searchInterval = 0.5f;
    
    private ITimedPowerUp timedPowerUp;
    private float targetAlpha = 0f;
    private float searchTimer = 0f;
    private bool hasSearchedForPlayer = false;

    private void Start()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (hideWhenInactive && canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        TryFindPowerUpSource();
    }

    private void Update()
    {
        if (timedPowerUp == null)
        {
            searchTimer += Time.deltaTime;
            if (searchTimer >= searchInterval)
            {
                searchTimer = 0f;
                TryFindPowerUpSource();
            }
            return;
        }

        bool isActive = timedPowerUp.IsActive();
        
        if (isActive)
        {
            targetAlpha = 1f;
            
            if (fillImage != null)
            {
                float remainingTime = timedPowerUp.GetRemainingTime();
                float maxTime = timedPowerUp.GetMaxTime();
                float fillAmount = maxTime > 0 ? remainingTime / maxTime : 0f;
                fillImage.fillAmount = fillAmount;
            }
        }
        else
        {
            targetAlpha = hideWhenInactive ? 0f : 0.3f;
            
            if (fillImage != null)
            {
                fillImage.fillAmount = 0f;
            }
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
        }
    }

    private void TryFindPowerUpSource()
    {
        if (powerUpSource == null)
        {
            FindPowerUpSource();
        }

        if (powerUpSource != null)
        {
            timedPowerUp = powerUpSource as ITimedPowerUp;
            if (timedPowerUp == null)
            {
                GameLogger.LogError($"PowerUpDisplay: {powerUpSource.GetType().Name} does not implement ITimedPowerUp interface!");
            }
            else if (!hasSearchedForPlayer)
            {
                GameLogger.Log($"PowerUpDisplay: Successfully connected to {powerUpSource.GetType().Name}");
                hasSearchedForPlayer = true;
            }
        }
    }

    private void FindPowerUpSource()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            MonoBehaviour[] components = player.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour component in components)
            {
                if (component is ITimedPowerUp)
                {
                    powerUpSource = component;
                    return;
                }
            }
            
            if (!hasSearchedForPlayer)
            {
                GameLogger.LogWarning($"PowerUpDisplay: Player found but has no ITimedPowerUp components");
                hasSearchedForPlayer = true;
            }
        }
    }
}

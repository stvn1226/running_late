using UnityEngine;
using UnityEngine.UI;

public abstract class PowerUp : MonoBehaviour
{
    [Header("Power-Up Settings")]
    public string powerUpName;
    public Sprite icon;
    public Color theme;
    public string desc;
    [SerializeField] private bool destroyOnPickup = true;
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] protected float activeTime = 10f;

    [Tooltip("Instant effects (e.g. ExtraTime) skip UI registration and always apply.")]
    [SerializeField] private bool isInstantEffect = false;

    [Header("Visual Effects")]
    [SerializeField] private GameObject pickupEffect;
    [SerializeField] private float effectDuration = 1f;

    private bool hasBeenPickedUp = false;

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasBeenPickedUp) return;

        if (showDebugInfo)
        {
            GameLogger.Log("POWERUP", $"Trigger detected with {collision.gameObject.name} (Tag: {collision.tag})");
        }

        if (collision.CompareTag("Player"))
        {
            PickUp(collision.gameObject);
        }
    }

    public void PickUp(GameObject player)
    {
        if (hasBeenPickedUp) return;


        if (showDebugInfo)
        {
            GameLogger.Log("POWERUP", $"Power-up {GetType().Name} picked up by {player.name}");
        }

        if (isInstantEffect)
        {
            ApplyEffect(player);

            if (UIPowerUpHandler.Instance != null && !string.IsNullOrEmpty(powerUpName))
            {
                UIPowerUpHandler.Instance.RegisterPowerUp(new PowerUpContract() { 
                    powerUpName = powerUpName, 
                    image = icon, 
                    color = theme, 
                    duration = 3f,
                    description = desc
                });
            }
        }
        else
        {
            bool registered = UIPowerUpHandler.Instance != null && UIPowerUpHandler.Instance.RegisterPowerUp(new PowerUpContract() { 
                powerUpName = powerUpName, 
                image = icon, 
                color = theme, 
                duration = activeTime,
                description = desc
            });

            if (registered)
            {
                ApplyEffect(player);
            }
            else
            {
                if (showDebugInfo)
                {
                    GameLogger.Log("POWERUP", $"Power-up {GetType().Name} NOT applied due to priority conflict.");
                }
            }
        }

        PlayPickupSound();
        SpawnPickupEffect();

        hasBeenPickedUp = true;

        if (destroyOnPickup)
        {
            StartCoroutine(AnimatePickupAndDestroy());
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private System.Collections.IEnumerator AnimatePickupAndDestroy()
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null) colliders[i].enabled = false;
        }

        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + Vector3.up * 1.2f;
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = startScale * 1.35f;

        float elapsed = 0f;
        float duration = 0.25f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            if (sr != null)
            {
                Color c = sr.color;
                c.a = Mathf.Lerp(1f, 0f, t);
                sr.color = c;
            }
            yield return null;
        }

        Destroy(gameObject);
    }

    protected abstract void ApplyEffect(GameObject player);

    private void PlayPickupSound()
    {
        AudioManager.Instance?.Play(AudioManager.SoundType.Powerup);
        AudioManager.Instance?.Play(AudioManager.SoundType.PowerupSFX);
    }

    private void SpawnPickupEffect()
    {
        if (pickupEffect != null)
        {
            GameObject effect = Instantiate(pickupEffect, transform.position, Quaternion.identity);
            Destroy(effect, effectDuration);
        }
    }
}
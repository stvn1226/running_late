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

        bool registered = UIPowerUpHandler.Instance.RegisterPowerUp(new PowerUpContract() { 
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

        PlayPickupSound();
        SpawnPickupEffect();

        hasBeenPickedUp = true;

        if (destroyOnPickup)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
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
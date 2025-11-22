using UnityEngine;

public abstract class PowerUp : MonoBehaviour
{
    [Header("Power-Up Settings")]
    [SerializeField] private bool destroyOnPickup = true;
    [SerializeField] private bool showDebugInfo = true;

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

        hasBeenPickedUp = true;

        if (showDebugInfo)
        {
            GameLogger.Log("POWERUP", $"Power-up {GetType().Name} picked up by {player.name}");
        }

        ApplyEffect(player);
        PlayPickupSound();
        SpawnPickupEffect();

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

using UnityEngine;

public class PowerUpTrigger : MonoBehaviour
{
    [SerializeField] private PowerUp powerUp;

    private void Awake()
    {
        if (powerUp == null)
        {
            powerUp = GetComponentInParent<PowerUp>();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (powerUp != null && collision.CompareTag("Player"))
        {
            powerUp.PickUp(collision.gameObject);
        }
    }
}

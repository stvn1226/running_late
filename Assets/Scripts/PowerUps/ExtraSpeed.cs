using UnityEngine;

public class ExtraSpeed : PowerUp
{
    [SerializeField] private float extraSpeed = 8f;

    protected override void ApplyEffect(GameObject player)
    {
        PlayerMovement movementController = player.GetComponentInParent<PlayerMovement>();
        if (movementController != null)
        {
            movementController.InvokeExtraSpeed(extraSpeed, effectDuration); // Increase speed by 8 points
        }
        else
        {
            GameLogger.LogError("POWERUP", "PlayerMovement component not found! Cannot increase speed.");
        }
    }
}

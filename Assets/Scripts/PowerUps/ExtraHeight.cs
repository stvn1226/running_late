using UnityEngine;

public class ExtraHeight : PowerUp
{
    [SerializeField] private float extraHeight = 4f;
    protected override void ApplyEffect(GameObject player)
    {
        PlayerJump jumpController = player.GetComponentInParent<PlayerJump>();
        if (jumpController != null)
        {
            jumpController.InvokeExtraJumpHeight(extraHeight, activeTime); // Increase jump height
        }
        else
        {
            GameLogger.LogError("POWERUP", "PlayerMovement component not found! Cannot increase jump height.");
        }
    }
}

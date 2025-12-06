using UnityEngine;

public class DoubleJump : PowerUp
{
    PlayerJump playerJump;

    protected override void ApplyEffect(GameObject player)
    {
        if (playerJump == null)
        {
            playerJump = player.GetComponentInParent<PlayerJump>();
        }

        if (playerJump != null)
        {
            playerJump.InvokeDoubleJump(activeTime);
        }
        else
        {
            GameLogger.LogError("PlayerJump component not found on player object.");
        }
    }
}

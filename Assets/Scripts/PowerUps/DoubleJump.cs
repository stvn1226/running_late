using UnityEngine;

public class DoubleJump : PowerUp
{
    PlayerJump playerJump;
    [SerializeField] private float activationTime = 10f;

    protected override void ApplyEffect(GameObject player)
    {
        if (playerJump == null)
        {
            playerJump = player.GetComponentInParent<PlayerJump>();
        }

        if (playerJump != null)
        {
            playerJump.InvokeDoubleJump();
        }
        else
        {
            GameLogger.LogError("PlayerJump component not found on player object.");
        }
    }
}

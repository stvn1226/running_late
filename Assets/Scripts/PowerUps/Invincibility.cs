using UnityEngine;

public class Invincibility : PowerUp
{
    protected override void ApplyEffect(GameObject player)
    {
        PlayerStates playerStates = player.GetComponentInParent<PlayerStates>();
        if (playerStates != null)
        {
            playerStates.InvokeInvincibility(activeTime);
        }
    }
}

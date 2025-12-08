using UnityEngine;

public class Shield : PowerUp
{
    protected override void ApplyEffect(GameObject player)
    {
        PlayerStates playerStates = player.GetComponentInParent<PlayerStates>();
        if (playerStates != null)
        {
            playerStates.InvokeShield();
        }

    }
}

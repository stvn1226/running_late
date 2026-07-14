using UnityEngine;

public class Toast : PowerUp
{
    protected override void ApplyEffect(GameObject player)
    {
        PlayerStates playerStates = player.GetComponentInParent<PlayerStates>();
        if (playerStates != null)
        {
            playerStates.InvokeToast();
        }

    }
}

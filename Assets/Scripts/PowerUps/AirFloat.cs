using UnityEngine;

public class AirFloat : PowerUp
{
    protected override void ApplyEffect(GameObject player)
    {
        PlayerJump playerJump = player.GetComponentInParent<PlayerJump>();
        if (playerJump != null)
        {
            playerJump.InvokeAirFloat(activeTime);
        }
    }
}

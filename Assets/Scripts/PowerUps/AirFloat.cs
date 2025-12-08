using UnityEngine;

public class AirFloat : PowerUp
{
    protected override void ApplyEffect(GameObject player)
    {
        PlayerJump playerJump = player.GetComponent<PlayerJump>();
        if (playerJump != null)
        {
            playerJump.InvokeAirFloat(activeTime);
        }
    }
}

using UnityEngine;

public class PlayerStomp : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerConfig config;
    [SerializeField] private Rigidbody2D playerRb;
    [SerializeField] private PlayerStates states;

    [Header("Stomp Settings")]
    [SerializeField] private float stompBounceForce = 15f;

    private void Awake()
    {
        if (playerRb == null)
        {
            playerRb = GetComponent<Rigidbody2D>();
        }
        if (states == null)
        {
            states = GetComponent<PlayerStates>();
        }
    }

    public void ExecuteStompBounce()
    {
        if (playerRb == null || states == null)
        {
            GameLogger.LogError("STOMP", "Missing required references!");
            return;
        }

        GameLogger.LogState($"STOMP BOUNCE EXECUTED! Current VelY: {playerRb.linearVelocity.y:F2}");
        ApplyBounce();
        AudioManager.Instance?.Play(AudioManager.SoundType.Bounce);
        PlayerAirDodge airDodge = GetComponentInParent<PlayerAirDodge>();
        if (airDodge != null)
        {
            airDodge.OnStompBounce();
        }
    }

    private void ApplyBounce()
    {
        Vector2 oldVelocity = playerRb.linearVelocity;
        
        playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, 0f);
        
        playerRb.AddForce(Vector2.up * stompBounceForce, ForceMode2D.Impulse);
        
        if (states != null)
        {
            states.StartJump();
        }

        GameLogger.LogMovement($"STOMP BOUNCE! Old VelY: {oldVelocity.y:F2} → Force: {stompBounceForce} → New VelY: {playerRb.linearVelocity.y:F2}");
    }
}
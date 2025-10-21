using UnityEngine;

public class PlayerStomp : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerConfig config;
    [SerializeField] private Rigidbody2D playerRb;
    [SerializeField] private PlayerStates states;

    [Header("Stomp Settings")]
    [SerializeField] private float stompBounceForce = 15f;
    [SerializeField] private LayerMask enemyLayer;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (CanStomp() && IsEnemy(collision))
        {
            ExecuteStomp(collision);
        }
    }

    private bool CanStomp()
    {
        if (playerRb == null || states == null)
        {
            GameLogger.LogError("STOMP", "Missing required references!");
            return false;
        }
        
        if (!states.isGrounded)
        {
            GameLogger.LogVerbose("STOMP", $"Can stomp - Velocity.y: {playerRb.linearVelocity.y:F2}");
            return true;
        }

        return false;
    }

    private bool IsEnemy(Collider2D collision)
    {
        int collisionLayer = collision.gameObject.layer;
        bool isInEnemyLayer = ((1 << collisionLayer) & enemyLayer) != 0;

        if (isInEnemyLayer)
        {
            GameLogger.LogVerbose("STOMP", $"Enemy detected: {collision.gameObject.name}");
        }

        return isInEnemyLayer;
    }

    private void ExecuteStomp(Collider2D enemy)
    {
        ApplyBounce();
        StaggerEnemy(enemy);
        GameLogger.LogState($"STOMP EXECUTED on {enemy.gameObject.name}! Bounce applied.");
        Debug.Log("STOMP EXECUTED!");
    }

    private void ApplyBounce()
    {
        playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, 0f);
        
        playerRb.AddForce(Vector2.up * stompBounceForce, ForceMode2D.Impulse);
        
        if (states != null)
        {
            states.StartJump();
        }

        GameLogger.LogMovement($"STOMP BOUNCE! Force: {stompBounceForce}, New Velocity.y: {playerRb.linearVelocity.y:F2}");
    }

    private void StaggerEnemy(Collider2D enemy)
    {
        enemy.GetComponentInParent<Pigeon>()?.TriggerStagger();
        GameLogger.LogVerbose("STOMP", $"Enemy {enemy.gameObject.name} staggered!");
    }
}

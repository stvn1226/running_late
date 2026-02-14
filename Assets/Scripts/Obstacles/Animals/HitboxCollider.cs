using UnityEngine;
using Utils;

public class HitboxCollider : MonoBehaviour
{
    [SerializeField] private Animal parent;

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (parent == null)
        {
            GameLogger.LogError("HITBOX", "Parent Animal reference is null!");
            return;
        }

        if (parent.isStaggered)
        {
            GameLogger.LogVerbose("HITBOX", $"[{parent.gameObject.name}] Ignoring collision - already staggered");
            return;
        }

        if (parent.wasHit)
        {
            GameLogger.LogVerbose("HITBOX", $"[{parent.gameObject.name}] Ignoring collision - recently hit (cooldown active)");
            return;
        }

        if (collision.gameObject.CompareTag(TagUtils.PLAYERKICK_TAG))
        {
            GameLogger.Log($"[{parent.gameObject.name}] Hit by PlayerKick trigger.");
            parent.TriggerStagger();
            parent.wasHit = true;
            return;
        }

        if (collision.gameObject.CompareTag(TagUtils.PLAYER_TAG))
        {
            GameLogger.Log($"[{parent.gameObject.name}] Player collision detected");
            
            Rigidbody2D playerRb = collision.GetComponentInParent<Rigidbody2D>();
            
            if (playerRb != null)
            {
                float velocityY = playerRb.linearVelocity.y;
                
                bool isStomp = velocityY < 0f;

                GameLogger.Log($"[{parent.gameObject.name}] VelY={velocityY:F2}, Result={(isStomp ? "STOMP" : "DAMAGE")}");

                if (isStomp)
                {
                    HandleStompOnEnemy(collision, playerRb);
                }
                else
                {
                    PlayerStates playerStates = collision.GetComponentInParent<PlayerStates>();
                    
                    if (playerStates != null && playerStates.isInvincible && !playerStates.isShielded)
                    {
                        GameLogger.Log($"[{parent.gameObject.name}] Player is INVULNERABLE - ignoring damage (no stomp)");
                        return;
                    }
                    
                    HandlePlayerDamage(collision, playerRb);
                }
            }
            else
            {
                GameLogger.LogError("HITBOX", $"[{parent.gameObject.name}] No Rigidbody2D found on player!");
                HandlePlayerDamage(collision, null);
            }
        }
    }

    private void HandleStompOnEnemy(Collider2D collision, Rigidbody2D playerRb)
    {
        GameLogger.Log($"[{parent.gameObject.name}] ✓ STOMPED (VelY: {playerRb.linearVelocity.y:F2}) - bouncing!");
        
        parent.TriggerStagger();
        parent.wasHit = true;

        PlayerStomp playerStomp = collision.GetComponentInParent<PlayerStomp>();
        
        if (playerStomp != null)
        {
            playerStomp.ExecuteStompBounce();
        }
        else
        {
            GameLogger.LogWarning("HITBOX", $"[{parent.gameObject.name}] PlayerStomp component not found!");
        }

        if (AnalyticsManager.Instance != null)
        {
            Vector3 playerPos = playerRb.transform.position;
            float velocityX = Mathf.Abs(playerRb.linearVelocity.x);

            var context = AnalyticsEvents.CreatePlayerActionContext(
                AnalyticsEvents.PLAYER_STOMP,
                playerPos.x,
                velocityX
            );
            context["enemy_type"] = parent.gameObject.name;
            context["velocity_y"] = playerRb.linearVelocity.y;

            AnalyticsManager.Instance.TrackPlayerAction(AnalyticsEvents.PLAYER_STOMP, context);
        }
    }

    private void HandlePlayerDamage(Collider2D collision, Rigidbody2D playerRb)
    {
        float velocityY = playerRb != null ? playerRb.linearVelocity.y : 0f;
        if (parent.cachedPlayerMovement == null)
            parent.cachedPlayerMovement = GameReferences.Instance.PlayerMovement;

        PlayerStates playerStates = collision.GetComponentInParent<PlayerStates>();
        bool isInvincible = playerStates != null && playerStates.isInvincible;
        bool isShielded = playerStates != null && playerStates.isShielded;

        if (isInvincible || isShielded)
        {
            GameLogger.Log($"[{parent.gameObject.name}] Player is INVINCIBLE - staggering enemy instead!");

            playerStates.RemoveShield();
            parent.TriggerStagger();
            parent.wasHit = true;

            if (AnalyticsManager.Instance != null)
            {
                float playerSpeed = 0f;
                if (parent.cachedPlayerMovement != null)
                {
                    Rigidbody2D rb = parent.cachedPlayerMovement.GetComponent<Rigidbody2D>();
                    playerSpeed = rb != null ? Mathf.Abs(rb.linearVelocity.x) : 0f;
                }

                AnalyticsManager.Instance.TrackEvent(AnalyticsEvents.OBSTACLE_HIT, new System.Collections.Generic.Dictionary<string, object>
            {
                { "obstacle_type", parent.GetType().Name },
                { "position_x", parent.transform.position.x },
                { "player_speed", playerSpeed },
                { "player_velocity_y", velocityY },
                { "player_invincible", true }
            });
            }

            return;
        }

        GameLogger.Log($"[{parent.gameObject.name}] >> DAMAGE (VelY: {velocityY:F2})");

        if (AnalyticsManager.Instance != null)
        {
            float playerSpeed = 0f;
            if (parent.cachedPlayerMovement != null)
            {
                Rigidbody2D rb = parent.cachedPlayerMovement.GetComponent<Rigidbody2D>();
                playerSpeed = rb != null ? Mathf.Abs(rb.linearVelocity.x) : 0f;
            }

            AnalyticsManager.Instance.TrackEvent(AnalyticsEvents.OBSTACLE_HIT, new System.Collections.Generic.Dictionary<string, object>
        {
            { "obstacle_type", parent.GetType().Name },
            { "position_x", parent.transform.position.x },
            { "player_speed", playerSpeed },
            { "player_velocity_y", velocityY }
        });
        }

        if (parent.cachedPlayerMovement != null)
            parent.cachedPlayerMovement.StaggerPlayer(new Vector2(-1, 1));
    }
}

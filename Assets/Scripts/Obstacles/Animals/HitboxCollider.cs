using UnityEngine;
using Utils;

public class HitboxCollider : MonoBehaviour
{
    [SerializeField] private Animal parent;

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (parent.wasHit || parent.isStaggered)
        {
            return;
        }

        if (collision.gameObject.CompareTag(TagUtils.PLAYERKICK_TAG))
        {
            GameLogger.Log($"[{parent.gameObject.name}] Hit by PlayerKick trigger.");
            parent.TriggerStagger();
            parent.wasHit = true;
            return;
        }

        bool isPlayerStompTag = collision.gameObject.CompareTag(TagUtils.PLAYERSTOMP_TAG);
        
        if (isPlayerStompTag)
        {
            GameLogger.Log($"[{parent.gameObject.name}] Player collision detected on {collision.gameObject.name} (Tag: {collision.tag})");
            
            Rigidbody2D playerRb = collision.GetComponentInParent<Rigidbody2D>();
            
            if (playerRb != null)
            {
                float velocityY = playerRb.linearVelocity.y;
                GameLogger.Log($"[{parent.gameObject.name}] Player velocity Y: {velocityY:F2}, Tag: {collision.tag}");

                bool isStomp = velocityY < 0f;

                if (isStomp)
                {
                    GameLogger.Log($"[{parent.gameObject.name}] ✓ STOMP CONDITION MET (Tag={collision.tag}, Y={velocityY:F2})");
                    HandleStompOnEnemy(collision, playerRb);
                }
                else
                {
                    GameLogger.Log($"[{parent.gameObject.name}] ✗ DAMAGE CONDITION (Tag={collision.tag}, Y={velocityY:F2})");
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
        GameLogger.Log($"[{parent.gameObject.name}] Stomped by Player (VelY: {playerRb.linearVelocity.y:F2}) - bouncing!");
        
        parent.TriggerStagger();
        parent.wasHit = true;

        PlayerStomp playerStomp = collision.GetComponent<PlayerStomp>();
        
        if (playerStomp != null)
        {
            GameLogger.Log($"[{parent.gameObject.name}] ✓ PlayerStomp found on {collision.gameObject.name}! Calling ExecuteStompBounce()");
            playerStomp.ExecuteStompBounce();
        }
        else
        {
            GameLogger.LogWarning("HITBOX", $"[{parent.gameObject.name}] PlayerStomp component not found on {collision.gameObject.name}!");
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
        GameLogger.Log($"[{parent.gameObject.name}] Collided with Player (VelY: {velocityY:F2}) - dealing damage");

        if (AnalyticsManager.Instance != null)
        {
            if (parent.cachedPlayerMovement == null)
                parent.cachedPlayerMovement = GameReferences.Instance.PlayerMovement;

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

        if (parent.cachedPlayerMovement == null)
            parent.cachedPlayerMovement = GameReferences.Instance.PlayerMovement;

        if (parent.cachedPlayerMovement != null)
            parent.cachedPlayerMovement.StaggerPlayer(new Vector2(-1, 1));

        parent.wasHit = true;

        StartCoroutine(parent.DisableHitboxTemp(2f));
    }
}

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

        if (collision.gameObject.CompareTag(TagUtils.PLAYERSTOMP_TAG))
        {
            GameLogger.Log($"[{parent.gameObject.name}] Hit by PlayerStomp trigger.");
            parent.TriggerStagger();
            parent.wasHit = true;
            return;
        }

        if (collision.gameObject.CompareTag(TagUtils.PLAYER_TAG))
        {
            GameLogger.Log($"[{parent.gameObject.name}] Collided with Player - dealing damage");
            
            if (AnalyticsManager.Instance != null)
            {
                if (parent.cachedPlayerMovement == null)
                    parent.cachedPlayerMovement = GameReferences.Instance.PlayerMovement;
        
                float playerSpeed = 0f;
                if (parent.cachedPlayerMovement != null)
                {
                    Rigidbody2D playerRb = parent.cachedPlayerMovement.GetComponent<Rigidbody2D>();
                    playerSpeed = playerRb != null ? Mathf.Abs(playerRb.linearVelocity.x) : 0f;
                }
        
                AnalyticsManager.Instance.TrackEvent(AnalyticsEvents.OBSTACLE_HIT, new System.Collections.Generic.Dictionary<string, object>
                {
                    { "obstacle_type", parent.GetType().Name },
                    { "position_x", parent.transform.position.x },
                    { "player_speed", playerSpeed }
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
}
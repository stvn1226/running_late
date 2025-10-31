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
            
            if (parent.cachedPlayerMovement == null)
                parent.cachedPlayerMovement = GameReferences.Instance.PlayerMovement;

            if (parent.cachedPlayerMovement != null)
                parent.cachedPlayerMovement.StaggerPlayer(new Vector2(-1, 1));
            
            parent.wasHit = true;

            StartCoroutine(parent.DisableHitboxTemp(2f));
        }
    }
}
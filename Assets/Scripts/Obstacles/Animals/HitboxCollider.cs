using UnityEngine;
using Utils;

public class HitboxCollider : MonoBehaviour
{
    [SerializeField] private Animal parent;
    private bool stomping;

    private void Update()
    {
        stomping = false;
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag(TagUtils.PLAYERKICK_TAG))
        {
            GameLogger.Log("[Pigeon] Pigeon hit by PlayerKick trigger.");
            parent.TriggerStagger();
            parent.wasHit = true;
            return;
        }

        if (collision.gameObject.CompareTag(TagUtils.PLAYERSTOMP_TAG))
        {
            stomping = true;
            parent.TriggerStagger();
            parent.wasHit = true;
            return;
        }

        if (collision.gameObject.CompareTag(TagUtils.PLAYER_TAG) && !stomping)
        {
            GameLogger.Log("[Pigeon] Pigeon collided with Player");
            if (parent.cachedPlayerMovement == null)
                parent.cachedPlayerMovement = GameReferences.Instance.PlayerMovement;

            if (parent.cachedPlayerMovement != null)
                parent.cachedPlayerMovement.StaggerPlayer(new Vector2(-1, 1));
            
            parent.wasHit = true;

            StartCoroutine(parent.DisableHitboxTemp(2f));
        }
    }
}

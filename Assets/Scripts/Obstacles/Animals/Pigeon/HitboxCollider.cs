using UnityEngine;
using Utils;

public class HitboxCollider : MonoBehaviour
{
    [SerializeField] private Pigeon pigeonParent;
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
            pigeonParent.TriggerStagger();
            pigeonParent.wasHit = true;
            return;
        }

        if (collision.gameObject.CompareTag(TagUtils.PLAYERSTOMP_TAG))
        {
            stomping = true;
            pigeonParent.TriggerStagger();
            pigeonParent.wasHit = true;
            return;
        }

        if (collision.gameObject.CompareTag(TagUtils.PLAYER_TAG) && !stomping)
        {
            GameLogger.Log("[Pigeon] Pigeon collided with Player");
            if (pigeonParent.cachedPlayerMovement == null)
                pigeonParent.cachedPlayerMovement = GameReferences.Instance.PlayerMovement;

            if (pigeonParent.cachedPlayerMovement != null)
                pigeonParent.cachedPlayerMovement.StaggerPlayer(new Vector2(-1, 1));
            
            pigeonParent.wasHit = true;

            StartCoroutine(pigeonParent.DisableHitboxTemp(2f));
        }
    }
}

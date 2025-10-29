using UnityEngine;
using Utils;

public class NearMissCollider : MonoBehaviour
{
    [SerializeField] private Animal parent;

    protected virtual void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag(TagUtils.PLAYER_TAG) && !parent.wasHit)
        {
            if (Random.Range(0f, 1f) <= 0.2f) //20% chance to trigger slowdown
            {
                StartCoroutine(collision.GetComponentInParent<PlayerStates>()?.StartSlowdown(.4f));
            }
        }

    }

}

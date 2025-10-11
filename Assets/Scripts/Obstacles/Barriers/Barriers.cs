using UnityEngine;
using Utils;

public class Barriers : MonoBehaviour
{
    [SerializeField] private bool showDebugInfo = true;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag(TagUtils.PLAYERKICK_TAG))
        {
            if (showDebugInfo) GameLogger.Log("BARRIER", "Barrier hit by PlayerKick trigger, destroying barrier.");
            Destroy(transform.parent.gameObject);
        }
    }
}

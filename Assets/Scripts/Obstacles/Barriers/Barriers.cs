using UnityEngine;

public class Barriers : MonoBehaviour
{
    [SerializeField] private bool showDebugInfo = true;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("PlayerKick"))
        {
            if (showDebugInfo) Debug.Log("Barrier hit by PlayerKick trigger, destroying barrier.");
            Destroy(transform.parent.gameObject);
        }
    }
}

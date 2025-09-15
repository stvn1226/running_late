using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [Header("Spawn Point Settings")]
    public bool isHomeSpawn = true;
    public bool isActive = true;
    
    private void OnDrawGizmos()
    {
        if (isActive)
        {
            Gizmos.color = isHomeSpawn ? Color.green : Color.blue;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.right * 2f);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (isActive)
        {
            Gizmos.color = isHomeSpawn ? Color.green : Color.blue;
            Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.3f);
            Gizmos.DrawSphere(transform.position, 1f);
        }
    }
}
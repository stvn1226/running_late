using UnityEngine;

public class PlayerBounds : MonoBehaviour
{
    private Camera cam;
    private Rigidbody2D rb;

    private void Awake()
    {
        cam = Camera.main;
        rb = GetComponent<Rigidbody2D>();
        
        // Try to get from children if not found on this GameObject
        if (rb == null)
        {
            rb = GetComponentInChildren<Rigidbody2D>();
        }
    }

    private void LateUpdate()
    {
        if (cam == null) return;
        if (Mathf.Abs(cam.transform.position.x - transform.position.x) > 20f) return;


        Vector3 viewPos = transform.position;
        Vector3 originalPos = viewPos;
        
        // Get camera bounds using viewport coordinates
        float distance = Mathf.Abs(cam.transform.position.z - transform.position.z);
        Vector3 bottomLeft = cam.ViewportToWorldPoint(new Vector3(0, 0, distance));
        Vector3 topRight = cam.ViewportToWorldPoint(new Vector3(1, 1, distance));
        
        // Clamp position to camera bounds
        viewPos.x = Mathf.Clamp(viewPos.x, bottomLeft.x, topRight.x);
        viewPos.y = Mathf.Clamp(viewPos.y, bottomLeft.y, topRight.y);
        
        // Apply the clamped position
        transform.position = viewPos;
        
        // Stop velocity if we hit a boundary to prevent getting stuck
        if (rb != null && viewPos != originalPos)
        {
            Vector2 velocity = rb.linearVelocity;
            
            // Stop horizontal velocity if we hit left/right boundary
            if (viewPos.x != originalPos.x)
            {
                velocity.x = -velocity.x;
            }
            
            // Stop vertical velocity if we hit top/bottom boundary
            if (viewPos.y != originalPos.y)
            {
                velocity.y = -velocity.y;
            }
            
            rb.linearVelocity = velocity;
        }
    }

}

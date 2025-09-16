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
                velocity.x = 0;
            }
            
            // Stop vertical velocity if we hit top/bottom boundary
            if (viewPos.y != originalPos.y)
            {
                velocity.y = 0;
            }
            
            rb.linearVelocity = velocity;
        }
    }

    //[Header("Boundary Settings")]
    //public float leftPadding = 0.5f;
    //public float rightPadding = 0.5f;
    //public float topPadding = 0.5f;
    //public float bottomPadding = 0.5f;

    //[Header("Constraint Settings")]
    //public bool constrainX = true;
    //public bool constrainY = true;
    //public bool stopVelocityOnBoundary = true;

    //private Camera mainCamera;
    //private Rigidbody2D playerRigidbody;
    //private Transform playerTransform;

    ///// <summary>Initialize camera reference and find player components</summary>
    //private void Awake()
    //{
    //    mainCamera = Camera.main;
    //    if (mainCamera == null)
    //    {
    //        Debug.LogError("PlayerBounds: No main camera found in scene!");
    //    }

    //    // Try to get components from this GameObject first
    //    playerRigidbody = GetComponent<Rigidbody2D>();
    //    playerTransform = transform;

    //    // If not found, try to get from children
    //    if (playerRigidbody == null)
    //    {
    //        playerRigidbody = GetComponentInChildren<Rigidbody2D>();
    //    }
    //}

    //private void LateUpdate()
    //{
    //    CheckPlayerBounds();
    //}

    ///// <summary>Check if player is within camera bounds and constrain if necessary</summary>
    //private void CheckPlayerBounds()
    //{
    //    if (mainCamera == null || playerTransform == null) return;

    //    Vector3 playerPosition = playerTransform.position;
    //    Vector3 constrainedPosition = playerPosition;
    //    bool wasConstrained = false;

    //    // Get camera bounds in world space
    //    Vector3 bottomLeft = GetCameraBounds(0, 0);
    //    Vector3 topRight = GetCameraBounds(1, 1);

    //    // Apply padding to create the actual boundary area
    //    float leftBound = bottomLeft.x + leftPadding;
    //    float rightBound = topRight.x - rightPadding;
    //    float bottomBound = bottomLeft.y + bottomPadding;
    //    float topBound = topRight.y - topPadding;

    //    // Constrain X position
    //    if (constrainX)
    //    {
    //        if (playerPosition.x < leftBound)
    //        {
    //            constrainedPosition.x = leftBound;
    //            wasConstrained = true;

    //            if (stopVelocityOnBoundary && playerRigidbody != null && playerRigidbody.linearVelocity.x < 0)
    //            {
    //                playerRigidbody.linearVelocity = new Vector2(0, playerRigidbody.linearVelocity.y);
    //            }
    //        }
    //        else if (playerPosition.x > rightBound)
    //        {
    //            constrainedPosition.x = rightBound;
    //            wasConstrained = true;

    //            if (stopVelocityOnBoundary && playerRigidbody != null && playerRigidbody.linearVelocity.x > 0)
    //            {
    //                playerRigidbody.linearVelocity = new Vector2(0, playerRigidbody.linearVelocity.y);
    //            }
    //        }
    //    }

    //    // Constrain Y position
    //    if (constrainY)
    //    {
    //        if (playerPosition.y < bottomBound)
    //        {
    //            constrainedPosition.y = bottomBound;
    //            wasConstrained = true;

    //            if (stopVelocityOnBoundary && playerRigidbody != null && playerRigidbody.linearVelocity.y < 0)
    //            {
    //                playerRigidbody.linearVelocity = new Vector2(playerRigidbody.linearVelocity.x, 0);
    //            }
    //        }
    //        else if (playerPosition.y > topBound)
    //        {
    //            constrainedPosition.y = topBound;
    //            wasConstrained = true;

    //            if (stopVelocityOnBoundary && playerRigidbody != null && playerRigidbody.linearVelocity.y > 0)
    //            {
    //                playerRigidbody.linearVelocity = new Vector2(playerRigidbody.linearVelocity.x, 0);
    //            }
    //        }
    //    }

    //    // Apply the constrained position
    //    if (wasConstrained)
    //    {
    //        playerTransform.position = constrainedPosition;
    //    }
    //}

    ///// <summary>Get camera bounds in world space using viewport coordinates</summary>
    //private Vector3 GetCameraBounds(float viewportX, float viewportY)
    //{
    //    float distance = Mathf.Abs(mainCamera.transform.position.z - playerTransform.position.z);
    //    return mainCamera.ViewportToWorldPoint(new Vector3(viewportX, viewportY, distance));
    //}

    ///// <summary>Check if player is currently within bounds</summary>
    //public bool IsPlayerInBounds()
    //{
    //    if (mainCamera == null || playerTransform == null) return true;

    //    Vector3 playerPosition = playerTransform.position;
    //    Vector3 bottomLeft = GetCameraBounds(0, 0);
    //    Vector3 topRight = GetCameraBounds(1, 1);

    //    float leftBound = bottomLeft.x + leftPadding;
    //    float rightBound = topRight.x - rightPadding;
    //    float bottomBound = bottomLeft.y + bottomPadding;
    //    float topBound = topRight.y - topPadding;

    //    bool inXBounds = !constrainX || (playerPosition.x >= leftBound && playerPosition.x <= rightBound);
    //    bool inYBounds = !constrainY || (playerPosition.y >= bottomBound && playerPosition.y <= topBound);

    //    return inXBounds && inYBounds;
    //}

    ///// <summary>Get the current boundary rectangle in world coordinates</summary>
    //public Rect GetBoundsRect()
    //{
    //    if (mainCamera == null) return new Rect();

    //    Vector3 bottomLeft = GetCameraBounds(0, 0);
    //    Vector3 topRight = GetCameraBounds(1, 1);

    //    float leftBound = bottomLeft.x + leftPadding;
    //    float rightBound = topRight.x - rightPadding;
    //    float bottomBound = bottomLeft.y + bottomPadding;
    //    float topBound = topRight.y - topPadding;

    //    return new Rect(leftBound, bottomBound, rightBound - leftBound, topBound - bottomBound);
    //}

    //private void OnDrawGizmosSelected()
    //{
    //    if (mainCamera == null) return;

    //    // Draw the boundary area
    //    Rect bounds = GetBoundsRect();

    //    Gizmos.color = Color.yellow;
    //    Vector3 center = new Vector3(bounds.center.x, bounds.center.y, transform.position.z);
    //    Vector3 size = new Vector3(bounds.width, bounds.height, 0.1f);

    //    Gizmos.DrawWireCube(center, size);

    //    // Draw corner markers
    //    Gizmos.color = Color.red;
    //    float markerSize = 0.2f;

    //    // Bottom-left
    //    Gizmos.DrawSphere(new Vector3(bounds.xMin, bounds.yMin, transform.position.z), markerSize);
    //    // Bottom-right  
    //    Gizmos.DrawSphere(new Vector3(bounds.xMax, bounds.yMin, transform.position.z), markerSize);
    //    // Top-left
    //    Gizmos.DrawSphere(new Vector3(bounds.xMin, bounds.yMax, transform.position.z), markerSize);
    //    // Top-right
    //    Gizmos.DrawSphere(new Vector3(bounds.xMax, bounds.yMax, transform.position.z), markerSize);
    //}
}

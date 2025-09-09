using UnityEngine;

public enum PlatformDirection
{
    TopOnly,      // Solid from top, passable from bottom (roof)
    LeftOnly,     // Solid from left, passable from right  
    RightOnly,    // Solid from right, passable from left
    BottomOnly    // Solid from bottom, passable from top
}

public class OneWayPlatform : MonoBehaviour
{
    [Header("One-Way Platform Settings")]
    public PlatformDirection platformDirection = PlatformDirection.TopOnly;
    
    [Header("Detection Settings")]
    public float directionThreshold = 0.1f;
    public float velocityThreshold = 0.5f;
    
    private BoxCollider2D platformCollider;
    private PlayerController currentPlayer;
    
    private void Start()
    {
        platformCollider = GetComponent<BoxCollider2D>();
        if (platformCollider == null)
        {
            Debug.LogError("OneWayPlatform requires a BoxCollider2D component!");
        }
    }
    
    private void Update()
    {
        if (currentPlayer != null)
        {
            UpdatePlatformState();
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            currentPlayer = other.GetComponent<PlayerController>();
            if (currentPlayer != null)
            {
                UpdatePlatformState();
            }
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            currentPlayer = null;
            // Reset to solid when player leaves
            SetPlatformSolid(true);
        }
    }
    
    private void UpdatePlatformState()
    {
        if (currentPlayer == null) return;
        
        Vector3 playerPos = currentPlayer.transform.position;
        Vector3 platformPos = transform.position;
        Rigidbody2D playerRb = currentPlayer.GetComponent<Rigidbody2D>();
        
        bool shouldBePassable = ShouldAllowPassThrough(playerPos, platformPos, playerRb.linearVelocity);
        SetPlatformSolid(!shouldBePassable);
    }
    
    private bool ShouldAllowPassThrough(Vector3 playerPos, Vector3 platformPos, Vector2 playerVelocity)
    {
        // Check if player has enough velocity to warrant direction checking
        if (playerVelocity.magnitude < velocityThreshold)
        {
            // If moving too slowly, default to solid
            return false;
        }
        
        switch (platformDirection)
        {
            case PlatformDirection.TopOnly:
                // Roof: passable from bottom (player moving up), solid from top (player moving down)
                return playerPos.y < platformPos.y && playerVelocity.y > directionThreshold;
                
            case PlatformDirection.LeftOnly:
                // Left wall: passable from right (player moving left), solid from left (player moving right)
                return playerPos.x > platformPos.x && playerVelocity.x < -directionThreshold;
                
            case PlatformDirection.RightOnly:
                // Right wall: passable from left (player moving right), solid from right (player moving left)
                return playerPos.x < platformPos.x && playerVelocity.x > directionThreshold;
                
            case PlatformDirection.BottomOnly:
                // Floor: passable from top (player moving down), solid from bottom (player moving up)
                return playerPos.y > platformPos.y && playerVelocity.y < -directionThreshold;
                
            default:
                return false;
        }
    }
    
    private void SetPlatformSolid(bool solid)
    {
        if (platformCollider != null)
        {
            platformCollider.isTrigger = !solid;
        }
    }
    
    private void OnDrawGizmos()
    {
        // Visualize platform direction
        Vector3 center = transform.position;
        Vector3 arrowDirection = Vector3.zero;
        Color gizmoColor = Color.green;
        
        switch (platformDirection)
        {
            case PlatformDirection.TopOnly:
                arrowDirection = Vector3.down; // Arrow points to passable direction
                gizmoColor = Color.green;
                break;
            case PlatformDirection.LeftOnly:
                arrowDirection = Vector3.right;
                gizmoColor = Color.blue;
                break;
            case PlatformDirection.RightOnly:
                arrowDirection = Vector3.left;
                gizmoColor = Color.red;
                break;
            case PlatformDirection.BottomOnly:
                arrowDirection = Vector3.up;
                gizmoColor = Color.yellow;
                break;
        }
        
        Gizmos.color = gizmoColor;
        Vector3 arrowStart = center + Vector3.up * 1.5f;
        Vector3 arrowEnd = arrowStart + arrowDirection * 0.8f;
        
        Gizmos.DrawLine(arrowStart, arrowEnd);
        
        // Draw arrow head
        Vector3 arrowHead1 = arrowEnd + (Quaternion.Euler(0, 0, 30) * -arrowDirection * 0.3f);
        Vector3 arrowHead2 = arrowEnd + (Quaternion.Euler(0, 0, -30) * -arrowDirection * 0.3f);
        
        Gizmos.DrawLine(arrowEnd, arrowHead1);
        Gizmos.DrawLine(arrowEnd, arrowHead2);
        
#if UNITY_EDITOR
        Vector3 labelPos = center + Vector3.up * 2.5f;
        UnityEditor.Handles.Label(labelPos, $"OneWay: {platformDirection}");
#endif
    }
}
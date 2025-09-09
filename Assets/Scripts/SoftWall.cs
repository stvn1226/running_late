using UnityEngine;

public class SoftWall : MonoBehaviour
{
    [Header("Soft Wall Settings")]
    [Tooltip("Speed threshold - player must be moving faster than this to pass through")]
    public float speedThreshold = 3f;
    
    [Tooltip("Allow wall running while passing through")]
    public bool allowWallRunning = true;
    
    [Tooltip("Wall direction - which side can the player wall run from?")]
    public PlatformDirection wallDirection = PlatformDirection.LeftOnly;
    
    private BoxCollider2D wallCollider;
    private GameObject wallRunDetector;
    private PlayerController currentPlayer;
    private bool playerInside = false;
    
    private void Start()
    {
        wallCollider = GetComponent<BoxCollider2D>();
        wallRunDetector = transform.Find("WallRunDetector")?.gameObject;
        
        if (wallCollider == null)
        {
            Debug.LogError("SoftWall requires a BoxCollider2D component!");
        }
        
        // Start as soft (trigger)
        wallCollider.isTrigger = true;
        
        if (wallRunDetector == null)
        {
            Debug.LogError("SoftWall requires a WallRunDetector child object!");
        }
        else
        {
            BoxCollider2D wallRunCollider = wallRunDetector.GetComponent<BoxCollider2D>();
            if (wallRunCollider != null)
            {
                wallRunCollider.isTrigger = false; // Wall run detector should be solid
            }
        }
    }
    
    private void Update()
    {
        if (currentPlayer != null)
        {
            UpdateWallState();
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                currentPlayer = player;
                HandlePlayerEnter(player);
            }
        }
    }
    
    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player") && playerInside && currentPlayer != null)
        {
            ManagePlayerInWall(currentPlayer);
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
            currentPlayer = null;
            // Reset to soft when player leaves
            MakeWallSoft();
        }
    }
    
    private void UpdateWallState()
    {
        if (currentPlayer == null) return;
        
        Vector3 playerPos = currentPlayer.transform.position;
        Vector3 wallPos = transform.position;
        Rigidbody2D playerRb = currentPlayer.GetComponent<Rigidbody2D>();
        
        // Determine which side player is approaching from
        bool approachingFromWallRunSide = IsApproachingFromWallRunSide(playerPos, wallPos, playerRb.linearVelocity);
        
        // Check if wall run button is pressed
        bool wallRunPressed = IsWallRunButtonPressed();
        
        // Wall should be HARD only if:
        // 1. Player is holding wall run button AND
        // 2. Player is approaching from the wall running side
        bool shouldBeHard = wallRunPressed && approachingFromWallRunSide;
        
        if (shouldBeHard)
        {
            MakeWallHard();
        }
        else
        {
            MakeWallSoft();
        }
    }
    
    private bool IsApproachingFromWallRunSide(Vector3 playerPos, Vector3 wallPos, Vector2 playerVelocity)
    {
        // Check which side the player is on and their velocity direction
        switch (wallDirection)
        {
            case PlatformDirection.LeftOnly:
                // Left wall - player should be on the left side and moving right
                return playerPos.x <= wallPos.x && playerVelocity.x > 0.1f;
                
            case PlatformDirection.RightOnly:
                // Right wall - player should be on the right side and moving left
                return playerPos.x >= wallPos.x && playerVelocity.x < -0.1f;
                
            default:
                return false;
        }
    }
    
    private bool IsWallRunButtonPressed()
    {
        if (currentPlayer == null) return false;
        
        // Try to get the new modular controller first
        PlayerControllerModular modularController = currentPlayer.GetComponent<PlayerControllerModular>();
        if (modularController != null)
        {
            PlayerStates states = modularController.GetStates();
            return states != null ? states.IsWallRunHeld : false;
        }
        
        // Fallback to original PlayerController if modular version not found
        PlayerController originalController = currentPlayer.GetComponent<PlayerController>();
        if (originalController != null)
        {
            return originalController.IsWallRunHeld;
        }
        
        return false;
    }
    
    private void MakeWallHard()
    {
        if (wallCollider != null)
        {
            wallCollider.isTrigger = false; // Solid
        }
        EnableWallRunning();
    }
    
    private void MakeWallSoft()
    {
        if (wallCollider != null)
        {
            wallCollider.isTrigger = true; // Passable
        }
        EnableWallRunning();
    }
    
    private void HandlePlayerEnter(PlayerController player)
    {
        playerInside = true;
        UpdateWallState(); // Immediately update state when player enters
    }
    
    private void ManagePlayerInWall(PlayerController player)
    {
        // If player gets stuck while inside soft wall, help them get unstuck
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        
        if (Mathf.Abs(playerRb.linearVelocity.x) < 0.5f && wallCollider.isTrigger)
        {
            PushPlayerOut(player);
        }
    }
    
    private void PushPlayerOut(PlayerController player)
    {
        Vector3 playerPos = player.transform.position;
        Vector3 wallPos = transform.position;
        
        // Determine which side is closer and push player there
        float direction = playerPos.x > wallPos.x ? 1f : -1f;
        
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        Vector2 pushForce = Vector2.right * direction * 300f;
        
        playerRb.AddForce(pushForce, ForceMode2D.Force);
    }
    
    private void EnableWallRunning()
    {
        if (wallRunDetector != null && allowWallRunning)
        {
            BoxCollider2D wallRunCollider = wallRunDetector.GetComponent<BoxCollider2D>();
            if (wallRunCollider != null)
            {
                wallRunCollider.enabled = true;
            }
        }
    }
    
    private void OnDrawGizmos()
    {
        // Visualize wall state and direction
        Color gizmoColor;
        string stateText;
        
        if (currentPlayer != null && !wallCollider.isTrigger)
        {
            // Hard wall
            gizmoColor = new Color(1f, 0f, 0f, 0.6f); // Red
            stateText = "HARD";
        }
        else
        {
            // Soft wall
            gizmoColor = new Color(0f, 1f, 0f, 0.3f); // Green
            stateText = "SOFT";
        }
        
        Gizmos.color = gizmoColor;
        
        if (wallCollider != null)
        {
            Gizmos.DrawCube(transform.position, wallCollider.size);
        }
        else
        {
            Gizmos.DrawCube(transform.position, Vector3.one * 0.5f);
        }
        
        // Draw arrow showing wall run direction
        Gizmos.color = Color.white;
        Vector3 arrowPos = transform.position + Vector3.up * 2f;
        Vector3 arrowDirection = Vector3.zero;
        
        switch (wallDirection)
        {
            case PlatformDirection.LeftOnly:
                arrowDirection = Vector3.right; // Arrow points right (approach from left)
                break;
            case PlatformDirection.RightOnly:
                arrowDirection = Vector3.left; // Arrow points left (approach from right)
                break;
        }
        
        if (arrowDirection != Vector3.zero)
        {
            Gizmos.DrawLine(arrowPos, arrowPos + arrowDirection * 0.8f);
        }
        
#if UNITY_EDITOR
        Vector3 labelPos = transform.position + Vector3.up * 3f;
        UnityEditor.Handles.Label(labelPos, $"{stateText}\n{wallDirection}\nWall Run Side");
#endif
    }
}
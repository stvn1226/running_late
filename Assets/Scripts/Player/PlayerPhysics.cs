using UnityEngine;

public class PlayerPhysics : MonoBehaviour
{
    [Header("References")]
    public PlayerConfig config;

    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private CapsuleCollider2D playerCollider;
    [SerializeField] private PlayerStates states;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Vector2 originalColliderSize;
    [SerializeField] private Vector2 originalColliderOffset;
    [SerializeField] private Vector2 crouchColliderSize;
    [SerializeField] private Vector2 crouchColliderOffset;

    // Logging variables
    private bool previousGrounded;
    private bool previousCanWallRun;
    private int previousWallDirection;
    private float lastPhysicsLog = 0f;
    private const float PHYSICS_LOG_INTERVAL = 1f;

    private void Awake()
    {
        if (playerCollider == null)
        {
            GameLogger.LogError("PHYSICS", "No CapsuleCollider2D found in children!");
        }
        
        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }
        
        originalColliderSize = playerCollider.size;
        originalColliderOffset = playerCollider.offset;
        
        GameLogger.LogPhysics($"Initialized - Collider: {playerCollider.name}, Size: {originalColliderSize}, Offset: {originalColliderOffset}");
    }

    private void Start()
    {
        if (config != null)
        {
            crouchColliderSize = new Vector2(
                originalColliderSize.x * config.crouchSizeMultiplier.x, 
                originalColliderSize.y * config.crouchSizeMultiplier.y
            );
            
            crouchColliderOffset = new Vector2(
                originalColliderOffset.x, 
                originalColliderOffset.y - (originalColliderSize.y - crouchColliderSize.y) * 0.5f
            );
            
            GameLogger.LogPhysics($"Config loaded - CrouchSize: {crouchColliderSize}, CrouchOffset: {crouchColliderOffset}");
            GameLogger.LogPhysics($"Ground layers: {string.Join(", ", config.groundLayer.ToString())}");
            GameLogger.LogPhysics($"Wall layers: {string.Join(", ", config.wallLayer.ToString())}");
        }
    }

    private void Update()
    {
        CheckGrounded();
        CheckWallRun();
        
        // Log physics state changes
        if (states.isGrounded != previousGrounded)
        {
            GameLogger.LogPhysics($"GROUNDED CHANGE: {previousGrounded} -> {states.isGrounded}, Velocity.y: {rb.linearVelocity.y:F2}");
            
            if (states.isGrounded && !previousGrounded && playerMovement != null)
            {
                playerMovement.HandleLanding();
            }
        }

        if (states.isGrounded && !previousGrounded && playerMovement != null)
        {
            playerMovement.HandleLanding();

            PlayerAirDodge airDodge = GetComponent<PlayerAirDodge>();
            if (airDodge != null)
            {
                airDodge.OnLanding();
            }

            PlayerDive dive = GetComponent<PlayerDive>();
            if (dive != null)
            {
                dive.OnLanding();
            }
        }

        if (states.canWallRun != previousCanWallRun || states.wallDirection != previousWallDirection)
        {
            GameLogger.LogPhysics($"WALL RUN CHANGE: CanWallRun={states.canWallRun}, Direction={states.wallDirection}, Input.x={states.moveInput.x:F2}");
        }
        
        // Periodic physics logging
        if (Time.time - lastPhysicsLog > PHYSICS_LOG_INTERVAL)
        {
            GameLogger.LogVerbose("PHYSICS", $"STATUS: Grounded={states.isGrounded}, CanWallRun={states.canWallRun}, WallDir={states.wallDirection}, Pos={transform.position}, ColliderPos={playerCollider.transform.position}");
            lastPhysicsLog = Time.time;
        }
        
        previousGrounded = states.isGrounded;
        previousCanWallRun = states.canWallRun;
        previousWallDirection = states.wallDirection;
        
        states.wasGrounded = states.isGrounded;
    }

    private void CheckGrounded()
    {
        if (config == null) return;

        // Vector2 boxCenter = (Vector2)transform.position + playerCollider.offset;
        Vector2 boxCenter = (Vector2)playerCollider.transform.position + playerCollider.offset;
        Vector2 boxSize = playerCollider.size;

        RaycastHit2D hit = Physics2D.BoxCast(boxCenter, boxSize, 0f, Vector2.down, config.groundCheckDistance, config.groundLayer);
        
        bool newGrounded = hit.collider != null && rb.linearVelocity.y <= 0.1f;
        
        if (hit.collider != null && states.isGrounded != newGrounded)
        {
            string hitObjectInfo = $"{hit.collider.name} (Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}, Tag: {hit.collider.tag})";
            GameLogger.LogPhysics($"GROUND CHECK HIT: {hitObjectInfo}, VelY: {rb.linearVelocity.y:F2}, Distance: {hit.distance:F3}");
        }
        
        states.isGrounded = newGrounded;
    }

    private void CheckWallRun()
    {
        if (config == null) return;

        if (states.isGrounded)
        {
            if (states.canWallRun) GameLogger.LogVerbose("PHYSICS", "Wall run disabled - player grounded");
            states.canWallRun = false;
            return;
        }

        Vector2 boxCenter = (Vector2)transform.position + playerCollider.offset;
        Vector2 boxSize = new Vector2(config.wallCheckDistance, playerCollider.size.y);

        // Check right wall
        RaycastHit2D rightHit = Physics2D.BoxCast(boxCenter, boxSize, 0f, Vector2.right, config.wallCheckDistance, config.wallLayer);
        // Check left wall
        RaycastHit2D leftHit = Physics2D.BoxCast(boxCenter, boxSize, 0f, Vector2.left, config.wallCheckDistance, config.wallLayer);

        bool previousCanWallRun = states.canWallRun;
        
        if (rightHit.collider != null && states.moveInput.x > 0.1f)
        {
            states.canWallRun = true;
            states.wallDirection = 1; // Right wall
            if (!previousCanWallRun) 
            {
                string hitInfo = $"{rightHit.collider.name} (Layer: {LayerMask.LayerToName(rightHit.collider.gameObject.layer)}, Tag: {rightHit.collider.tag})";
                GameLogger.LogPhysics($"RIGHT WALL DETECTED: {hitInfo}, Distance: {rightHit.distance:F3}");
            }
        }
        else if (leftHit.collider != null && states.moveInput.x < -0.1f)
        {
            states.canWallRun = true;
            states.wallDirection = -1; // Left wall
            if (!previousCanWallRun)
            {
                string hitInfo = $"{leftHit.collider.name} (Layer: {LayerMask.LayerToName(leftHit.collider.gameObject.layer)}, Tag: {leftHit.collider.tag})";
                GameLogger.LogPhysics($"LEFT WALL DETECTED: {hitInfo}, Distance: {leftHit.distance:F3}");
            }
        }
        else
        {
            if (states.canWallRun)
            {
                GameLogger.LogVerbose("PHYSICS", $"Wall run lost - RightHit: {rightHit.collider != null}, LeftHit: {leftHit.collider != null}, Input.x: {states.moveInput.x:F2}");
            }
            states.canWallRun = false;
        }
    }

    public void UpdateColliderSize()
    {
        bool shouldCrouch = states.isCrouching || states.isSliding || states.isRolling;
        CapsuleDirection2D newDirection = shouldCrouch ? CapsuleDirection2D.Horizontal : CapsuleDirection2D.Vertical;
        Vector2 newSize = shouldCrouch ? crouchColliderSize : originalColliderSize;
        Vector2 newOffset = shouldCrouch ? crouchColliderOffset : originalColliderOffset;
        
        GameLogger.LogPhysics($"COLLIDER UPDATE: Crouch={shouldCrouch}, Size={newSize}, Offset={newOffset}, Direction={newDirection}");
        
        playerCollider.direction = newDirection;
        playerCollider.size = newSize;
        playerCollider.offset = newOffset;
    }

    private void OnDrawGizmosSelected()
    {
        if (playerCollider == null || config == null) return;

        Vector2 boxCenter = (Vector2)transform.position + playerCollider.offset;
        Vector2 boxSize = playerCollider.size;

        // Draw ground check
        Gizmos.color = states != null && states.isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireCube(boxCenter + Vector2.down * config.groundCheckDistance, boxSize);

        // Draw wall check
        Gizmos.color = states != null && states.canWallRun ? Color.blue : Color.gray;
        Vector2 wallCheckSize = new Vector2(config.wallCheckDistance, boxSize.y);
        Gizmos.DrawWireCube(boxCenter + Vector2.right * config.wallCheckDistance, wallCheckSize);
        Gizmos.DrawWireCube(boxCenter + Vector2.left * config.wallCheckDistance, wallCheckSize);
    }
}

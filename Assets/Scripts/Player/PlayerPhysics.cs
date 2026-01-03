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

    // Wall height detection
    private float lastWallHeightCheck = 0f;

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
        CheckSlope();
        
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
            GameLogger.Log("PHYSICS", $"STATUS: Grounded={states.isGrounded}, CanWallRun={states.canWallRun}, WallDir={states.wallDirection}, Pos={transform.position}, ColliderPos={playerCollider.transform.position}");
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

        Vector2 boxCenter = (Vector2)playerCollider.transform.position + playerCollider.offset;
        Vector2 boxSize = playerCollider.size;

        RaycastHit2D hit = Physics2D.BoxCast(boxCenter, boxSize, 0f, Vector2.down, config.groundCheckDistance, config.groundLayer);
        
        bool newGrounded = hit.collider != null && rb.linearVelocity.y <= 0.1f;
        
        states.isGrounded = newGrounded;
    }

    private void CheckWallRun()
    {
        if (config == null) return;

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
            states.wallDirection = 1;
            if (!previousCanWallRun) 
            {
                states.RecordWallContact();
                string hitInfo = $"{rightHit.collider.name} (Layer: {LayerMask.LayerToName(rightHit.collider.gameObject.layer)}, Tag: {rightHit.collider.tag})";
                GameLogger.LogPhysics($"RIGHT WALL DETECTED: {hitInfo}, Distance: {rightHit.distance:F3}");
            }
            DetectWallHeight(rightHit, 1);
        }
        else if (leftHit.collider != null && states.moveInput.x < -0.1f)
        {
            states.canWallRun = true;
            states.wallDirection = -1;
            if (!previousCanWallRun)
            {
                states.RecordWallContact();
                string hitInfo = $"{leftHit.collider.name} (Layer: {LayerMask.LayerToName(leftHit.collider.gameObject.layer)}, Tag: {leftHit.collider.tag})";
                GameLogger.LogPhysics($"LEFT WALL DETECTED: {hitInfo}, Distance: {leftHit.distance:F3}");
            }
            DetectWallHeight(leftHit, -1);
        }
        else
        {
            if (states.canWallRun)
            {
                GameLogger.Log("PHYSICS", $"Wall run lost - RightHit: {rightHit.collider != null}, LeftHit: {leftHit.collider != null}, Input.x: {states.moveInput.x:F2}");
            }
            states.canWallRun = false;
        }
    }

    private void CheckSlope()
    {
        if (config == null || !config.enableSlopeDetection)
        {
            states.slopeNormal = Vector2.up;
            states.slopeAngle = 0f;
            states.isOnSlope = false;
            states.isSlopeTooSteep = false;
            return;
        }

        Vector2 playerBottom = (Vector2)playerCollider.transform.position + 
                               playerCollider.offset - 
                               Vector2.up * (playerCollider.size.y * 0.5f);
    
        float checkDistance = config.slopeCheckDistance + 0.15f;
    
        RaycastHit2D hit = Physics2D.Raycast(
            playerBottom, 
            Vector2.down, 
            checkDistance, 
            config.groundLayer
        );

        bool wasOnSlope = states.isOnSlope;

        if (hit.collider != null)
        {
            states.slopeNormal = hit.normal;
            states.slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            states.isOnSlope = states.slopeAngle > 0.1f;
            states.isSlopeTooSteep = states.slopeAngle > config.maxWalkableSlope;

            if (states.isOnSlope && !wasOnSlope)
            {
                GameLogger.Log("SLOPE", $"ENTERED SLOPE: Angle={states.slopeAngle:F1}°, Normal={states.slopeNormal}, TooSteep={states.isSlopeTooSteep}, Obj={hit.collider.name}");
            }
            else if (states.isOnSlope)
            {
                GameLogger.LogVerbose("SLOPE", $"ON SLOPE: Angle={states.slopeAngle:F1}°, Normal={states.slopeNormal}, Grounded={states.isGrounded}, VelY={rb.linearVelocity.y:F2}");
            }
        }
        else
        {
            if (wasOnSlope)
            {
                GameLogger.Log("SLOPE", "EXITED SLOPE: No raycast hit");
            }
            
            states.slopeNormal = Vector2.up;
            states.slopeAngle = 0f;
            states.isOnSlope = false;
            states.isSlopeTooSteep = false;
        }
    }
    
    private void DetectWallHeight(RaycastHit2D wallHit, int direction)
    {
        if (Time.time - lastWallHeightCheck < config.wallHeightCheckInterval)
        {
            return;
        }

        lastWallHeightCheck = Time.time;

        Vector2 rayStart = (Vector2)transform.position + playerCollider.offset;
        
        states.distanceToWallTop = config.maxWallHeightCheck;
        states.isNearWallTop = false;

        for (float h = 0.5f; h < config.maxWallHeightCheck; h += 0.5f)
        {
            Vector2 checkPos = rayStart + Vector2.up * h;
            RaycastHit2D check = Physics2D.Raycast(
                checkPos, 
                Vector2.right * direction, 
                config.wallCheckDistance * 1.5f, 
                config.wallLayer
            );

            if (check.collider == null)
            {
                states.distanceToWallTop = h;
                states.isNearWallTop = h < config.wallVaultTriggerDistance;
                
                GameLogger.Log("PHYSICS", $"Wall top detected at height: {h:F2}, Near top: {states.isNearWallTop}");
                break;
            }
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

        // Slope Visualization
        if (config.enableSlopeDetection && states != null)
        {
            Vector2 playerBottom = (Vector2)transform.position + 
                                   playerCollider.offset - 
                                   Vector2.up * (playerCollider.size.y * 0.5f);
    
            float checkDistance = config.slopeCheckDistance + 0.15f;
    
            Gizmos.color = states.isOnSlope ? Color.yellow : Color.cyan;
            Gizmos.DrawLine(playerBottom, playerBottom + Vector2.down * checkDistance);
    
            if (states.isOnSlope)
            {
                Gizmos.color = states.isSlopeTooSteep ? Color.red : Color.green;
                Gizmos.DrawLine(playerBottom, playerBottom + states.slopeNormal * 2f);
            }
        }
    }
}

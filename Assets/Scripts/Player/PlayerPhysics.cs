#define ENABLE_VERBOSE_LOGGING

using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerPhysics : MonoBehaviour
{
    [Header("References")]
    public PlayerConfig config;

    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private CapsuleCollider2D playerCollider;
    [SerializeField] private PlayerStates states;
    [SerializeField] private SpriteRenderer playerSprite;
    [SerializeField] private Vector2 originalColliderSize;
    [SerializeField] private Vector2 originalColliderOffset;
    [SerializeField] private Vector2 crouchColliderSize;
    [SerializeField] private Vector2 crouchColliderOffset;

    private bool previousGrounded;
    private bool previousCanWallRun;
    private int previousWallDirection;
    private float lastPhysicsLog = 0f;
    private const float PHYSICS_LOG_INTERVAL = 1f;
    private const int WALLRUN_DEPTH_THRESHOLD = -12;
    private const float WALL_CONTACT_GRACE_PERIOD = 0.05f; // REDUCED: 0.2f -> 0.05f for snappy disconnect

    private Collider2D currentWallCollider;
    private BuildingWallsTrigger currentWallTrigger;
    private float wallContactLostTime;

    private float lastWallHeightCheck = 0f;
    private float groundedBufferTimer = 0f;
    private const float GROUNDED_BUFFER_TIME = 0.1f;
    private bool hasLoggedDetection = false;

    private void Awake()
    {
        if (playerCollider == null)
        {
            GameLogger.LogError("PHYSICS", "No CapsuleCollider2D found in children!");
        }
        
        if (playerSprite == null)
        {
            playerSprite = GetComponentInChildren<SpriteRenderer>();
            if (playerSprite == null)
            {
                GameLogger.LogError("PHYSICS", "No SpriteRenderer found in children! Wall depth detection will not work.");
            }
        }
        
        originalColliderSize = playerCollider.size;
        originalColliderOffset = playerCollider.offset;
        
        PhysicsMaterial2D noBounceMaterial = new PhysicsMaterial2D("PlayerNoBounce");
        noBounceMaterial.friction = 0f;
        noBounceMaterial.bounciness = 0f;
        playerCollider.sharedMaterial = noBounceMaterial;
        rb.sharedMaterial = noBounceMaterial;
        
        GameLogger.LogPhysics($"Initialized - Collider: {playerCollider.name}, Size: {originalColliderSize}, Offset: {originalColliderOffset}, Material: NoBounceFriction");
        
        if (playerSprite != null)
        {
            GameLogger.LogPhysics($"Player Sprite - Layer: {playerSprite.sortingLayerName}, Order: {playerSprite.sortingOrder}");
        }
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

    private void FixedUpdate()
    {
        CheckSlope();
        CheckGrounded();
        CheckWallRun();
    }

    private void Update()
    {
        if (states.isGrounded != previousGrounded)
        {
#if ENABLE_VERBOSE_LOGGING
            GameLogger.LogPhysics($"GROUNDED CHANGE: {previousGrounded} -> {states.isGrounded}");
#endif
            
            if (states.isGrounded && !previousGrounded)
            {
                states.OnLanded?.Invoke();
            }
        }

        if (states.canWallRun != previousCanWallRun || states.wallDirection != previousWallDirection)
        {
#if ENABLE_VERBOSE_LOGGING
            GameLogger.LogPhysics($"WALL RUN CHANGE: CanWallRun={states.canWallRun}, Direction={states.wallDirection}");
#endif
        }
        
#if ENABLE_VERBOSE_LOGGING
        if (Time.time - lastPhysicsLog > PHYSICS_LOG_INTERVAL)
        {
            GameLogger.Log("PHYSICS", $"STATUS: Grounded={states.isGrounded}, CanWallRun={states.canWallRun}");
            lastPhysicsLog = Time.time;
        }
#endif
        
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

        float extraDistance = states.isOnSlope ? config.slopeGroundCheckExtra : 0f;
        RaycastHit2D hit = Physics2D.BoxCast(boxCenter, boxSize, 0f, Vector2.down, config.groundCheckDistance + extraDistance, config.groundLayer);
        
        bool hitGround = hit.collider != null;
        
        if (hitGround)
        {
            if (states.isOnSlope)
            {
                bool shouldBeGrounded = rb.linearVelocity.y <= config.slopeGroundedYVelocityThreshold;
                if (shouldBeGrounded)
                {
                    states.isGrounded = true;
                    groundedBufferTimer = GROUNDED_BUFFER_TIME;
                }
                else
                {
                    if (groundedBufferTimer > 0f)
                    {
                        states.isGrounded = true;
                        groundedBufferTimer -= Time.fixedDeltaTime;
                    }
                    else
                    {
                        states.isGrounded = false;
                    }
                }
            }
            else
            {
                bool shouldBeGrounded = rb.linearVelocity.y <= 0.1f;
                if (shouldBeGrounded)
                {
                    states.isGrounded = true;
                    groundedBufferTimer = GROUNDED_BUFFER_TIME;
                }
                else
                {
                    states.isGrounded = false;
                    groundedBufferTimer = 0f;
                }
            }
        }
        else
        {
            states.isGrounded = false;
            groundedBufferTimer = 0f;
        }
    }

    private void CheckWallRun()
    {
        if (config == null) return;

        if (states.isOnSlope)
        {
            states.canWallRun = false;
            return;
        }

        Vector2 boxCenter = (Vector2)transform.position + playerCollider.offset;
        float directionX = states.moveInput.x;
        float detectionDistance = config.wallCheckDistance;
        
        // Multi-Ray Detection (Top, Center, Bottom) to prevent gaps
        Vector2 topOrigin = boxCenter + Vector2.up * (playerCollider.size.y * 0.4f);
        Vector2 bottomOrigin = boxCenter + Vector2.down * (playerCollider.size.y * 0.4f);
        
        bool wallRight = false;
        bool wallLeft = false;
        Collider2D rightHit = null;
        Collider2D leftHit = null;

        // Helper to check for visual obstruction by foreground buildings
        bool IsObstructed(RaycastHit2D hit)
        {
            BuildingDetector targetBuilding = hit.collider.GetComponentInParent<BuildingDetector>();
            if (targetBuilding == null) 
            {
                // GameLogger.Log("PHYSICS", $"[OBSTRUCTION] No BuildingDetector found on {hit.collider.name}");
                return false;
            }

            float playerZ = transform.position.z;
            float targetDist = Mathf.Abs(targetBuilding.buildingZ - playerZ);
            
            // Check all colliders at the hit point to see if any belong to a closer building
            Collider2D[] overlappers = Physics2D.OverlapPointAll(hit.point);
            
            foreach (Collider2D col in overlappers)
            {
                // Optimization: Skip self and the specific wall collider we hit
                if (col == hit.collider) continue;
                
                BuildingDetector blocker = col.GetComponentInParent<BuildingDetector>();
                
                if (blocker != null && blocker != targetBuilding)
                {
                    // ROBUST LANE LOGIC: Use Sorting Order (Integer)
                    // Higher sorting order = Closer to camera = In Front
                    bool isCloser = blocker.sortingOrder > targetBuilding.sortingOrder;
                    
                    // Debug Log for potential blockers
                    GameLogger.Log("OBSTRUCTION", $"Target: {targetBuilding.buildingName} (Order:{targetBuilding.sortingOrder}) | Blocker: {blocker.buildingName} (Order:{blocker.sortingOrder}) | IsCloser: {isCloser}");

                    // If blocker is CLOSER to player/camera than target, it's an obstruction
                    if (isCloser) 
                    {
                        GameLogger.Log("OBSTRUCTION", $"BLOCKED! Wall on {targetBuilding.buildingName} (Order:{targetBuilding.sortingOrder}) blocked by {blocker.buildingName} (Order:{blocker.sortingOrder})");
                        return true;
                    }
                }
            }
            return false;
        }

        // Helper function for 3-ray check with skin offset to avoid "inside collider" failure
        Collider2D CheckDirection(Vector2 direction)
        {
            float skinOffset = 0.1f; // Step back 10cm from center
            Vector2 offset = -direction * skinOffset;
            
            Vector2 rayCenter = boxCenter + offset;
            Vector2 rayTop = topOrigin + offset;
            Vector2 rayBottom = bottomOrigin + offset;
            float adjustedDistance = detectionDistance + skinOffset;

            RaycastHit2D hitCenter = Physics2D.Raycast(rayCenter, direction, adjustedDistance, config.wallLayer);
            if (hitCenter.collider != null && !IsObstructed(hitCenter)) return hitCenter.collider;
            
            RaycastHit2D hitTop = Physics2D.Raycast(rayTop, direction, adjustedDistance, config.wallLayer);
            if (hitTop.collider != null && !IsObstructed(hitTop)) return hitTop.collider;
            
            RaycastHit2D hitBottom = Physics2D.Raycast(rayBottom, direction, adjustedDistance, config.wallLayer);
            if (hitBottom.collider != null && !IsObstructed(hitBottom)) return hitBottom.collider;
            
            return null;
        }

        // Helper for ground fallback (Overlap detection)
        Collider2D CheckOverlapFallback(Vector2 direction)
        {
            // Only use fallback if grounded (where raycasts are most likely to fail due to overlap)
            if (!states.isGrounded) return null;

            float overlapWidth = 0.2f;
            Vector2 checkSize = new Vector2(overlapWidth, playerCollider.size.y * 0.8f);
            Vector2 checkPos = boxCenter + direction * (playerCollider.size.x * 0.5f);

            Collider2D[] cols = Physics2D.OverlapBoxAll(checkPos, checkSize, 0f, config.wallLayer);
            foreach (var col in cols)
            {
                // Simple obstruction check approximation for overlap
                BuildingDetector targetBuilding = col.GetComponentInParent<BuildingDetector>();
                if (targetBuilding != null)
                {
                    // Since OverlapPointAll is already used in IsObstructed, we can reuse similar logic
                    // For a low-risk hotfix, we return the first one found that isn't a trigger (unless it's SoftWall)
                    // and let the rest of the flow handle SetSolid.
                    return col;
                }
            }
            return null;
        }


        // RIGHT CHECK
        rightHit = CheckDirection(Vector2.right);
        if (rightHit == null) rightHit = CheckOverlapFallback(Vector2.right);
        wallRight = rightHit != null;

        // LEFT CHECK
        leftHit = CheckDirection(Vector2.left);
        if (leftHit == null) leftHit = CheckOverlapFallback(Vector2.left);
        wallLeft = leftHit != null;

        // Logic: prioritize direction of input
        bool wallDetected = false;
        
        if (wallRight && directionX > 0.1f)
        {
            currentWallCollider = rightHit;
            states.canWallRun = true;
            states.wallDirection = 1;

            // Side detection for flip lock (LWall/RWall tags)
            int side = TryGetWallSide(rightHit);
            if (side != 0) states.wallDirection = side;
            
            wallDetected = true;
            
            var wallTrigger = rightHit.GetComponentInParent<BuildingWallsTrigger>();
            if (states.wallRunHeld)
            {
                SetCurrentWall(wallTrigger);
            }
            else
            {
                ClearCurrentWall();
            }

            if (!previousCanWallRun) 
            {
                states.RecordWallContact();
                GameLogger.Log("PHYSICS", $"[WALLRUN] Started RIGHT on {rightHit.name}");
            }
            DetectWallHeight(rightHit, 1);
        }
        else if (wallLeft && directionX < -0.1f)
        {
            currentWallCollider = leftHit;
            states.canWallRun = true;
            states.wallDirection = -1;

            // Side detection for flip lock (LWall/RWall tags)
            int side = TryGetWallSide(leftHit);
            if (side != 0) states.wallDirection = side;

            wallDetected = true;
            
            var wallTrigger = leftHit.GetComponentInParent<BuildingWallsTrigger>();
            if (states.wallRunHeld)
            {
                SetCurrentWall(wallTrigger);
            }
            else
            {
                ClearCurrentWall();
            }

            if (!previousCanWallRun)
            {
                states.RecordWallContact();
                GameLogger.Log("PHYSICS", $"[WALLRUN] Started LEFT on {leftHit.name}");
            }
            DetectWallHeight(leftHit, -1);
        }
        else
        {
            ClearCurrentWall();
            // Grace period: Use previous state
            if (previousCanWallRun)
            {
                // Start timer if not already running
                 if (wallContactLostTime == 0f) 
                 {
                     wallContactLostTime = Time.time;
                 }
                 
                 // If timer exceeded, cancel wall run
                 if (Time.time - wallContactLostTime > WALL_CONTACT_GRACE_PERIOD)
                 {
                     states.canWallRun = false;
                     currentWallCollider = null;
                     wallContactLostTime = 0f;
                 }
                 else
                 {
                     // Keep wall run active during grace period
                     states.canWallRun = true;
                 }
            }
            else
            {
                states.canWallRun = false;
                currentWallCollider = null;
                wallContactLostTime = 0f;
            }
        }
        
        // Reset grace timer ONLY IF we actually found a wall this frame
        if (wallDetected) 
        {
            wallContactLostTime = 0f;
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
            states.slopeTangent = Vector2.right;
            return;
        }

        Vector2 playerBottom = (Vector2)playerCollider.transform.position + 
                               playerCollider.offset - 
                               Vector2.up * (playerCollider.size.y * 0.5f);

        float checkDistance = config.slopeCheckDistance + 0.15f;

        RaycastHit2D centerHit = Physics2D.Raycast(playerBottom, Vector2.down, checkDistance, config.groundLayer);
        RaycastHit2D leftHit = Physics2D.Raycast(playerBottom + Vector2.left * 0.25f, Vector2.down, checkDistance, config.groundLayer);
        RaycastHit2D rightHit = Physics2D.Raycast(playerBottom + Vector2.right * 0.25f, Vector2.down, checkDistance, config.groundLayer);

        RaycastHit2D hit = default;
        float closestDist = float.MaxValue;
        
        if (centerHit.collider != null && centerHit.distance < closestDist) { hit = centerHit; closestDist = centerHit.distance; }
        if (leftHit.collider != null && leftHit.distance < closestDist) { hit = leftHit; closestDist = leftHit.distance; }
        if (rightHit.collider != null && rightHit.distance < closestDist) { hit = rightHit; closestDist = rightHit.distance; }

        bool wasOnSlope = states.isOnSlope;

        if (hit.collider != null)
        {
            states.slopeNormal = hit.normal;
            states.slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            
            Vector2 slopeRight = new Vector2(states.slopeNormal.y, -states.slopeNormal.x).normalized;
            states.slopeTangent = slopeRight;
            
            bool isSlopeAngleValid = states.slopeAngle > config.minSlopeAngle && states.slopeAngle <= config.maxWalkableSlope;
            bool shouldBeOnSlope = isSlopeAngleValid;
            
            if (shouldBeOnSlope)
            {
                states.slopeBuffer = 0.15f;
            }
            
            states.isOnSlope = shouldBeOnSlope || states.slopeBuffer > 0f;
            states.isSlopeTooSteep = states.slopeAngle > config.maxWalkableSlope;

            if (states.isOnSlope && !wasOnSlope)
            {
                GameLogger.Log("SLOPE", $"ENTERED SLOPE: Angle={states.slopeAngle:F1}°, Normal={states.slopeNormal}, Tangent={states.slopeTangent}, Obj={hit.collider.name}");
            }
        }
        else
        {
            if (wasOnSlope)
            {
                GameLogger.Log("SLOPE", "EXITED SLOPE: No ground detected");
            }

            states.slopeNormal = Vector2.up;
            states.slopeAngle = 0f;
            states.isOnSlope = false;
            states.isSlopeTooSteep = false;
            states.slopeTangent = Vector2.right;
        }
    }
    
    private void DetectWallHeight(Collider2D wallCollider, int direction)
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

        Gizmos.color = states != null && states.isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireCube(boxCenter + Vector2.down * config.groundCheckDistance, boxSize);

        Gizmos.color = states != null && states.canWallRun ? Color.blue : Color.gray;
        
        // Match the logic in CheckWallRun (Center, Top, Bottom)
        float distance = config.wallCheckDistance;
        Vector2 topOrigin = boxCenter + Vector2.up * (playerCollider.size.y * 0.4f);
        Vector2 bottomOrigin = boxCenter + Vector2.down * (playerCollider.size.y * 0.4f);
        
        // Right Checks
        Gizmos.DrawLine(boxCenter, boxCenter + Vector2.right * distance);
        Gizmos.DrawLine(topOrigin, topOrigin + Vector2.right * distance);
        Gizmos.DrawLine(bottomOrigin, bottomOrigin + Vector2.right * distance);
        
        // Left Checks
        Gizmos.DrawLine(boxCenter, boxCenter + Vector2.left * distance);
        Gizmos.DrawLine(topOrigin, topOrigin + Vector2.left * distance);
        Gizmos.DrawLine(bottomOrigin, bottomOrigin + Vector2.left * distance);

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

    private void SetCurrentWall(BuildingWallsTrigger wall)
    {
        if (currentWallTrigger == wall) return;

        if (currentWallTrigger != null)
        {
            if (config.wallRunDebug) Debug.Log($"[FIX_WALLRUN] Clearing previous wall: {currentWallTrigger.name}");
            currentWallTrigger.SetSolid(false);
        }

        currentWallTrigger = wall;
        
        if (currentWallTrigger != null)
        {
            if (config.wallRunDebug) Debug.Log($"[FIX_WALLRUN] Setting current wall: {currentWallTrigger.name}");
            currentWallTrigger.SetSolid(true);
        }
    }

    private void ClearCurrentWall()
    {
        if (currentWallTrigger != null)
        {
            if (config.wallRunDebug) Debug.Log($"[FIX_WALLRUN] Clearing wall: {currentWallTrigger.name}");
            currentWallTrigger.SetSolid(false);
            currentWallTrigger = null;
        }
    }

    private int TryGetWallSide(Collider2D col)
    {
        // Check self and parents for tags
        Transform current = col.transform;
        while (current != null)
        {
            if (current.CompareTag("LWall")) return 1;
            if (current.CompareTag("RWall")) return -1;
            current = current.parent;
        }
        return 0;
    }
}

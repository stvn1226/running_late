#define ENABLE_VERBOSE_LOGGING

using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerPhysics : MonoBehaviour
{
    [Header("References")]
    public PlayerConfig config;

    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private CapsuleCollider2D playerCollider;
    [SerializeField] private CapsuleCollider2D hitboxCollider;
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
        
        PhysicsMaterial2D noBounceMaterial = new PhysicsMaterial2D("PlayerNoB ounce");
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
        float playerZ = transform.position.z;
        
        // STEP 1: Find ALL buildings nearby
        float searchRadius = config.wallCheckDistance * 4f;
        Collider2D[] allColliders = Physics2D.OverlapCircleAll(boxCenter, searchRadius);
        
        System.Collections.Generic.List<BuildingDetector> nearbyBuildings = new System.Collections.Generic.List<BuildingDetector>();
        
        foreach (Collider2D col in allColliders)
        {
            if (col.CompareTag("Building") && col.isTrigger)
            {
                BuildingDetector detector = col.GetComponent<BuildingDetector>();
                if (detector != null)
                {
                    nearbyBuildings.Add(detector);
                }
            }
        }
        
        if (nearbyBuildings.Count == 0)
        {
            states.canWallRun = false;
            return;
        }
        
        // STEP 2: For EACH building, find its wall colliders and separate by direction
        // Also verify that there's no building blocking the view
        Collider2D rightHit = null;
        Collider2D leftHit = null;
        float rightMinZDiff = float.MaxValue;
        float leftMinZDiff = float.MaxValue;
        BuildingDetector rightBuilding = null;
        BuildingDetector leftBuilding = null;
        
        foreach (BuildingDetector building in nearbyBuildings)
        {
            // CRITICAL: Skip if player is INSIDE this building's trigger
            Collider2D buildingTrigger = building.GetComponent<Collider2D>();
            if (buildingTrigger != null && buildingTrigger.OverlapPoint(transform.position))
            {
                continue;
            }
            
            Collider2D[] buildingColliders = building.GetComponentsInChildren<Collider2D>();
            
            foreach (Collider2D col in buildingColliders)
            {
                // CRITICAL: Skip the building detection collider (the one on the parent GameObject)
                if (col.gameObject == building.gameObject) continue;
                
                // Skip any triggers
                if (col.isTrigger) continue;
                
                // Check if it's a wall layer
                if (((1 << col.gameObject.layer) & config.wallLayer) == 0)
                    continue;
                
                // CRITICAL: Check vertical distance - wall must be within reasonable height range
                float verticalDistance = Mathf.Abs(col.transform.position.y - transform.position.y);
                float maxVerticalDistance = 5f; // Adjust based on your game's scale
                if (verticalDistance > maxVerticalDistance)
                    continue;
                
                // CRITICAL: Raycast from player to BUILDING CENTER (not wall collider)
                // This ensures we detect if another building's BoxCollider2D is blocking
                Vector2 playerPos = transform.position;
                Vector2 buildingCenter = building.transform.position;
                Vector2 direction = (buildingCenter - playerPos).normalized;
                float distance = Vector2.Distance(playerPos, buildingCenter);
                
                // Cast ray to see if any building collider is in the way
                RaycastHit2D[] hits = Physics2D.RaycastAll(playerPos, direction, distance);
                bool isBlocked = false;
                
                foreach (RaycastHit2D hit in hits)
                {
                    // Check if it's a building detection collider (not this building)
                    BuildingDetector blocker = hit.collider.GetComponent<BuildingDetector>();
                    if (blocker != null && blocker != building)
                    {
                        // Another building is blocking this wall
                        // Check if the blocker is visually in front
                        if (Mathf.Abs(blocker.buildingZ - playerZ) < Mathf.Abs(building.buildingZ - playerZ))
                        {
                            isBlocked = true;
                            break;
                        }
                    }
                }
                
                if (isBlocked) continue;
                
                float zDiff = Mathf.Abs(building.buildingZ - playerZ);
                
                // CRITICAL: Use HORIZONTAL raycast only (ignore vertical differences)
                // This prevents detecting walls below when on rooftops
                Vector2 wallPos = col.transform.position;
                Vector2 horizontalDirection = wallPos.x > playerPos.x ? Vector2.right : Vector2.left;
                float horizontalDistance = Mathf.Abs(wallPos.x - playerPos.x);
                
                // Cast a HORIZONTAL ray from player towards the wall
                RaycastHit2D wallCheck = Physics2D.Raycast(playerPos, horizontalDirection, horizontalDistance, config.wallLayer);
                
                // If raycast didn't hit this specific collider, skip it
                if (wallCheck.collider != col)
                    continue;
                
                // Check RIGHT walls
                if (col.transform.position.x > transform.position.x)
                {
                    if (zDiff < rightMinZDiff)
                    {
                        rightMinZDiff = zDiff;
                        rightHit = col;
                        rightBuilding = building;
                    }
                }
                // Check LEFT walls
                else if (col.transform.position.x < transform.position.x)
                {
                    if (zDiff < leftMinZDiff)
                    {
                        leftMinZDiff = zDiff;
                        leftHit = col;
                        leftBuilding = building;
                    }
                }
            }
        }
        
        // Log ONLY when first entering detection AND haven't logged yet
        bool nowDetecting = (rightHit != null || leftHit != null);
        
        if (nowDetecting && !hasLoggedDetection)
        {
            hasLoggedDetection = true;
            string buildingsInfo = "";
            foreach (BuildingDetector bd in nearbyBuildings)
            {
                float zDiff = Mathf.Abs(bd.buildingZ - playerZ);
                buildingsInfo += $"\n  - {bd.buildingName}: Z={bd.buildingZ:F3} ZDiff={zDiff:F3}";
            }
            
            string rightInfo = rightHit != null ? $"RIGHT: {rightHit.name} at X={rightHit.transform.position.x:F1} from {rightBuilding.buildingName} (Z={rightBuilding.buildingZ:F3}, ZDiff={rightMinZDiff:F3})" : "RIGHT: None";
            string leftInfo = leftHit != null ? $"LEFT: {leftHit.name} at X={leftHit.transform.position.x:F1} from {leftBuilding.buildingName} (Z={leftBuilding.buildingZ:F3}, ZDiff={leftMinZDiff:F3})" : "LEFT: None";
            
            GameLogger.Log("WallRunWorkflow", $"[DETECTION] Player XYZ=({transform.position.x:F1},{transform.position.y:F1},{playerZ:F3}) | Buildings={nearbyBuildings.Count}{buildingsInfo}\n  → {rightInfo}\n  → {leftInfo}");
        }
        else if (!nowDetecting && hasLoggedDetection)
        {
            hasLoggedDetection = false;
        }

        bool previousCanWallRun = states.canWallRun;
        
        if (rightHit != null && states.moveInput.x > 0.1f)
        {
            states.canWallRun = true;
            states.wallDirection = 1;
            if (!previousCanWallRun) 
            {
                states.RecordWallContact();
                GameLogger.Log("WallRunWorkflow", $"[WALLRUN START] RIGHT: {rightHit.name} (Building: {rightBuilding.buildingName})");
            }
            DetectWallHeight(rightHit, 1);
        }
        else if (leftHit != null && states.moveInput.x < -0.1f)
        {
            states.canWallRun = true;
            states.wallDirection = -1;
            if (!previousCanWallRun)
            {
                states.RecordWallContact();
                GameLogger.Log("WallRunWorkflow", $"[WALLRUN START] LEFT: {leftHit.name} (Building: {leftBuilding.buildingName})");
            }
            DetectWallHeight(leftHit, -1);
        }
        else
        {
            if (states.canWallRun)
            {
                GameLogger.Log("WallRunWorkflow", $"[WALLRUN END] RightHit: {rightHit != null}, LeftHit: {leftHit != null}, Input.x: {states.moveInput.x:F2}");
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
        hitboxCollider.direction = newDirection;
        hitboxCollider.size = newSize;
        hitboxCollider.offset = newOffset;
    }

    private void OnDrawGizmosSelected()
    {
        if (playerCollider == null || config == null) return;

        Vector2 boxCenter = (Vector2)transform.position + playerCollider.offset;
        Vector2 boxSize = playerCollider.size;

        Gizmos.color = states != null && states.isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireCube(boxCenter + Vector2.down * config.groundCheckDistance, boxSize);

        Gizmos.color = states != null && states.canWallRun ? Color.blue : Color.gray;
        Vector2 wallCheckSize = new Vector2(config.wallCheckDistance, boxSize.y);
        Gizmos.DrawWireCube(boxCenter + Vector2.right * config.wallCheckDistance, wallCheckSize);
        Gizmos.DrawWireCube(boxCenter + Vector2.left * config.wallCheckDistance, wallCheckSize);

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

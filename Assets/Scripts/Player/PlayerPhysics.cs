
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
    private float lastGroundedTime = 0f;
    private const float PHYSICS_LOG_INTERVAL = 1f;
    private const int WALLRUN_DEPTH_THRESHOLD = -12;
    private const float WALL_CONTACT_GRACE_PERIOD = 0.12f;

    private Collider2D currentWallCollider;
    private Collider2D previousWallCollider;
    private BuildingWallsTrigger currentWallTrigger;
    private float wallContactLostTime;
    private bool wasInGrace = false;

    // Cache for Phase 1 logic
    private bool hadValidWallHitThisFrame;
    private float lastWallHitTime;
    private Collider2D lastWallCollider;
    private int lastWallDirection;
    private bool previousIsWallRunning;

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
        CheckWallSlide();
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
        previousIsWallRunning = states.isWallRunning;
        
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
        
        if (states.isGrounded)
        {
            lastGroundedTime = Time.time;
        }
    }

    private void CheckWallRun()
    {
        if (config == null) return;

        if (states.isOnSlope && !states.isWallRunning)
        {
            if (Mathf.Abs(states.moveInput.x) < 0.1f) // Standardized to 0.1f for consistency
            {
                states.canWallRun = false;
                return;
            }
        }

        Vector2 boxCenter = (Vector2)transform.position + playerCollider.offset;
        float directionX = states.moveInput.x;
        float detectionDistance = config.wallCheckDistance;
        hadValidWallHitThisFrame = false; // Reset for this frame
        
        // Multi-Ray Detection (Top, Center, Bottom) to prevent gaps
        Vector2 topOrigin = boxCenter + Vector2.up * (playerCollider.size.y * 0.4f);
        Vector2 bottomOrigin = boxCenter + Vector2.down * (playerCollider.size.y * 0.4f);
        
        bool wallRight = false;
        bool wallLeft = false;
        Collider2D rightHit = null;
        Collider2D leftHit = null;

        bool IsObstructed(Vector2 point, Collider2D targetCollider)
        {
            BuildingDetector targetBuilding = targetCollider.GetComponentInParent<BuildingDetector>();
            if (targetBuilding == null) return false;

            Collider2D[] overlappers = Physics2D.OverlapPointAll(point);
            
            foreach (Collider2D col in overlappers)
            {
                if (col == targetCollider) continue;
                
                BuildingDetector blocker = col.GetComponentInParent<BuildingDetector>();
                
                if (blocker != null && blocker != targetBuilding)
                {
                    if (blocker.sortingOrder > targetBuilding.sortingOrder) return true;
                    
                    if (blocker.sortingOrder == targetBuilding.sortingOrder)
                    {
                        // Deterministic depth tie-breaker: Smaller Z is closer to the camera.
                        if (blocker.buildingZ < targetBuilding.buildingZ) return true;
                    }
                }
            }
            return false;
        }

        bool IsVerticalWallSurface(Vector2 normal)
        {
            if (!config.wallRunStrictNormals) return true;
            return Mathf.Abs(normal.x) >= config.wallNormalMinX;
        }

        Collider2D CheckDirection(Vector2 direction)
        {
            float skinOffset = 0.1f; 
            Vector2 offset = -direction * skinOffset;
            float adjustedDistance = detectionDistance + skinOffset;

            Vector2[] rays = { boxCenter + offset, topOrigin + offset, bottomOrigin + offset };
            
            Collider2D bestFound = null;
            int maxOrder = int.MinValue;
            float maxDepth = float.MaxValue;

            foreach (var rayOrigin in rays)
            {
                // Multi-ray check to prioritize front-most building
                RaycastHit2D[] hits = Physics2D.RaycastAll(rayOrigin, direction, adjustedDistance, config.wallLayer);
                foreach (var hit in hits)
                {
                    if (IsObstructed(hit.point, hit.collider)) continue;

                    // HOTFIX: Normal verification to avoid slopes
                    if (!IsVerticalWallSurface(hit.normal))
                    {
                        if (config.wallRunDebug)
                        {
                            float angle = Vector2.Angle(Vector2.up, hit.normal);
                            GameLogger.Log("WALLRUN", $"Reject slope hit: collider={hit.collider.name} normal={hit.normal} angle={angle:F1} method=Raycast");
                        }
                        continue;
                    }

                    BuildingDetector bd = hit.collider.GetComponentInParent<BuildingDetector>();
                    int order = bd != null ? bd.sortingOrder : -100;
                    float depth = bd != null ? bd.buildingZ : 100f; // Default large for far away

                    if (order > maxOrder || (order == maxOrder && depth < maxDepth))
                    {
                        maxOrder = order;
                        maxDepth = depth;
                        bestFound = hit.collider;
                    }
                }
            }
            return bestFound;
        }

        Collider2D CheckOverlapFallback(Vector2 direction)
        {
            // Air-catch gate: allow fallback in air if player is trying to move toward the wall
            if (!states.isGrounded && (!states.wallRunHeld || Mathf.Abs(states.moveInput.x) < 0.5f)) return null; 

            float overlapWidth = 0.65f; 
            Vector2 checkSize = new Vector2(overlapWidth, playerCollider.size.y * 1.1f);
            // Dynamic offset: reach further if moving fast
            float speedFactor = Mathf.Clamp01(Mathf.Abs(rb.linearVelocity.x) / 30f);
            Vector2 checkPos = boxCenter + direction * (playerCollider.size.x * 0.5f + overlapWidth * (0.4f + speedFactor * 0.2f));

            Collider2D[] cols = Physics2D.OverlapBoxAll(checkPos, checkSize, 0f, config.wallLayer);
            Collider2D bestCandidate = null;
            float minDistance = float.MaxValue;
            int maxSortingOrder = int.MinValue;
            float maxDepth = float.MaxValue;

            foreach (var col in cols)
            {
                Vector2 closestPoint = col.ClosestPoint(boxCenter);
                if (IsObstructed(closestPoint, col)) continue;

                // HOTFIX: Overlap doesn't give normals, so we cast a short ray to verify
                if (config.wallRunStrictNormals)
                {
                    Vector2 rayDir = (closestPoint - boxCenter).normalized;
                    float distToPoint = Vector2.Distance(boxCenter, closestPoint);
                    RaycastHit2D normalHit = Physics2D.Raycast(boxCenter, rayDir, distToPoint + 0.2f, config.wallLayer);
                    
                    if (normalHit.collider != null && !IsVerticalWallSurface(normalHit.normal))
                    {
                        if (config.wallRunDebug)
                        {
                            float angle = Vector2.Angle(Vector2.up, normalHit.normal);
                            GameLogger.Log("WALLRUN", $"Reject slope hit: collider={col.name} normal={normalHit.normal} angle={angle:F1} method=OverlapFallback");
                        }
                        continue;
                    }
                }

                float dist = Vector2.Distance(boxCenter, closestPoint);
                BuildingDetector bd = col.GetComponentInParent<BuildingDetector>();
                int sOrder = bd != null ? bd.sortingOrder : -100;
                float depth = bd != null ? bd.buildingZ : 100f;

                // Priority: Lane (Sorting Order) -> Depth (Smaller Z is Closer) -> Distance
                bool isBetter = false;
                if (sOrder > maxSortingOrder) isBetter = true;
                else if (sOrder == maxSortingOrder)
                {
                    if (depth < maxDepth - 0.0000001f) isBetter = true; 
                    else if (Mathf.Abs(depth - maxDepth) < 0.0000001f)
                    {
                        float flickerEpsilon = (currentWallCollider != null && col == currentWallCollider) ? -0.1f : 0.01f;
                        if (dist < minDistance + flickerEpsilon) isBetter = true;
                    }
                }

                if (isBetter)
                {
                    minDistance = dist;
                    maxSortingOrder = sOrder;
                    maxDepth = depth;
                    bestCandidate = col;
                }
            }
            
            if (bestCandidate != null && !states.isGrounded)
            {
                // Successful air-to-wall catch
            }
            
            return bestCandidate;
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
            hadValidWallHitThisFrame = true;
            currentWallCollider = rightHit;
            states.canWallRun = true;
            states.wallDirection = 1;

            // Side detection for flip lock (LWall/RWall tags)
            int side = TryGetWallSide(rightHit);
            if (side != 0) states.wallDirection = side;
            
            if (Mathf.Sign(directionX) != Mathf.Sign(states.wallDirection))
            {
                states.canWallRun = false;
                hadValidWallHitThisFrame = false; // Contradictory input invalidates hit
                ClearCurrentWall();
                return;
            }

            wallDetected = true;
            wasInGrace = false;
            
            var wallTrigger = rightHit.GetComponentInParent<BuildingWallsTrigger>();
            if (states.wallRunHeld)
            {
                SetCurrentWall(wallTrigger);
            }
            else
            {
                ClearCurrentWall();
            }

            if (!previousCanWallRun || currentWallCollider != previousWallCollider) 
            {
                states.RecordWallContact();
                
                // Record initial contact state
                lastWallHitTime = Time.time;
                lastWallCollider = rightHit;
                lastWallDirection = states.wallDirection;
            }
            DetectWallHeight(rightHit, 1);
        }
        else if (wallLeft && directionX < -0.1f)
        {
            hadValidWallHitThisFrame = true;
            currentWallCollider = leftHit;
            states.canWallRun = true;
            states.wallDirection = -1;

            // Side detection for flip lock (LWall/RWall tags)
            int side = TryGetWallSide(leftHit);
            if (side != 0) states.wallDirection = side;

            if (Mathf.Sign(directionX) != Mathf.Sign(states.wallDirection))
            {
                states.canWallRun = false;
                hadValidWallHitThisFrame = false;
                ClearCurrentWall();
                return;
            }

            wallDetected = true;
            wasInGrace = false;
            
            var wallTrigger = leftHit.GetComponentInParent<BuildingWallsTrigger>();
            if (states.wallRunHeld)
            {
                SetCurrentWall(wallTrigger);
            }
            else
            {
                ClearCurrentWall();
            }

            if (!previousCanWallRun || currentWallCollider != previousWallCollider)
            {
                states.RecordWallContact();
                
                lastWallHitTime = Time.time;
                lastWallCollider = leftHit;
                lastWallDirection = states.wallDirection;
            }
            DetectWallHeight(leftHit, -1);
        }
        else
        {
            // Grace Period: Only maintains active run, never initiates new ones
            states.canWallRun = false;

            if (previousIsWallRunning)
            {
                if (wallContactLostTime == 0f) wallContactLostTime = Time.time;

                bool recentlyGrounded = (Time.time - lastGroundedTime) < 0.1f;
                float effectiveGrace = recentlyGrounded ? 0.02f : WALL_CONTACT_GRACE_PERIOD;

                if (Time.time - wallContactLostTime <= effectiveGrace)
                {
                    // Proximity check: Must remain physically close during grace
                    Vector2 sideDir = Vector2.right * lastWallDirection;
                    float checkDist = detectionDistance * 0.8f; 
                    bool isStillClose = Physics2D.OverlapBox(boxCenter + sideDir * (playerCollider.size.x * 0.5f), new Vector2(checkDist, playerCollider.size.y), 0f, config.wallLayer);

                    if (isStillClose)
                    {
                        states.canWallRun = true;
                        states.wallDirection = lastWallDirection;
                        wasInGrace = true;
                    }
                    else
                    {
                        states.canWallRun = false;
                        currentWallCollider = null;
                        ClearCurrentWall();
                        wasInGrace = false;
                    }
                }
                else
                {
                    currentWallCollider = null;
                    ClearCurrentWall();
                    wasInGrace = false;
                }
            }
            else
            {
                currentWallCollider = null;
                ClearCurrentWall();
                wasInGrace = false;
            }
        }
        
        if (hadValidWallHitThisFrame)
        {
            wallContactLostTime = 0f;
            lastWallHitTime = Time.time;
            lastWallCollider = currentWallCollider;
            lastWallDirection = states.wallDirection;
        }

        previousWallCollider = currentWallCollider;
    }

    private void CheckWallSlide()
    {
        // Contact count is maintained by collision/trigger callbacks — no polling needed.
        // Reset the count defensively when grounded so it can't get stuck.
        if (states.isGrounded) midWallContactCount = 0;
        states.canWallSlide = midWallContactCount > 0 && !states.isGrounded && !states.isWallRunning;
    }

    private int midWallContactCount = 0;

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (!IsMidWallObject(col.gameObject)) return;
        midWallContactCount++;
        UpdateMidWallDirectionFromNormal(col.GetContact(0).normal);
    }

    private void OnCollisionExit2D(Collision2D col)
    {
        if (!IsMidWallObject(col.gameObject)) return;
        midWallContactCount = Mathf.Max(0, midWallContactCount - 1);
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (!IsMidWallObject(col.gameObject)) return;
        midWallContactCount++;
        UpdateMidWallDirectionFromPosition(col.transform.position);
    }

    private void OnTriggerExit2D(Collider2D col)
    {
        if (!IsMidWallObject(col.gameObject)) return;
        midWallContactCount = Mathf.Max(0, midWallContactCount - 1);
    }

    private bool IsMidWallObject(GameObject go)
    {
        return config != null && ((1 << go.layer) & config.midWallLayer.value) != 0;
    }

    private void UpdateMidWallDirectionFromNormal(Vector2 normal)
    {
        if (Mathf.Abs(normal.x) > 0.5f)
            states.wallDirection = normal.x > 0f ? -1 : 1;
    }

    private void UpdateMidWallDirectionFromPosition(Vector3 wallPosition)
    {
        states.wallDirection = wallPosition.x > transform.position.x ? 1 : -1;
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

        // FALLBACK OVERLAP BOX (Visualization)
        if (config.wallRunDebug)
        {
            float overlapWidth = 0.5f;
            Vector2 checkSize = new Vector2(overlapWidth, playerCollider.size.y * 1.0f);
            
            Gizmos.color = new Color(1, 0.5f, 0, 0.3f); // Orange Transparent
            // Right Fallback
            Vector2 rightPos = boxCenter + Vector2.right * (playerCollider.size.x * 0.5f + overlapWidth * 0.4f);
            Gizmos.DrawWireCube(rightPos, checkSize);
            
            // Left Fallback
            Vector2 leftPos = boxCenter + Vector2.left * (playerCollider.size.x * 0.5f + overlapWidth * 0.4f);
            Gizmos.DrawWireCube(leftPos, checkSize);

            if (currentWallCollider != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(currentWallCollider.bounds.center, 0.2f);
                Gizmos.DrawLine(boxCenter, currentWallCollider.bounds.center);
            }
        }

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
            currentWallTrigger.SetSolid(false);
        }

        currentWallTrigger = wall;
        
        if (currentWallTrigger != null)
        {
            currentWallTrigger.SetSolid(true);
        }
    }

    private void ClearCurrentWall()
    {
        if (currentWallTrigger != null)
        {
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

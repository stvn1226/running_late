#define ENABLE_VERBOSE_LOGGING

using MoreMountains.Feedbacks;
using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private static readonly int XVelocity = Animator.StringToHash("xVelocity");
    private static readonly int YVelocity = Animator.StringToHash("yVelocity");

    [Header("References")]
    public PlayerConfig config;
    
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerStates states;
    [SerializeField] private BoxCollider2D kickBox;
    [SerializeField] private PlayerWallClimb wallClimb;

    [SerializeField] private SpriteRenderer playerSprite;
    
    [Header("Slide & Roll Drag Multiplier")]
    [Tooltip("Multiplier applied to config.slideDrag for slide and roll deceleration. Higher = faster stop. Current: 3.5x")]
    [Range(1f, 10f)]
    [SerializeField] private float slideDragMultiplier = 3.5f;
    
    [Header("Wall Detection")]
    private bool isNearBuildingWall = false;
    private float buildingWallProximityTimer = 0f;
    private const float BUILDING_WALL_PROXIMITY_TIME = 0.5f;
    
    private float lastSignificantVelocityX = 0f;
    private float flipCooldownTimer = 0f;
    private float wallThresholdTimer = 0f;
    private const float FLIP_COOLDOWN = 0.1f;
    private const float VELOCITY_THRESHOLD = 1.0f;
    private const float WALL_THRESHOLD_DECAY_TIME = 0.3f;

    // Enhanced logging for sprite flipping
    private bool previousSpriteFlipX;
    private Vector2 previousVelocity;
    private float lastFlipAnalysisLog = 0f;
    private const float FLIP_ANALYSIS_LOG_INTERVAL = 2f;

    private float lastWallRunVelocity = 0f;
    public MMF_Player landingFeedbacks;

    private float maxRunSpeed;
    private Coroutine activeSpeedBoostCoroutine;
    private float slopeSnapDisableTimer = 0f;

    private float footstepTimer = 0f;
    private const float FOOTSTEP_INTERVAL = 0.3f; // Adjust this value - lower = faster footsteps

    private void Start()
    {
        rb.linearDamping = 0f;
        rb.linearDamping = 0f;
        previousSpriteFlipX = playerSprite.flipX;

        maxRunSpeed = config.maxRunSpeed;

        if (kickBox != null)
        {
            kickBox.enabled = false;
        }

        if (wallClimb == null)
        {
            wallClimb = GetComponent<PlayerWallClimb>();
        }
        
        if (states != null)
        {
            states.OnLanded += HandleLanding;
        }
        
#if ENABLE_VERBOSE_LOGGING
        GameLogger.LogMovement("PlayerMovement Started");
#endif
    }

    private void OnDestroy()
    {
        if (states != null)
        {
            states.OnLanded -= HandleLanding;
        }
    }

    public void InvokeExtraSpeed(float extraSpeed, float duration)
    {
        if (activeSpeedBoostCoroutine != null)
        {
            StopCoroutine(activeSpeedBoostCoroutine);
            maxRunSpeed = config.maxRunSpeed;
            GameLogger.Log("POWERUP", "Extra Speed effect refreshed - resetting timer");
        }
        activeSpeedBoostCoroutine = StartCoroutine(ExtraSpeedCoroutine(extraSpeed, duration));
    }

    private IEnumerator ExtraSpeedCoroutine(float extraSpeed, float duration)
    {
        float originalMaxSpeed = config.maxRunSpeed;
        maxRunSpeed = originalMaxSpeed + extraSpeed;
        
        GameLogger.Log("POWERUP", $"Extra Speed activated! Max speed: {originalMaxSpeed} -> {maxRunSpeed} for {duration} seconds");
        
        yield return new WaitForSeconds(duration);
        
        maxRunSpeed = originalMaxSpeed;
        activeSpeedBoostCoroutine = null;
        
        GameLogger.Log("POWERUP", $"Extra Speed expired! Max speed restored to {maxRunSpeed}");
    }

    public void HandleKickBox()
    {
        if (kickBox != null)
        {
            bool shouldKickBoxBeEnabled = states.isKicking;
        
            if (kickBox.enabled != shouldKickBoxBeEnabled)
            {
                kickBox.enabled = shouldKickBoxBeEnabled;
#if ENABLE_VERBOSE_LOGGING
                GameLogger.LogVerbose("KICK", shouldKickBoxBeEnabled ? "KickBox ENABLED" : "KickBox DISABLED");
#endif
            }
        }
    }

    private void FixedUpdate()
    {
        if (!states.isGrounded)
        {
            states.landingHorizontalVelocity = rb.linearVelocity.x;
            states.landingVerticalVelocity = rb.linearVelocity.y;
        }
        
        ApplyDrag();
        ClampFallSpeed();
        ClampHorizontalSpeed();

        if (states.isStaggered) return;
      
        StickToSlope();
        HandleSpriteFlipping();
        HandleMovement();
        
        AnalyzeFlipBehavior();

        previousVelocity = rb.linearVelocity;
        previousSpriteFlipX = playerSprite.flipX;
    }


    public void SetNearBuildingWall(bool isNear)
    {
        isNearBuildingWall = isNear;
        if (isNear)
        {
            buildingWallProximityTimer = BUILDING_WALL_PROXIMITY_TIME;
#if ENABLE_VERBOSE_LOGGING
            GameLogger.LogVerbose("MOVEMENT", "BUILDING WALL PROXIMITY");
#endif
        }
    }

    private void AnalyzeFlipBehavior()
    {
        if (Time.time - lastFlipAnalysisLog > FLIP_ANALYSIS_LOG_INTERVAL)
        {
#if ENABLE_VERBOSE_LOGGING
            Vector2 currentVel = rb.linearVelocity;
            GameLogger.LogVerbose("MOVEMENT", $"FLIP: Sprite={playerSprite.flipX}, WallRun={states.isWallRunning}, Ground={states.isGrounded}");
#endif
            lastFlipAnalysisLog = Time.time;
        }
    }

    private void HandleSpriteFlipping()
    {
        flipCooldownTimer -= Time.fixedDeltaTime;
        wallThresholdTimer -= Time.fixedDeltaTime;
        buildingWallProximityTimer -= Time.fixedDeltaTime;
        
        float currentVelX = rb.linearVelocity.x;
        float absVelX = Mathf.Abs(currentVelX);
        
        if (absVelX > VELOCITY_THRESHOLD)
        {
            lastSignificantVelocityX = currentVelX;
        }

        if (states.isWallRunning)
        {
            lastWallRunVelocity = currentVelX;
            
            if (states.wallDirection == 1) // LWall -> Wall on player's RIGHT
            {
                if (playerSprite.flipX) 
                {
                    playerSprite.flipX = false;
                }
                if (states.moveInput.x < 0f)
                {
                    states.moveInput.x = 0f; 
                }
            }
            else if (states.wallDirection == -1) // RWall -> Wall on player's LEFT
            {
                if (!playerSprite.flipX)
                {
                    playerSprite.flipX = true;
                }
                if (states.moveInput.x > 0f)
                {
                    states.moveInput.x = 0f; 
                }
            }
            
            return; // Skip normal flip logic while wallrunning
        }
        
        // Enhanced flip logic with wall run awareness AND building proximity
        bool isNearWall = states.canWallRun || states.isWallRunning;
        bool isNearBuildingWallRecently = buildingWallProximityTimer > 0f;
        
        // Reset wall threshold timer when near wall OR building
        if (isNearWall || isNearBuildingWall)
        {
            wallThresholdTimer = WALL_THRESHOLD_DECAY_TIME;
        }
        
        // Use elevated threshold if recently near wall OR building
        bool recentlyNearWall = wallThresholdTimer > 0f;
        bool shouldUseElevatedThreshold = isNearWall || recentlyNearWall || isNearBuildingWallRecently;
        
        float flipThreshold = shouldUseElevatedThreshold ? VELOCITY_THRESHOLD * 4f : VELOCITY_THRESHOLD;
        
        if (flipCooldownTimer <= 0f && absVelX > flipThreshold)
        {
            float velocityChange = Mathf.Abs(currentVelX - previousVelocity.x);
            if (shouldUseElevatedThreshold && velocityChange > 5f)
            {
#if ENABLE_VERBOSE_LOGGING
                GameLogger.LogVerbose("MOVEMENT", "FLIP PREVENTED: Drastic velocity change near wall");
#endif
                return;
            }

            bool isFlipContradictory = false;
            if (wallThresholdTimer > WALL_THRESHOLD_DECAY_TIME * 0.8f) // Recién salió de wall run
            {
                if (lastWallRunVelocity < 0 && currentVelX > flipThreshold && Mathf.Abs(lastWallRunVelocity) > absVelX)
                    isFlipContradictory = true;
                else if (lastWallRunVelocity > 0 && currentVelX < -flipThreshold && Mathf.Abs(lastWallRunVelocity) > absVelX)
                    isFlipContradictory = true;
            }

            if (isFlipContradictory)
            {
#if ENABLE_VERBOSE_LOGGING
                GameLogger.LogVerbose("MOVEMENT", "FLIP PREVENTED: Would contradict recent wall run");
#endif
                return;
            }
            
            bool shouldFlipLeft = currentVelX < -flipThreshold;
            bool shouldFlipRight = currentVelX > flipThreshold;
            
            if (shouldFlipLeft && !playerSprite.flipX)
            {
#if ENABLE_VERBOSE_LOGGING
                GameLogger.LogMovement($"SPRITE FLIP LEFT: VelX={currentVelX:F2}");
#endif
                playerSprite.flipX = true;
                kickBox.offset = new Vector2(-Mathf.Abs(kickBox.offset.x), kickBox.offset.y);
                flipCooldownTimer = FLIP_COOLDOWN;
            }
            else if (shouldFlipRight && playerSprite.flipX)
            {
#if ENABLE_VERBOSE_LOGGING
                GameLogger.LogMovement($"SPRITE FLIP RIGHT: VelX={currentVelX:F2}");
#endif
                playerSprite.flipX = false;
                kickBox.offset = new Vector2(Mathf.Abs(kickBox.offset.x), kickBox.offset.y);
                flipCooldownTimer = FLIP_COOLDOWN;
            }
        }
        else if (absVelX < 0.1f && states.moveInput.x != 0 && flipCooldownTimer <= 0f)
        {
            bool shouldFlipLeft = states.moveInput.x < 0;
            bool shouldFlipRight = states.moveInput.x > 0;
            
            if (shouldFlipLeft && !playerSprite.flipX)
            {
#if ENABLE_VERBOSE_LOGGING
                GameLogger.LogMovement("SPRITE FLIP LEFT (Input)");
#endif
                playerSprite.flipX = true;
                kickBox.offset = new Vector2(-Mathf.Abs(kickBox.offset.x), kickBox.offset.y);
                flipCooldownTimer = FLIP_COOLDOWN * 0.3f;
            }
            else if (shouldFlipRight && playerSprite.flipX)
            {
#if ENABLE_VERBOSE_LOGGING
                GameLogger.LogMovement("SPRITE FLIP RIGHT (Input)");
#endif
                playerSprite.flipX = false;
                kickBox.offset = new Vector2(Mathf.Abs(kickBox.offset.x), kickBox.offset.y);
                flipCooldownTimer = FLIP_COOLDOWN * 0.3f;
            }
        }
        
#if ENABLE_VERBOSE_LOGGING
        if (shouldUseElevatedThreshold && absVelX > VELOCITY_THRESHOLD && absVelX <= flipThreshold)
        {
            GameLogger.LogVerbose("MOVEMENT", "FLIP PREVENTED: Near wall/building");
        }
#endif
    }

    public void StaggerPlayer(Vector2 knockbackDirection)
    {
        if (config == null || states == null)
        {
            GameLogger.LogError("[STAGGER] Config or States is null!");
            return;
        }

        if (states.isInvincible) return;

        GameLogger.Log($"[STAGGER] Starting stagger - duration: {config.staggerDuration}s, knockback: {knockbackDirection}");
    
        states.StartStagger(config.staggerDuration);

        AudioManager.Instance?.Play(AudioManager.SoundType.Stagger);
        GamepadVibrationManager.Instance?.TriggerHitVibration();

        Vector2 knockbackForce = knockbackDirection.normalized * config.staggerKnockbackForce;
        
        // Authoritatively override velocity to stop forward momentum immediately
        // We use a minimum vertical impulse to ensure the player "bounces" off the obstacle
        float finalKnockbackY = Mathf.Max(knockbackForce.y, 10f);
        rb.linearVelocity = new Vector2(knockbackForce.x, finalKnockbackY);
    
        GameLogger.Log($"[STAGGER] Velocity authoritatively set to: {rb.linearVelocity} (Force: {config.staggerKnockbackForce}, Dir: {knockbackDirection})");
    }


    private void HandleMovement()
    {
        if (config == null) return;

        float forceMultiplier = 1f;

        if (states.isStaggered)
        {
            forceMultiplier *= config.staggerMovementReduction;
        }

        if (states.isWallRunning)
            HandleWallRunMovement(forceMultiplier);
        else if (states.isSlidingOnCable)
            HandleCableSlideMovement();
        else if (states.isRolling)
            HandleRollMovement(forceMultiplier);
        else if (states.isSliding)
            HandleSlideMovement(forceMultiplier);
        else if (states.isCrouching)
            HandleCrouchMovement();
        else if (states.isDiving)
            HandleDiveMovement(forceMultiplier);
        else
            HandleNormalMovement(forceMultiplier);
    }
    
    private void HandleCrouchMovement()
    {
        float currentSpeed = Mathf.Abs(rb.linearVelocity.x);
        
        if (currentSpeed > 0.5f)
        {
            Vector2 velocity = rb.linearVelocity;
            Vector2 dragForces = -velocity * (config.slideDrag * slideDragMultiplier * 0.5f * Time.fixedDeltaTime);
            rb.AddForce(new Vector2(dragForces.x, 0), ForceMode2D.Force);
        }
    }

    private void HandleNormalMovement(float forceMultiplier)
    {
        if (states.isCrouching || states.isSliding || states.isDiving || states.isRolling) return;

        float horizontalInput = states.moveInput.x;

        if (Mathf.Abs(horizontalInput) > 0.1f)
        {
            float targetSpeed = horizontalInput * maxRunSpeed;
            float speedDifference = targetSpeed - rb.linearVelocity.x;
        
            bool isAccelerating = Mathf.Approximately(Mathf.Sign(speedDifference), Mathf.Sign(horizontalInput));
            bool isBelowMaxSpeed = Mathf.Abs(rb.linearVelocity.x) < maxRunSpeed;
        
            // Skip run forces in the very first frames of a jump while still grounded to avoid slope interference
            bool isJumpingWhileGrounded = states.isJumping && states.isGrounded;
            bool canApplyRunForce = (isAccelerating || isBelowMaxSpeed) && !isJumpingWhileGrounded;
            
            if (canApplyRunForce)
            {
                float baseForce = states.isGrounded ? config.runForce : config.runForce * config.airControlMultiplier;
                
                if (states.isKicking)
                {
                    baseForce *= config.kickForceReduction;
                }

                baseForce *= forceMultiplier;

                float appliedForce = Mathf.Clamp(speedDifference * baseForce, -baseForce, baseForce);
                
                // MVR: Simplified branching. We use slope-tangent movement if we ARE on a slope 
                // (including the persistence buffer) AND we aren't performing the initial jump frames.
                bool useSlopeMovement = states.isOnSlope && !states.isSlopeTooSteep && !isJumpingWhileGrounded;

                if (useSlopeMovement)
                {
                    Vector2 moveDirection = states.slopeTangent * horizontalInput;
                    
                    // Standard ground movement on slope
                    if (states.isGrounded && !states.isJumping)
                    {
                        float slopeCurrentSpeed = Vector2.Dot(rb.linearVelocity, moveDirection.normalized);
                        float slopeTargetSpeed = moveDirection.magnitude * maxRunSpeed;
                        
                        float slopeSpeedDiff = slopeTargetSpeed - slopeCurrentSpeed;
                        float slopeAccel = config.slopeMoveAccel * forceMultiplier;
                        float slopeForceAmount = Mathf.Clamp(slopeSpeedDiff * slopeAccel, -slopeAccel, slopeAccel);
                        
                        rb.AddForce(moveDirection.normalized * (slopeForceAmount * Time.fixedDeltaTime), ForceMode2D.Force);
                        
                        // Gravity compensation for uphill/downhill
                        bool isGoingUphill = moveDirection.y > 0f;
                        if (isGoingUphill)
                        {
                            float slopeAngleRad = states.slopeAngle * Mathf.Deg2Rad;
                            float gravityCompensation = Mathf.Abs(Physics2D.gravity.y) * Mathf.Sin(slopeAngleRad);
                            rb.AddForce(moveDirection.normalized * (gravityCompensation * Time.fixedDeltaTime), ForceMode2D.Force);
                        }
                        else 
                        {
                            float downhillBoost = config.runForce * 1.2f * forceMultiplier;
                            rb.AddForce(moveDirection.normalized * (downhillBoost * Time.fixedDeltaTime), ForceMode2D.Force);
                        }
                    }
                    else // Persistent Tangent Projection for air control near slopes or during jumps
                    {
                        Vector2 slopeForce = states.slopeTangent * (appliedForce * Time.fixedDeltaTime);
                        rb.AddForce(slopeForce, ForceMode2D.Force);
                    }
                }
                else
                {
                    rb.AddForce(Vector2.right * (appliedForce * Time.fixedDeltaTime), ForceMode2D.Force);
                }
            }
        }

        if (states.crouchHeld && !states.isGrounded)
        {
            rb.AddForce(Vector2.down * (config.runForce * Time.fixedDeltaTime), ForceMode2D.Force);
        }

        if (states.isGrounded && Mathf.Abs(horizontalInput) > 0.1f)
        {
            footstepTimer -= Time.fixedDeltaTime;
            if (footstepTimer <= 0f)
            {
                AudioManager.Instance?.Play(AudioManager.SoundType.Footsteps);
                footstepTimer = FOOTSTEP_INTERVAL;
            }
        }
        else
        {
            footstepTimer = 0f; // Reset when not moving or not grounded
        }
    }

    private void StickToSlope()
    {
        slopeSnapDisableTimer -= Time.fixedDeltaTime;
    
        if (!states.isOnSlope || states.isSlopeTooSteep)
            return;

        if (states.isJumping || states.isRolling || states.isDiving || states.isKicking)
            return;

        if (slopeSnapDisableTimer > 0f)
            return;

        // Removed the hard-limit on upward velocity to prevent high-speed lift-off
        // Stickiness is now purely determined by perpendicular velocity relative to slope
        // bool isMovingUphillFast = rb.linearVelocity.y > 0.2f; // Removed

        Vector2 perpendicular = states.slopeNormal;
        float perpendicularVel = Vector2.Dot(rb.linearVelocity, perpendicular);
    
        if (perpendicularVel > 0.01f)
        {
            Vector2 correctionForce = -perpendicular * (perpendicularVel * config.slopeStickStrength / Time.fixedDeltaTime);
            rb.AddForce(correctionForce, ForceMode2D.Force);
        
#if ENABLE_VERBOSE_LOGGING
            GameLogger.LogVerbose("SLOPE", "STICK applied");
#endif
        }

        // NEW: apply tangential gravity when descending and having input
        float slopeAngleRad = states.slopeAngle * Mathf.Deg2Rad;
        float tangentialGravity = Mathf.Abs(Physics2D.gravity.y) 
                                  * Mathf.Sin(slopeAngleRad) 
                                  * config.slopeGravityInfluence;
        
        float horizontalInput = states.moveInput.x;
        Vector2 downhillDirAlongSlope = states.slopeTangent * Mathf.Sign(rb.linearVelocity.x);
        
        // Only boost if actively moving downhill with input, not when standing still
        if (downhillDirAlongSlope.y < 0f && rb.linearVelocity.y < 0f && Mathf.Abs(horizontalInput) > 0.1f)
        {
            // Boosted tangential gravity to overcome drag system
            rb.AddForce(downhillDirAlongSlope * (tangentialGravity * 50f * Time.fixedDeltaTime), ForceMode2D.Force);
        }
    }

    public void DisableSlopeSnapForJump()
    {
        slopeSnapDisableTimer = config.slopeSnapDisableTime;
    }

    private void HandleSlideMovement(float forceMultiplier)
    {
        if (states.isOnSlope && !states.isSlopeTooSteep)
        {
            Vector2 slopeRight = new Vector2(states.slopeNormal.y, -states.slopeNormal.x);
            float currentDir = Mathf.Sign(rb.linearVelocity.x);
            if (currentDir == 0f) currentDir = 1f;
            
            Vector2 slopeDir = slopeRight * currentDir;
            
            if (slopeDir.y < 0f && states.landingGraceTimer <= 0f)
            {
                float currentSpeed = Mathf.Abs(rb.linearVelocity.x);
                float momentumFactor = Mathf.Clamp01(currentSpeed / (maxRunSpeed * 0.8f));
                float slopeAngleFactor = Mathf.Clamp01(states.slopeAngle / config.maxWalkableSlope);
                float angleBonus = 1f + (slopeAngleFactor * 1.5f); // Increased bonus
                float slopeBoost = config.runForce * 2.5f * momentumFactor * angleBonus; // Using config.runForce as base for scaling
                
                rb.AddForce(slopeDir * (slopeBoost * Time.fixedDeltaTime), ForceMode2D.Force);
                
#if ENABLE_VERBOSE_LOGGING
                GameLogger.LogVerbose("SLOPE", "SLIDE ON SLOPE applied");
#endif
            }
        }
    }

    private void HandleRollMovement(float forceMultiplier)
    {
        if (states.isOnSlope && !states.isSlopeTooSteep)
        {
            Vector2 slopeRight = new Vector2(states.slopeNormal.y, -states.slopeNormal.x);
            float currentDir = Mathf.Sign(rb.linearVelocity.x);
            if (currentDir == 0f) currentDir = 1f;
            
            Vector2 slopeDir = slopeRight * currentDir;
            
            if (slopeDir.y < 0f)
            {
                float currentSpeed = Mathf.Abs(rb.linearVelocity.x);
                float momentumFactor = Mathf.Clamp01(currentSpeed / (maxRunSpeed * 0.8f));
                float slopeBoost = 250f * momentumFactor;
                
                rb.AddForce(slopeDir * (slopeBoost * Time.fixedDeltaTime), ForceMode2D.Force);
                
#if ENABLE_VERBOSE_LOGGING
                GameLogger.LogVerbose("SLOPE", "ROLL ON SLOPE applied");
#endif
            }
        }
    }

    private void HandleDiveMovement(float forceMultiplier)
    {
        // Diving maintains momentum with no horizontal control
        // Player is committed to the dive direction
    }

    private void HandleWallRunMovement(float forceMultiplier)
    {
        float climbBoost = wallClimb != null ? wallClimb.GetWallRunForceMultiplier() : 1f;
        
        if (states.isCornerClimbing)
            climbBoost *= 0.3f;
        
        float totalMultiplier = forceMultiplier * climbBoost;

        rb.AddForce(Vector2.up * (config.wallRunForce * totalMultiplier * Time.fixedDeltaTime), ForceMode2D.Force);

        float horizontalInput = states.moveInput.x;
        if (Mathf.Abs(horizontalInput) > 0.1f)
        {
            float horizontalForce = horizontalInput * config.runForce * 0.5f * forceMultiplier;
            rb.AddForce(Vector2.right * (horizontalForce * Time.fixedDeltaTime), ForceMode2D.Force);
        }
    }

    private void ApplyDrag()
    {
        // Don't apply drag while jumping to preserve the impulse
        if (states.isJumping) return;

        Vector2 velocity = rb.linearVelocity;
        float horizontalInput = states.moveInput.x;
        
        if (Mathf.Abs(velocity.x) < 0.15f && Mathf.Abs(horizontalInput) < 0.1f && states.isGrounded)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }
        
        if (states.justLanded && states.landingGraceTimer > 0f)
        {
            GameLogger.LogVerbose("MOVEMENT", $"Landing grace active - maintaining velocity: {rb.linearVelocity.x:F2}, Timer: {states.landingGraceTimer:F2}");
            return;
        }
    
        if (!states.isGrounded)
            return;
        
        if (states.isRolling)
        {
            float currentDragMultiplier = slideDragMultiplier;
            if (states.isOnSlope)
            {
                // Reduce roll friction when going downhill
                Vector2 slopeDirection = new Vector2(states.slopeNormal.y, -states.slopeNormal.x) * Mathf.Sign(rb.linearVelocity.x);
                if (slopeDirection.y < 0f) currentDragMultiplier *= 0.5f;
            }

            Vector2 dragForces = -velocity * (config.slideDrag * currentDragMultiplier * Time.fixedDeltaTime);
            rb.AddForce(new Vector2(dragForces.x, 0), ForceMode2D.Force);
            return;
        }

        if (states.isKicking && states.isGrounded)
        {
            Vector2 dragForces = -velocity * (config.kickGroundDrag * Time.fixedDeltaTime);
            rb.AddForce(new Vector2(dragForces.x, 0), ForceMode2D.Force);
            return;
        }
        
        if (states.isSliding)
        {
            if (states.isOnSlope && !states.isSlopeTooSteep)
            {
                Vector2 slopeDirection = new Vector2(states.slopeNormal.y, -states.slopeNormal.x);
                float velocityAlongSlope = Vector2.Dot(velocity, slopeDirection);
                
                // Reduce slide friction when going downhill to maintain or gain momentum
                float currentDragMultiplier = slideDragMultiplier;
                bool isMovingDownhill = (slopeDirection.y * Mathf.Sign(velocityAlongSlope)) < 0f;
                
                if (isMovingDownhill)
                {
                    currentDragMultiplier *= 0.3f; // 70% reduction in drag
                }

                Vector2 dragForces = -slopeDirection * (velocityAlongSlope * (config.slideDrag * currentDragMultiplier * Time.fixedDeltaTime));
                rb.AddForce(dragForces, ForceMode2D.Force);
            }
            else
            {
                Vector2 dragForces = -velocity * (config.slideDrag * slideDragMultiplier * Time.fixedDeltaTime);
                rb.AddForce(new Vector2(dragForces.x, 0), ForceMode2D.Force);
            }
            return;
        }
        
        if (states.isOnSlope && !states.isSlopeTooSteep)
        {
            Vector2 slopeDirection = new Vector2(states.slopeNormal.y, -states.slopeNormal.x);
            float velocityAlongSlope = Vector2.Dot(velocity, slopeDirection);
    
            if (Mathf.Abs(horizontalInput) < 0.1f)
            {
                if (Mathf.Abs(velocityAlongSlope) > 0.1f)
                {
                    Vector2 stopForce = -slopeDirection * (velocityAlongSlope * (config.slopeStopDecel * Time.fixedDeltaTime));
                    rb.AddForce(stopForce, ForceMode2D.Force);
                }
                return;
            }
    
            bool inputOpposesMovement = (horizontalInput * velocityAlongSlope) < 0;
            
            if (inputOpposesMovement)
            {
                float slopeTargetDrag = config.groundDrag * 1.5f;
                Vector2 slopeDragForce = -slopeDirection * (velocityAlongSlope * (slopeTargetDrag * Time.fixedDeltaTime));
                rb.AddForce(slopeDragForce, ForceMode2D.Force);
            }
            
            return;
        }
        
        float targetDrag = 0f;
        
        if (Mathf.Abs(horizontalInput) < 0.1f)
        {
            targetDrag = config.groundDrag * 2.1f; 
        }
        else
        {
            bool inputOpposesMovement = (horizontalInput * velocity.x) < 0;
            targetDrag = inputOpposesMovement ? config.groundDrag * 1.5f : config.groundDrag * 0.1f;
        }
        
        Vector2 dragForce = -velocity * (targetDrag * Time.fixedDeltaTime);
        rb.AddForce(new Vector2(dragForce.x, 0), ForceMode2D.Force);
    }

    public void HandleLanding()
    {
        if (config == null) return;

        if (!states.isGrounded) //updates the velocities while in air ahead of landing
        {
            states.landingHorizontalVelocity = rb.linearVelocity.x * 1.5f;
            states.landingVerticalVelocity = rb.linearVelocity.y;
        }

        if (states.wasGrounded != states.isGrounded && states.isGrounded)
        {
            if (states.justLanded && states.landingGraceTimer > config.landingGracePeriod * 0.8f)
            {
                GameLogger.LogVerbose("MOVEMENT", "Ignoring duplicate landing detection");
                return;
            }

            landingFeedbacks.PlayFeedbacks();
            
            float downwardSpeed = Mathf.Abs(states.landingVerticalVelocity);
            bool isSlamLanding = downwardSpeed > Mathf.Abs(config.slamLandingThreshold);
            bool hasHorizontalMomentum = Mathf.Abs(states.landingHorizontalVelocity) > 0.1f;
            bool hasHorizontalInput = Mathf.Abs(states.moveInput.x) > 0f;
            bool isHoldingCrouchButton = states.crouchHeld;
            bool isLandingIntoSlide = isHoldingCrouchButton && hasHorizontalInput;
            
            GameLogger.LogMovement($"Landing detected! Vertical: {states.landingVerticalVelocity:F2}, Horizontal: {states.landingHorizontalVelocity:F2}, DownSpeed: {downwardSpeed:F2}, HasMomentum: {hasHorizontalMomentum}, Input: {states.moveInput.x:F2}, Crouch: {isHoldingCrouchButton}, Slam: {isSlamLanding}, IntoSlide: {isLandingIntoSlide}");
            
            if (isLandingIntoSlide)
            {
                float horizontalBoost = (config.momentumConversionRatio * downwardSpeed);
                float inputDirection = Mathf.Sign(states.landingHorizontalVelocity);
                
                float preservedMomentum = states.landingHorizontalVelocity;
                float slideBoost = horizontalBoost * inputDirection;
                float newHorizontalVelocity = preservedMomentum + slideBoost;
                
                newHorizontalVelocity = Mathf.Clamp(newHorizontalVelocity, -maxRunSpeed * 2.5f, maxRunSpeed * 2.5f);
                
                rb.linearVelocity = new Vector2(newHorizontalVelocity, 0f);
                states.landingHorizontalVelocity = newHorizontalVelocity;
                
                GameLogger.LogMovement($"SLIDE LANDING! Preserved: {preservedMomentum:F2}, Boost: {slideBoost:F2}, Final: {newHorizontalVelocity:F2}");
            }
            else if (isSlamLanding && hasHorizontalMomentum)
            {
                float horizontalBoost = (config.momentumConversionRatio * downwardSpeed);
                float movementDirection = Mathf.Sign(states.landingHorizontalVelocity);
                
                float preservedMomentum = states.landingHorizontalVelocity;
                float newHorizontalVelocity = preservedMomentum + (horizontalBoost * movementDirection);
                
                rb.linearVelocity = new Vector2(newHorizontalVelocity, 0f);
                states.landingHorizontalVelocity = newHorizontalVelocity;
                
                GameLogger.LogMovement($"SLAM LANDING! Preserved: {preservedMomentum:F2}, Boost: {horizontalBoost:F2}, Final: {newHorizontalVelocity:F2}");
            }
            else
            {
                rb.linearVelocity = new Vector2(states.landingHorizontalVelocity, 0f);
                GameLogger.LogMovement($"Regular landing: Preserving horizontal velocity {states.landingHorizontalVelocity:F2}");
            }
            
            states.justLanded = true;
            states.landingGraceTimer = isLandingIntoSlide ? config.landingGracePeriod * 1.5f : config.landingGracePeriod;
        }
    }

    private void ClampFallSpeed()
    {
        if (config == null) return;

        if (rb.linearVelocity.y < -config.maxFallSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -config.maxFallSpeed);
        }
    }

    /// <summary>
    /// Enforces the absolute hard cap on horizontal speed. Applied every physics frame after all
    /// movement forces. The player can exceed maxRunSpeed via momentum but never surpass maxHorizontalSpeed.
    /// </summary>
    private void ClampHorizontalSpeed()
    {
        if (config == null) return;

        float absVelX = Mathf.Abs(rb.linearVelocity.x);
        if (absVelX > config.maxHorizontalSpeed)
        {
            rb.linearVelocity = new Vector2(Mathf.Sign(rb.linearVelocity.x) * config.maxHorizontalSpeed, rb.linearVelocity.y);

#if ENABLE_VERBOSE_LOGGING
            GameLogger.LogVerbose("MOVEMENT", $"Hard speed cap applied: {absVelX:F2} -> {config.maxHorizontalSpeed}");
#endif
        }
    }

    private void HandleCableSlideMovement()
    {
        Vector2 closestPoint = FindClosestPointOnCable(states.currentCable);
        Vector2 directionToPoint = (closestPoint - rb.position).normalized;
        float slideForceMagnitude = config.cableSlideForce;
        Vector2 slideForce = directionToPoint * slideForceMagnitude;
        rb.AddForce(slideForce * Time.fixedDeltaTime, ForceMode2D.Impulse);
    }

    private Vector2 FindClosestPointOnCable(EdgeCollider2D cableCollider)
    {
        Vector2 playerPosition = rb.position;
        Vector2[] cablePoints = cableCollider.points;
        Vector2 closestPoint = cableCollider.transform.TransformPoint(cablePoints[cablePoints.Length - 1]);
        foreach (Vector2 point in cablePoints)
        {
            Vector2 worldPoint = cableCollider.transform.TransformPoint(point);
            if (worldPoint.x < playerPosition.x) continue;
            // closestPoint = new Vector2 (worldPoint.x, worldPoint.y + .5f);
            closestPoint = new Vector2 (worldPoint.x, worldPoint.y + 0.15f); // Reduced offset for stability
            break;

        }
        return closestPoint;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // Zero out horizontal velocity when pressing against a wall that is not being wall-run
        if (!states.isGrounded && !states.isWallRunning && !states.isWallSliding)
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (Mathf.Abs(contact.normal.x) > 0.9f) // Vertical wall
                {
                    // If near the top of the wall/building, allow gliding over it instead of stopping
                    if (states.isNearWallTop) continue;

                    // If moving into the wall, kill horizontal velocity to prevent "sliding through" or bouncing weirdly
                    float relativeVelX = rb.linearVelocity.x;
                    if (Mathf.Sign(relativeVelX) != Mathf.Sign(contact.normal.x))
                    {
                        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                    }
                }
            }
        }
    }
}

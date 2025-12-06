using MoreMountains.Feedbacks;
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

    private void Start()
    {
        rb.linearDamping = 0f;
        rb.linearDamping = 0f;
        previousSpriteFlipX = playerSprite.flipX;
        
        if (kickBox != null)
        {
            kickBox.enabled = false;
        }

        if (wallClimb == null)
        {
            wallClimb = GetComponent<PlayerWallClimb>();
        }
        
        GameLogger.LogMovement($"Started - Initial sprite flip: {previousSpriteFlipX}");
    }

    public void HandleKickBox()
    {
        if (kickBox != null)
        {
            bool shouldKickBoxBeEnabled = states.isKicking;
        
            if (kickBox.enabled != shouldKickBoxBeEnabled)
            {
                kickBox.enabled = shouldKickBoxBeEnabled;
                GameLogger.LogVerbose("KICK", $"KickBox {(shouldKickBoxBeEnabled ? "ENABLED" : "DISABLED")} - isKicking: {states.isKicking}");
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

        if (states.isStaggered) return;
      
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
            GameLogger.LogVerbose("MOVEMENT", $"BUILDING WALL PROXIMITY: {isNear}");
        }
    }

    private void AnalyzeFlipBehavior()
    {
        if (Time.time - lastFlipAnalysisLog > FLIP_ANALYSIS_LOG_INTERVAL)
        {
            Vector2 currentVel = rb.linearVelocity;
            
            // Log current state for analysis
            GameLogger.LogVerbose("MOVEMENT", $"FLIP ANALYSIS: " +
                $"SpriteFlip={playerSprite.flipX}, " +
                $"Vel=({currentVel.x:F2},{currentVel.y:F2}), " +
                $"WallRun={states.isWallRunning}, " +
                $"CanWallRun={states.canWallRun}, " +
                $"WallDir={states.wallDirection}, " +
                $"Input.x={states.moveInput.x:F2}, " +
                $"Grounded={states.isGrounded}");
                
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
                GameLogger.LogVerbose("MOVEMENT", $"FLIP PREVENTED: Drastic velocity change {velocityChange:F1} near wall/building");
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
                GameLogger.LogVerbose("MOVEMENT", $"FLIP PREVENTED: Would contradict recent wall run. LastWallVel={lastWallRunVelocity:F2}, CurrentVel={currentVelX:F2}");
                return;
            }
            
            bool shouldFlipLeft = currentVelX < -flipThreshold;
            bool shouldFlipRight = currentVelX > flipThreshold;
            
            if (shouldFlipLeft && !playerSprite.flipX)
            {
                GameLogger.LogMovement($"SPRITE FLIP LEFT: VelX={currentVelX:F2}, Threshold={flipThreshold:F1}, WallRun={states.isWallRunning}, CanWallRun={states.canWallRun}, BuildingNear={isNearBuildingWallRecently}");
                playerSprite.flipX = true;
                kickBox.offset = new Vector2(-Mathf.Abs(kickBox.offset.x), kickBox.offset.y);
                flipCooldownTimer = FLIP_COOLDOWN;
            }
            else if (shouldFlipRight && playerSprite.flipX)
            {
                GameLogger.LogMovement($"SPRITE FLIP RIGHT: VelX={currentVelX:F2}, Threshold={flipThreshold:F1}, WallRun={states.isWallRunning}, CanWallRun={states.canWallRun}, BuildingNear={isNearBuildingWallRecently}");
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
                GameLogger.LogMovement($"SPRITE FLIP LEFT (Input): Input={states.moveInput.x:F2}, VelX={currentVelX:F2}");
                playerSprite.flipX = true;
                kickBox.offset = new Vector2(-Mathf.Abs(kickBox.offset.x), kickBox.offset.y);
                flipCooldownTimer = FLIP_COOLDOWN * 0.3f;
            }
            else if (shouldFlipRight && playerSprite.flipX)
            {
                GameLogger.LogMovement($"SPRITE FLIP RIGHT (Input): Input={states.moveInput.x:F2}, VelX={currentVelX:F2}");
                playerSprite.flipX = false;
                kickBox.offset = new Vector2(Mathf.Abs(kickBox.offset.x), kickBox.offset.y);
                flipCooldownTimer = FLIP_COOLDOWN * 0.3f;
            }
        }
        
        // Debug logging
        if (shouldUseElevatedThreshold && absVelX > VELOCITY_THRESHOLD && absVelX <= flipThreshold)
        {
            GameLogger.LogVerbose("MOVEMENT", $"FLIP PREVENTED: Near wall/building, VelX={currentVelX:F2}, Threshold={flipThreshold:F1}, WallTimer={wallThresholdTimer:F2}, BuildingTimer={buildingWallProximityTimer:F2}");
        }
    }

    public void StaggerPlayer(Vector2 knockbackDirection)
    {
        if (config == null || states == null)
        {
            GameLogger.LogError("[STAGGER] Config or States is null!");
            return;
        }

        GameLogger.Log($"[STAGGER] Starting stagger - duration: {config.staggerDuration}s, knockback: {knockbackDirection}");
    
        states.StartStagger(config.staggerDuration);

        AudioManager.Instance?.Play(AudioManager.SoundType.Stagger);
        GamepadVibrationManager.Instance?.TriggerHitVibration();

        Vector2 knockbackForce = knockbackDirection.normalized * config.staggerKnockbackForce;
        rb.linearVelocity = new Vector2(knockbackForce.x, Mathf.Max(knockbackForce.y, rb.linearVelocity.y));
    
        GameLogger.Log($"[STAGGER] Velocity set to: {rb.linearVelocity}");
    }


    private void HandleMovement()
    {
        if (config == null) return;

        float forceMultiplier = states.isKicking ? config.kickForceReduction : 1f;

        if (states.isStaggered)
        {
            forceMultiplier *= config.staggerMovementReduction;
        }

        states.animator.SetFloat(XVelocity, Mathf.Abs(rb.linearVelocity.x));
        states.animator.SetFloat(YVelocity, rb.linearVelocity.y);

        if (states.isWallRunning)
            HandleWallRunMovement(forceMultiplier);
        else if (states.isRolling)
            HandleRollMovement(forceMultiplier);
        else if (states.isSliding)
            HandleSlideMovement(forceMultiplier);
        else if (states.isDiving)
            HandleDiveMovement(forceMultiplier);
        else
            HandleNormalMovement(forceMultiplier);
    }

    private void HandleNormalMovement(float forceMultiplier)
    {
        if (states.isKicking) return;
        if (states.isCrouching || states.isSliding || states.isDiving || states.isRolling) return;

        float horizontalInput = states.moveInput.x;

        if (Mathf.Abs(horizontalInput) > 0.1f)
        {
            float targetSpeed = horizontalInput * config.maxRunSpeed;
            float speedDifference = targetSpeed - rb.linearVelocity.x;
    
            bool isAccelerating = Mathf.Approximately(Mathf.Sign(speedDifference), Mathf.Sign(horizontalInput));
            bool isBelowMaxSpeed = Mathf.Abs(rb.linearVelocity.x) < config.maxRunSpeed;
    
            if (isAccelerating || isBelowMaxSpeed)
            {
                float force = states.isGrounded ? config.runForce : config.runForce * config.airControlMultiplier;
                force *= forceMultiplier;

                float appliedForce = Mathf.Clamp(speedDifference * force, -force, force);
                rb.AddForce(Vector2.right * (appliedForce * Time.fixedDeltaTime), ForceMode2D.Force);
            }
        }

        if (states.crouchHeld && !states.isGrounded)
        {
            rb.AddForce(Vector2.down * (config.runForce * Time.fixedDeltaTime), ForceMode2D.Force);
        }
    }


    private void HandleSlideMovement(float forceMultiplier)
    {
        // Sliding uses momentum only - no horizontal input processed
        // Could add slide steering here if needed
    }

    private void HandleRollMovement(float forceMultiplier)
    {
        // Rolling uses momentum only, similar to sliding
        // No horizontal input processed during roll
    }

    private void HandleDiveMovement(float forceMultiplier)
    {
        // Diving maintains momentum with no horizontal control
        // Player is committed to the dive direction
    }

    private void HandleWallRunMovement(float forceMultiplier)
    {
        if (states.isKicking) return;

        float climbBoost = wallClimb != null ? wallClimb.GetWallRunForceMultiplier() : 1f;
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
        Vector2 velocity = rb.linearVelocity;
        float horizontalInput = states.moveInput.x;
        
        if (states.justLanded && states.landingGraceTimer > 0f)
        {
            GameLogger.LogVerbose("MOVEMENT", $"Landing grace active - maintaining velocity: {rb.linearVelocity.x:F2}, Timer: {states.landingGraceTimer:F2}");
            return;
        }
    
        if (!states.isGrounded || states.isKicking)
            return;
        
        if (states.isRolling)
        {
            if (Mathf.Abs(horizontalInput) < 0.1f || (horizontalInput * velocity.x) < 0)
            {
                Vector2 dragForces = -velocity * (config.slideDrag * 0.4f * Time.fixedDeltaTime);
                rb.AddForce(new Vector2(dragForces.x, 0), ForceMode2D.Force);
            }
            return;
        }
        
        if (states.isSliding)
        {
            if (Mathf.Abs(horizontalInput) < 0.1f || (horizontalInput * velocity.x) < 0)
            {
                Vector2 dragForces = -velocity * (config.slideDrag * 0.3f * Time.fixedDeltaTime);
                rb.AddForce(new Vector2(dragForces.x, 0), ForceMode2D.Force);
            }
            return;
        }
        
        float targetDrag = 0f;
        
        if (Mathf.Abs(horizontalInput) < 0.1f)
        {
            targetDrag = config.groundDrag;
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
                
                newHorizontalVelocity = Mathf.Clamp(newHorizontalVelocity, -config.maxRunSpeed * 2.5f, config.maxRunSpeed * 2.5f);
                
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
}

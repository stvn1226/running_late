using UnityEngine;

public class PlayerWallClimb : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerConfig config;
    [SerializeField] private PlayerStates states;
    [SerializeField] private Rigidbody2D rb;

    // Controls how slowly vY decays during the wall slide.
    // Lower value = rise sustains longer before flattening to lateral movement.
    private const float WALL_SLIDE_CURVE_RATE = 1.5f;

    private float wallDirection = 0f;
    private bool wasOnWall = false;
    private bool wasWallRunning = false;

    private void Awake()
    {
        if (config == null)
        {
            config = GetComponent<PlayerMovement>()?.config;
        }

        if (states == null)
        {
            states = GetComponent<PlayerStates>();
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }
    }

    private void Update()
    {
        if (states.isWallSliding && !states.isVaultingWall)
        {
            ApplyWallSlideVelocity();
            return; // Wall slide and wall run are mutually exclusive; return early.
        }

        if (states.isWallRunning && !states.isVaultingWall)
        {
            CheckForPerfectClimb();
            CheckCornerClimb();
            wallDirection = states.wallDirection;
            wasOnWall = true;
            wasWallRunning = true;
            
            if (states.isNearWallTop)
            {
                GameLogger.LogState($"CLIMBING NEAR WALL TOP! Distance: {states.distanceToWallTop:F2}, VelY: {rb.linearVelocity.y:F2}, PerfectClimb: {states.isPerfectClimb}");
            }
        }
        else if (wasOnWall && !states.isWallRunning && !states.isVaultingWall)
        {
            bool nearTop = states.isNearWallTop;
            
            if (!nearTop && rb.linearVelocity.y > 0.5f)
            {
                float playerTopY = transform.position.y + 1f;
                RaycastHit2D wallCheck = Physics2D.Raycast(
                    new Vector2(transform.position.x, playerTopY), 
                    Vector2.right * wallDirection, 
                    1f, 
                    config.wallLayer
                );
                
                if (wallCheck.collider == null)
                {
                    nearTop = true;
                    GameLogger.LogState($"FAST CLIMB DETECTION! Player cleared wall top while climbing fast. VelY: {rb.linearVelocity.y:F2}");
                }
            }
            
            GameLogger.LogState($"LEFT WALL - NearTop={nearTop}, Distance={states.distanceToWallTop:F2}, VelY={rb.linearVelocity.y:F2}, PerfectClimb={states.isPerfectClimb}, StoredMomentum={states.storedMomentum:F2}");
            
            if (nearTop)
            {
                GameLogger.LogState($"Player cleared wall top! Triggering vault. Distance: {states.distanceToWallTop:F2}, VelY: {rb.linearVelocity.y:F2}");
                TriggerWallVault();
            }
            else
            {
                HandleEarlyWallRelease();
            }
            
            states.distanceToWallTop = 999f;
            states.isNearWallTop = false;
            wasOnWall = false;
            wasWallRunning = false;
        }
        else if (wasWallRunning && !states.isWallRunning && !states.canWallRun && !states.isVaultingWall)
        {
            HandleEarlyWallRelease();
            wasWallRunning = false;
        }
    }

    /// <summary>
    /// Drives vertical velocity during a wall slide following a sqrt-shaped decay curve.
    /// vY starts at wallSlideSpeed and falls off as 1/sqrt(1 + t * CURVE_RATE), producing
    /// a strong initial rise that gradually smooths out to lateral movement over time.
    /// Horizontal velocity is left untouched so the player retains lateral momentum.
    /// </summary>
    private void ApplyWallSlideVelocity()
    {
        // vY = wallSlideSpeed / sqrt(1 + t * RATE).
        // At t=0 this equals wallSlideSpeed exactly; it decays following a sqrt shape
        // with no normalization loss, so the full speed is felt from the first frame.
        float vY = config.wallSlideSpeed / Mathf.Sqrt(1f + states.wallSlideTimer * WALL_SLIDE_CURVE_RATE);
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, vY);
    }

    private void HandleEarlyWallRelease()
    {
        if (states.isPerfectClimb && states.storedMomentum > 0f && !states.wallRunHeld)
        {
            float currentHorizontalSpeed = Mathf.Abs(rb.linearVelocity.x);
            
            if (currentHorizontalSpeed < states.storedMomentum)
            {
                float restoredMomentum = states.storedMomentum * wallDirection;
                rb.linearVelocity = new Vector2(restoredMomentum, rb.linearVelocity.y);
                
                GameLogger.LogState($"MOMENTUM RESTORED! Released wall run button early during perfect climb. Restored: {states.storedMomentum:F2} m/s (Direction: {wallDirection}), Current VelX: {rb.linearVelocity.x:F2}, VelY: {rb.linearVelocity.y:F2}");
                
                if (AnalyticsManager.Instance != null)
                {
                    AnalyticsManager.Instance.TrackEvent("perfect_climb_momentum_restored", new System.Collections.Generic.Dictionary<string, object>
                    {
                        { "stored_momentum", states.storedMomentum },
                        { "wall_direction", wallDirection },
                        { "velocity_y", rb.linearVelocity.y },
                        { "previous_velocity_x", currentHorizontalSpeed }
                    });
                }
            }
            else
            {
                GameLogger.LogVerbose("WALLCLIMB", $"Momentum NOT restored - current speed ({currentHorizontalSpeed:F2}) >= stored ({states.storedMomentum:F2})");
            }
        }
        
        ResetClimbState();
    }

    private void CheckForPerfectClimb()
    {
        if (states.isPerfectClimb)
        {
            return;
        }

        float timeSinceInput = Time.time - states.wallClimbInputTime;
        float timeSinceContact = Time.time - states.wallContactTime;

        if (timeSinceInput < config.perfectClimbWindow && timeSinceContact < config.perfectClimbWindow)
        {
            float inputToContactDelta = Mathf.Abs(states.wallClimbInputTime - states.wallContactTime);

            if (inputToContactDelta < config.perfectClimbWindow)
            {
                ActivatePerfectClimb();
            }
        }
    }
    
    private void CheckCornerClimb()
    {
        if (config == null) return;
        if (!config.enableCornerClimb) return;
        if (states.isCornerClimbing) return;
        if (states.isVaultingWall) return;
    
        bool isInCornerRange = states.distanceToWallTop < config.cornerClimbDetectionDistance 
                               && states.distanceToWallTop > config.wallVaultTriggerDistance;
    
        bool hasProblematicVelocity = rb.linearVelocity.y > config.cornerClimbMinVelocity 
                                      && rb.linearVelocity.y < config.cornerClimbMaxVelocity;
    
        bool isOnWall = states.isWallRunning || (states.canWallRun && Mathf.Abs(states.moveInput.x) > 0.1f);
    
        if (isInCornerRange && hasProblematicVelocity && isOnWall)
        {
            GameLogger.LogState($"[CORNER CLIMB ASSIST] Activating! Distance: {states.distanceToWallTop:F2}m, VelY: {rb.linearVelocity.y:F2} m/s");
            TriggerCornerClimb();
        }
    }
    
    private void TriggerCornerClimb()
    {
        states.isCornerClimbing = true;
        states.cornerClimbTimer = config.cornerClimbDuration;
    
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, config.cornerClimbBoostForce);
    
        GameLogger.LogState($"CORNER CLIMB BOOST! Applied {config.cornerClimbBoostForce} m/s upward velocity");
    
        if (AnalyticsManager.Instance != null)
        {
            AnalyticsManager.Instance.TrackEvent("corner_climb_triggered", new System.Collections.Generic.Dictionary<string, object>
            {
                { "distance_to_top", states.distanceToWallTop },
                { "velocity_y_before", rb.linearVelocity.y },
                { "boost_applied", config.cornerClimbBoostForce },
                { "wall_direction", states.wallDirection }
            });
        }
    }

    private void ActivatePerfectClimb()
    {
        states.isPerfectClimb = true;
        states.storedMomentum = Mathf.Abs(rb.linearVelocity.x);

        GameLogger.LogState($"PERFECT CLIMB ACTIVATED! Stored momentum: {states.storedMomentum:F2}, Boost: {config.perfectClimbBoostMultiplier}x");

        if (AnalyticsManager.Instance != null)
        {
            AnalyticsManager.Instance.TrackEvent("perfect_climb_activated", new System.Collections.Generic.Dictionary<string, object>
            {
                { "stored_momentum", states.storedMomentum },
                { "boost_multiplier", config.perfectClimbBoostMultiplier }
            });
        }
    }

    private void TriggerWallVault()
    {
        states.StartVault(config.wallVaultDuration);

        Vector2 currentVel = rb.linearVelocity;
        float forwardBoost = config.wallVaultHorizontalBoost * wallDirection;
        float dampedVertical = currentVel.y * config.wallVaultVerticalDamping;

        rb.linearVelocity = new Vector2(forwardBoost, Mathf.Max(dampedVertical, 0f));

        GameLogger.LogState($"WALL VAULT TRIGGERED! Direction: {wallDirection}, Horizontal: {forwardBoost:F2}, Vertical: {dampedVertical:F2} (was {currentVel.y:F2}), Final: {rb.linearVelocity}");

        if (AnalyticsManager.Instance != null)
        {
            AnalyticsManager.Instance.TrackEvent("wall_vault_triggered", new System.Collections.Generic.Dictionary<string, object>
            {
                { "distance_to_top", states.distanceToWallTop },
                { "horizontal_boost", forwardBoost },
                { "perfect_climb", states.isPerfectClimb },
                { "stored_momentum", states.storedMomentum }
            });
        }
        
        ResetClimbState();
    }

    public float GetWallRunForceMultiplier()
    {
        return states.isPerfectClimb ? config.perfectClimbBoostMultiplier : 1f;
    }

    public void ResetClimbState()
    {
        states.isPerfectClimb = false;
        states.storedMomentum = 0f;
        states.wallClimbInputTime = -999f;
    }
}

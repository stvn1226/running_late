using UnityEngine;

public class PlayerDive : MonoBehaviour
{
    [Header("References")]
    public PlayerConfig config;
    
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerStates states;
    [SerializeField] private PlayerPhysics playerPhysics;

    private void Start()
    {
        if (states != null)
        {
            states.OnLanded += OnLanding;
        }
    }

    private void OnDestroy()
    {
        if (states != null)
        {
            states.OnLanded -= OnLanding;
        }
    }

    private void Update()
    {
        if (states.isStaggered || states.isKicking) return;

        if (states.isDiving && states.canWallRun)
        {
            states.EndDive();
            GameLogger.LogMovement("DIVE ENDED: Wall contact detected");
        }

        HandleDiveInput();
        HandleDiveToRollTransition();
        HandleRollExit();
    }

    private void HandleRollExit()
    {
        if (states.isRolling && !states.isGrounded)
        {
            states.EndRoll();
            GameLogger.LogMovement("ROLL: Cancelled - player left ground");
        }
    }

    private void HandleDiveInput()
    {
        if (config == null) return;

        if (states.divePressed && states.canDive && !states.isDiving && !states.isRolling)
        {
            bool hasMinimumSpeed = Mathf.Abs(rb.linearVelocity.x) >= config.minDiveVelocity || Mathf.Abs(states.moveInput.x) > 0.1f;
            
            if (states.isGrounded)
            {
                if (hasMinimumSpeed)
                {
                    PerformGroundedDive();
                }
                else
                {
                    GameLogger.LogMovement($"DIVE: Cancelled - insufficient velocity ({rb.linearVelocity.x:F2}) and no input");
                }
            }
            else
            {
                if (hasMinimumSpeed)
                {
                    PerformAirDive();
                }
                else
                {
                    GameLogger.LogMovement($"DIVE: Cancelled - insufficient air velocity ({rb.linearVelocity.x:F2})");
                }
            }
        }
    }

    private void PerformGroundedDive()
    {
        float diveDirection = Mathf.Abs(rb.linearVelocity.x) > 0.1f ? Mathf.Sign(rb.linearVelocity.x) : Mathf.Sign(states.moveInput.x);
        
        if (diveDirection == 0f) diveDirection = 1f;

        StartRoll();

        float currentHorizontalVelocity = rb.linearVelocity.x;
        float targetVelocity = currentHorizontalVelocity + (config.diveHorizontalBoost * diveDirection * 0.5f);
        
        rb.linearVelocity = new Vector2(targetVelocity, 0f);

        GameLogger.LogMovement($"GROUNDED DIVE: Direction={diveDirection}, Current={currentHorizontalVelocity:F2}, Final={targetVelocity:F2}");
        
        if (AnalyticsManager.Instance != null)
        {
            var context = AnalyticsEvents.CreatePlayerActionContext(
                "grounded_dive", 
                transform.position.x, 
                rb.linearVelocity.x
            );
            context["dive_direction"] = diveDirection;
            AnalyticsManager.Instance.TrackPlayerAction("dive_grounded", context);
        }
    }

    private void PerformAirDive()
    {
        float diveDirection = Mathf.Sign(rb.linearVelocity.x);
        if (diveDirection == 0f) diveDirection = Mathf.Sign(states.moveInput.x);
        if (diveDirection == 0f) diveDirection = 1f;

        states.StartDive(config.diveDuration);
        states.canDive = false;

        float currentHorizontalVelocity = rb.linearVelocity.x;
        float currentVerticalVelocity = rb.linearVelocity.y;
        
        float newHorizontalVelocity = currentHorizontalVelocity + (config.diveHorizontalBoost * diveDirection);
        float newVerticalVelocity = currentVerticalVelocity * config.diveVerticalReduction;

        rb.linearVelocity = new Vector2(newHorizontalVelocity, newVerticalVelocity);

        GameLogger.LogMovement($"AIR DIVE: Direction={diveDirection}, HVel={currentHorizontalVelocity:F2}->{newHorizontalVelocity:F2}, VVel={currentVerticalVelocity:F2}->{newVerticalVelocity:F2}");
        
        AudioManager.Instance?.Play(AudioManager.SoundType.Jump);
        
        if (AnalyticsManager.Instance != null)
        {
            var context = AnalyticsEvents.CreatePlayerActionContext(
                "air_dive", 
                transform.position.x, 
                rb.linearVelocity.x
            );
            context["dive_direction"] = diveDirection;
            context["horizontal_boost"] = config.diveHorizontalBoost;
            context["vertical_reduction"] = config.diveVerticalReduction;
            AnalyticsManager.Instance.TrackPlayerAction("dive_air", context);
        }
    }

    private void HandleDiveToRollTransition()
    {
        if (states.isDiving && states.isGrounded && !states.isRolling)
        {
            states.EndDive();
            StartRoll();
            
            GameLogger.LogMovement($"DIVE TO ROLL: Transitioned on landing, VelX={rb.linearVelocity.x:F2}");
        }
    }

    private void StartRoll()
    {
        states.StartRoll(config.rollDuration);
        
        states.isCrouching = true;
        states.isSliding = true;
        
        if (playerPhysics != null)
        {
            playerPhysics.UpdateColliderSize();
        }
        
        GameLogger.LogMovement($"ROLL: Started with velocity {rb.linearVelocity.x:F2}");
    }

    public void OnLanding()
    {
        states.ResetDive();
    }

    public void OnDoubleJump()
    {
        if (states.isDiving)
        {
            states.EndDive();
        }
        states.ResetDive();
        GameLogger.LogMovement("DIVE: Cancelled and reset on double jump");
    }

    public void OnObstacleBounce()
    {
        if (states.isDiving)
        {
            states.EndDive();
        }
        states.ResetDive();
        GameLogger.LogMovement("DIVE: Cancelled and reset on obstacle bounce");
    }
}

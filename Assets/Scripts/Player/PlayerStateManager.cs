#define ENABLE_VERBOSE_LOGGING

using UnityEngine;

public class PlayerStateManager : MonoBehaviour
{
    [Header("References")]
    public PlayerConfig config;
    
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerStates states;
    [SerializeField] private PlayerPhysics playerPhysics;
    private PlayerAirDodge playerAirDodge;

    // Logging variables
    private bool previousCrouching;
    private bool previousSliding;
    private bool previousWallRunning;
    private bool previousRolling;
    private float lastStateLog = 0f;
    private const float STATE_LOG_INTERVAL = 1f;
    
    private void Awake()
    {
        playerAirDodge = GetComponent<PlayerAirDodge>();
    }

    private void Update()
    {
        HandleStates();
        HandleExpressionChange();

        // CableManager.instance.isFastFalling = states.crouchHeld;
        CableManager.instance.isFastFalling = states.crouchHeld && !states.isGrounded;
        
        // Periodic state logging
        if (Time.time - lastStateLog > STATE_LOG_INTERVAL)
        {
            GameLogger.LogVerbose("STATE", $"STATE: Crouch={states.isCrouching}, Slide={states.isSliding}, Roll={states.isRolling}, Dive={states.isDiving}, WallRun={states.isWallRunning}, Grounded={states.isGrounded}, Speed={Mathf.Abs(rb.linearVelocity.x):F1}");
            lastStateLog = Time.time;
        }
    }

    private void HandleExpressionChange()
    {
        if (states.hasToast)
        {
            if (states.isStaggered)
            {
                ExpressionManager.Instance?.SwitchExpression(9);
            }
            else if (states.isNearObstacle)
            {
                ExpressionManager.Instance?.SwitchExpression(7);
            }
            else if (states.isNearPowerUp)
            {
                ExpressionManager.Instance?.SwitchExpression(5);
            }
            else if (states.isSlowFalling)
            {
                ExpressionManager.Instance?.SwitchExpression(15);
            }
            else if (states.hasInvincibilityPowerUp)
            {
                ExpressionManager.Instance?.SwitchExpression(17);
            }
            else if (states.isShielded)
            {
                ExpressionManager.Instance?.SwitchExpression(19);
            }
            else if (states.hasHighJump && states.isJumping)
            {
                ExpressionManager.Instance?.SwitchExpression(12);
            }
            else if (states.hasHighJump && !states.isJumping)
            {
                ExpressionManager.Instance?.SwitchExpression(11);
            }
            else if (states.hasDoubleJump)
            {
                ExpressionManager.Instance?.SwitchExpression(10);
            }
            else if (states.hasFastRun)
            {
                ExpressionManager.Instance?.SwitchExpression(3);
            }
            else
            {
                ExpressionManager.Instance?.SwitchExpression(1);
            }
        }
        else
        {
            if (states.isStaggered)
            {
                ExpressionManager.Instance?.SwitchExpression(9);
            }
            else if (states.isNearObstacle)
            {
                ExpressionManager.Instance?.SwitchExpression(8);
            }
            else if (states.isNearPowerUp)
            {
                ExpressionManager.Instance?.SwitchExpression(6);
            }
            else if (states.isSlowFalling)
            {
                ExpressionManager.Instance?.SwitchExpression(16);
            }
            else if (states.hasInvincibilityPowerUp)
            {
                ExpressionManager.Instance?.SwitchExpression(18);
            }
            else if (states.isShielded)
            {
                ExpressionManager.Instance?.SwitchExpression(19);
            }
            else if (states.hasHighJump && states.isJumping)
            {
                ExpressionManager.Instance?.SwitchExpression(14);
            }
            else if (states.hasHighJump && !states.isJumping)
            {
                ExpressionManager.Instance?.SwitchExpression(13);
            }
            else if (states.hasDoubleJump)
            {
                ExpressionManager.Instance?.SwitchExpression(10);
            }
            else if (states.hasFastRun)
            {
                ExpressionManager.Instance?.SwitchExpression(4);
            }
            else
            {
                ExpressionManager.Instance?.SwitchExpression(2);
            }
        }
    }

    private void HandleStates()
    {
        if (config == null) return;

        previousCrouching = states.isCrouching;
        previousSliding = states.isSliding;
        previousWallRunning = states.isWallRunning;
        previousRolling = states.isRolling;

        HandleRollSafety();

        HandleCrouchAndSlide();

        HandleWallRun(previousWallRunning);

        HandleWallSlide();

        HandleKick();

        if (previousCrouching != states.isCrouching || previousSliding != states.isSliding || previousRolling != states.isRolling)
        {
            GameLogger.LogState($"COLLIDER UPDATE TRIGGERED: Crouch {previousCrouching}->{states.isCrouching}, Slide {previousSliding}->{states.isSliding}, Roll {previousRolling}->{states.isRolling}");
            playerPhysics?.UpdateColliderSize();
        }
    }

    private void HandleRollSafety()
    {
        if (states.isRolling)
        {
            float currentSpeed = Mathf.Abs(rb.linearVelocity.x);
            
            if (!states.isGrounded)
            {
                GameLogger.LogState("ROLL SAFETY: Player left ground while rolling, ending roll");
                states.EndRoll();
            }
            else if (states.isWallRunning)
            {
                GameLogger.LogState("ROLL SAFETY: Player started wall running while rolling, ending roll");
                states.EndRoll();
            }
            else if (currentSpeed < 0.3f)
            {
                GameLogger.LogState($"ROLL ENDED: Speed too low ({currentSpeed:F2})");
                states.EndRoll();
            }
            else if (states.rollTimer <= 0f && (states.isCrouching || states.isSliding))
            {
                GameLogger.LogState("ROLL SAFETY: Roll timer expired but crouch/slide flags still set, force clearing");
                states.EndRoll();
            }
        }
    }

    private void HandleCrouchAndSlide()
    {
        if (states.isRolling) return;
        if (states.isSlidingOnCable) return;

        bool isOnGroundOrSlope = states.isGrounded || (states.isOnSlope && rb.linearVelocity.y < 2f);
        bool shouldProcessCrouchSlide = states.crouchHeld && isOnGroundOrSlope && !states.isWallRunning && !states.isJumping;
        
        if (shouldProcessCrouchSlide)
        {
            float currentSpeed = Mathf.Abs(rb.linearVelocity.x);
            
            bool shouldSlide = currentSpeed > 2f || (states.justLanded && currentSpeed > 1f);
            
            bool isSlidingDownhill = states.isOnSlope && !states.isSlopeTooSteep;
            if (isSlidingDownhill)
            {
                Vector2 slopeRight = new Vector2(states.slopeNormal.y, -states.slopeNormal.x);
                float currentDir = Mathf.Sign(rb.linearVelocity.x);
                if (currentDir != 0f)
                {
                    Vector2 slopeDir = slopeRight * currentDir;
                    isSlidingDownhill = slopeDir.y < 0f;
                }
            }
            
            float slideEndThreshold = isSlidingDownhill ? 0.1f : 0.3f;
            
            if (states.isSliding && currentSpeed < slideEndThreshold)
            {
                GameLogger.LogState($"SLIDE ENDED: Speed too low ({currentSpeed:F2})");
                states.isSliding = false;
                states.isCrouching = true;
                return;
            }
            
            if (shouldSlide)
            {
                if (!states.isSliding)
                {
                    GameLogger.LogState($"STARTED SLIDING: Speed={currentSpeed:F1}, JustLanded={states.justLanded}");
                    
                    if (AnalyticsManager.Instance != null)
                    {
                        Vector3 playerPos = transform.position;
                        var context = AnalyticsEvents.CreatePlayerActionContext(
                            AnalyticsEvents.PLAYER_SLIDE, 
                            playerPos.x, 
                            rb.linearVelocity.x
                        );
                        context["current_speed"] = currentSpeed;
                        context["just_landed"] = states.justLanded;
            
                        AnalyticsManager.Instance.TrackPlayerAction(AnalyticsEvents.PLAYER_SLIDE, context);
                    }
                }
                states.isCrouching = false;
                states.isSliding = true;
            }
            else if (!states.isSliding)
            {
                if (!states.isCrouching)
                {
                    GameLogger.LogState($"STARTED CROUCHING: Speed={currentSpeed:F1}");
                }
                states.isCrouching = true;
                states.isSliding = false;
            }
        }
        else
        {
            if (states.isCrouching || states.isSliding)
            {
                GameLogger.LogState($"STOPPED CROUCH/SLIDE: CrouchHeld={states.crouchHeld}, Grounded={states.isGrounded}, WallRun={states.isWallRunning}, Jumping={states.isJumping}");
            }
            states.isCrouching = false;
            states.isSliding = false;
        }
    }

    private void HandleWallRun(bool previousWallRunning)
    {
        if (states.wallRunHeld && !states.canWallRun)
        {
            states.RecordWallClimbInput();
        }

        // bool shouldWallRun = states.wallRunHeld && states.canWallRun && Mathf.Abs(states.moveInput.x) > 0.1f;
        bool shouldWallRun = states.wallRunHeld && states.canWallRun && Mathf.Abs(states.moveInput.x) > 0.1f && !states.isRolling;
        
        if (shouldWallRun && !previousWallRunning && rb.linearVelocity.y < -1f)
        {
            Vector2 currentVelocity = rb.linearVelocity;
            
            float reducedFallSpeed = Mathf.Max(currentVelocity.y * 0.5f, -8f);
            
            rb.linearVelocity = new Vector2(currentVelocity.x, reducedFallSpeed + 2f);
            
            GameLogger.LogState($"Wall run started while falling! Velocity adjusted from {currentVelocity.y:F2} to {rb.linearVelocity.y:F2}");
            
            if (AnalyticsManager.Instance != null)
            {
                Vector3 playerPos = transform.position;
                var context = AnalyticsEvents.CreatePlayerActionContext(
                    AnalyticsEvents.PLAYER_WALLRUN, 
                    playerPos.x, 
                    rb.linearVelocity.x
                );
                context["velocity_y"] = currentVelocity.y;
                context["adjusted_velocity_y"] = rb.linearVelocity.y;
        
                AnalyticsManager.Instance.TrackPlayerAction(AnalyticsEvents.PLAYER_WALLRUN, context);
            }
        }
        
        if (shouldWallRun != states.isWallRunning)
        {
            GameLogger.LogState($"WALL RUN CHANGE: {states.isWallRunning} -> {shouldWallRun}, WallRunHeld={states.wallRunHeld}, CanWallRun={states.canWallRun}, Input.x={states.moveInput.x:F2}");
            
            if (shouldWallRun && !states.isWallRunning)
            {
                if (states.isDiving)
                {
                    GameLogger.LogState("DIVE INTERRUPTED: Wall run started while diving");
                    states.EndDive();
                }

                if (playerAirDodge != null)
                {
                    playerAirDodge.OnWallRunStart();
                }
            }
        }
        
        states.isWallRunning = shouldWallRun;
    }

    private void HandleWallSlide()
    {
        if (states.wallRunHeld && !states.canWallSlide)
        {
            states.RecordWallClimbInput();
        }

        bool shouldWallSlide = states.wallRunHeld && states.canWallSlide && !states.isGrounded;

        if (shouldWallSlide == states.isWallSliding) return;

        GameLogger.LogState($"WALL SLIDE CHANGE: {states.isWallSliding} -> {shouldWallSlide}, WallRunHeld={states.wallRunHeld}, CanWallSlide={states.canWallSlide}");

        if (shouldWallSlide)
        {
            states.StartWallSlide();
        }
        else
        {
            states.EndWallSlide();
        }
    }

    private void HandleKick()
    {
        if (states.kickPressed)
        {
            GameLogger.LogState($"KICK STARTED: Duration={config.kickDuration}");
            // states.kickTimer = config.kickDuration;
            states.kickPressed = false;
        }
    }

}

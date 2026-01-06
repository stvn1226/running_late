using UnityEngine;

public class PlayerStateManager : MonoBehaviour
{
    [Header("References")]
    public PlayerConfig config;
    
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerStates states;
    [SerializeField] private PlayerPhysics playerPhysics;

    // Logging variables
    private bool previousCrouching;
    private bool previousSliding;
    private bool previousWallRunning;
    private bool previousRolling;
    private float lastStateLog = 0f;
    private const float STATE_LOG_INTERVAL = 1f;

    private void Update()
    {
        HandleStates();

        CableManager.instance.isFastFalling = states.crouchHeld;
        
        // Periodic state logging
        if (Time.time - lastStateLog > STATE_LOG_INTERVAL)
        {
            GameLogger.LogVerbose("STATE", $"STATE: Crouch={states.isCrouching}, Slide={states.isSliding}, Roll={states.isRolling}, Dive={states.isDiving}, WallRun={states.isWallRunning}, Grounded={states.isGrounded}, Speed={Mathf.Abs(rb.linearVelocity.x):F1}");
            lastStateLog = Time.time;
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
            else if (states.rollTimer <= 0f && (states.isCrouching || states.isSliding))
            {
                GameLogger.LogState("ROLL SAFETY: Roll timer expired but crouch/slide flags still set, force clearing");
                states.EndRoll();
            }
        }
    }

    private void HandleCrouchAndSlide()
    {
        // Don't interfere with rolling state
        if (states.isRolling) return;
        if (states.isSlidingOnCable) return;

        bool shouldProcessCrouchSlide = states.crouchHeld && states.isGrounded && !states.isWallRunning && !states.isJumping;
        
        if (shouldProcessCrouchSlide)
        {
            float currentSpeed = Mathf.Abs(rb.linearVelocity.x);
            
            bool shouldSlide = currentSpeed > 2f || (states.justLanded && currentSpeed > 1f);
            
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
            else
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

        bool shouldWallRun = states.wallRunHeld && states.canWallRun && Mathf.Abs(states.moveInput.x) > 0.1f;
        
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
                PlayerAirDodge airDodge = GetComponent<PlayerAirDodge>();
                if (airDodge != null)
                {
                    airDodge.OnWallRunStart();
                }
            }
        }
        
        states.isWallRunning = shouldWallRun;
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

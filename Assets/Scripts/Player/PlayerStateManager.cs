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
    private float lastStateLog = 0f;
    private const float STATE_LOG_INTERVAL = 1f;

    private void Update()
    {
        HandleStates();
        
        // Periodic state logging
        if (Time.time - lastStateLog > STATE_LOG_INTERVAL)
        {
            GameLogger.LogVerbose("STATE", $"STATE: Crouch={states.isCrouching}, Slide={states.isSliding}, WallRun={states.isWallRunning}, Grounded={states.isGrounded}, Speed={Mathf.Abs(rb.linearVelocity.x):F1}");
            lastStateLog = Time.time;
        }
    }

    private void HandleStates()
    {
        if (config == null) return;

        previousCrouching = states.isCrouching;
        previousSliding = states.isSliding;
        previousWallRunning = states.isWallRunning;

        HandleCrouchAndSlide();

        HandleWallRun(previousWallRunning);

        HandleKick();

        if (previousCrouching != states.isCrouching || previousSliding != states.isSliding)
        {
            GameLogger.LogState($"COLLIDER UPDATE TRIGGERED: Crouch {previousCrouching}->{states.isCrouching}, Slide {previousSliding}->{states.isSliding}");
            playerPhysics?.UpdateColliderSize();
        }
    }

    private void HandleCrouchAndSlide()
    {
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
        bool shouldWallRun = states.wallRunHeld && states.canWallRun && Mathf.Abs(states.moveInput.x) > 0.1f;
        
        if (shouldWallRun && !previousWallRunning && rb.linearVelocity.y < -1f)
        {
            Vector2 currentVelocity = rb.linearVelocity;
            
            float reducedFallSpeed = Mathf.Max(currentVelocity.y * 0.5f, -8f);
            
            rb.linearVelocity = new Vector2(currentVelocity.x, reducedFallSpeed + 2f);
            
            GameLogger.LogState($"Wall run started while falling! Velocity adjusted from {currentVelocity.y:F2} to {rb.linearVelocity.y:F2}");
        }
        
        if (shouldWallRun != states.isWallRunning)
        {
            GameLogger.LogState($"WALL RUN CHANGE: {states.isWallRunning} -> {shouldWallRun}, WallRunHeld={states.wallRunHeld}, CanWallRun={states.canWallRun}, Input.x={states.moveInput.x:F2}");
        }
        
        states.isWallRunning = shouldWallRun;
    }

    private void HandleKick()
    {
        if (states.kickPressed)
        {
            GameLogger.LogState($"KICK STARTED: Duration={config.kickDuration}");
            states.kickTimer = config.kickDuration;
            states.kickPressed = false;
        }
    }
}

using UnityEngine;

public class PlayerStateManager : MonoBehaviour
{
    [Header("References")]
    public PlayerConfig config;
    
    private Rigidbody2D rb;
    private PlayerStates states;
    private PlayerPhysics playerPhysics;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        states = GetComponent<PlayerStates>();
        playerPhysics = GetComponent<PlayerPhysics>();
    }

    private void Update()
    {
        HandleStates();
    }

    private void HandleStates()
    {
        if (config == null) return;

        // Store previous states to detect changes
        bool previousCrouching = states.isCrouching;
        bool previousSliding = states.isSliding;
        bool previousWallRunning = states.isWallRunning;

        // Handle crouching and sliding
        HandleCrouchAndSlide();

        // Handle wall running
        HandleWallRun(previousWallRunning);

        // Handle kicking
        HandleKick();

        // Update collider size if crouching or sliding state changed
        if (previousCrouching != states.isCrouching || previousSliding != states.isSliding)
        {
            playerPhysics?.UpdateColliderSize();
        }
    }

    private void HandleCrouchAndSlide()
    {
        // Both crouching and sliding require the crouch button to be held AND player to be grounded
        // This prevents immediately re-entering crouch/slide after jumping out of it
        if (states.crouchHeld && states.isGrounded && !states.isWallRunning && !states.isJumping)
        {
            // Determine if we should crouch or slide based on velocity
            if (Mathf.Abs(rb.linearVelocity.x) > 1f)
            {
                // Moving fast while crouching = sliding
                states.isCrouching = false;
                states.isSliding = true;
            }
            else
            {
                // Stationary or slow while crouching = crouching
                states.isCrouching = true;
                states.isSliding = false;
            }
        }
        else
        {
            // No crouch button held, not grounded, or jumping = no crouching or sliding
            states.isCrouching = false;
            states.isSliding = false;
        }
    }

    private void HandleWallRun(bool previousWallRunning)
    {
        // Enhanced wall running state - allow starting wall run even when falling
        bool shouldWallRun = states.wallRunHeld && states.canWallRun && Mathf.Abs(states.moveInput.x) > 0.1f;
        
        // If transitioning from not wall running to wall running while falling, give a velocity boost
        if (shouldWallRun && !previousWallRunning && rb.linearVelocity.y < -1f)
        {
            // Player is falling and starting to wall run - help them stick to the wall
            Vector2 currentVelocity = rb.linearVelocity;
            
            // Reduce downward velocity to make wall running feel more responsive
            float reducedFallSpeed = Mathf.Max(currentVelocity.y * 0.5f, -8f); // Cap at -8 to prevent excessive slowdown
            
            // Apply slight upward boost to help initiate wall run
            rb.linearVelocity = new Vector2(currentVelocity.x, reducedFallSpeed + 2f);
            
            Debug.Log($"Wall run started while falling! Velocity adjusted from {currentVelocity.y:F2} to {rb.linearVelocity.y:F2}");
        }
        
        states.isWallRunning = shouldWallRun;
    }

    private void HandleKick()
    {
        if (states.kickPressed)
        {
            states.kickTimer = config.kickDuration;
            states.kickPressed = false; // Reset the press flag
        }
    }
}
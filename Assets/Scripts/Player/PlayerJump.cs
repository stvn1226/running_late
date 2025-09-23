using UnityEngine;

public class PlayerJump : MonoBehaviour
{
    [Header("References")]
    public PlayerConfig config;
    
    private Rigidbody2D rb;
    private PlayerStates states;
    private float originalGravityScale;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        states = GetComponent<PlayerStates>();
        originalGravityScale = rb.gravityScale;
    }

    private void Update()
    {
        if (!states.isStaggered) 
        { 
            HandleJumpInput();
            HandleJumpCut();
        }

        UpdateJumpPhysics();
    }

    private void HandleJumpInput()
    {
        if (config == null || states.isKicking) return;

        // Handle coyote time
        if (states.wasGrounded && !states.isGrounded && !states.isJumping)
        {
            states.StartCoyoteTime(config.coyoteTime);
        }
        
        // Execute jump - jumping should cancel crouch/slide states
        if (states.jumpInputBuffered && (states.isGrounded || states.canCoyoteJump))
        {
            // Jump cancels crouch and slide states
            if (states.isCrouching || states.isSliding)
            {
                CancelCrouchSlideStates();
            }
            PerformJump();
        }
        else if (states.jumpInputBuffered && states.isWallRunning)
        {
            WallJump();
        }
    }

    private void PerformJump()
    {
        // Reset vertical velocity for consistent jump height
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        
        // Apply minimum jump force initially
        rb.AddForce(Vector2.up * config.minJumpForce, ForceMode2D.Impulse);
        
        // Reset jump state
        states.StartJump();
    }

    private void CancelCrouchSlideStates()
    {
        // Force exit crouch and slide states when jumping
        states.isCrouching = false;
        states.isSliding = false;
        
        // Also notify the physics component to reset collider size
        PlayerPhysics playerPhysics = GetComponent<PlayerPhysics>();
        if (playerPhysics != null)
        {
            playerPhysics.UpdateColliderSize();
        }
    }

    private void HandleJumpCut()
    {
        if (config == null) return;

        // Variable jump: apply additional upward force while jump is held
        if (states.jumpHeld && states.isJumping && !states.isGrounded && states.jumpHoldTimer < config.jumpHoldTime)
        {
            // Calculate additional force based on how long jump has been held
            float holdProgress = states.jumpHoldTimer / config.jumpHoldTime;
            float additionalForce = Mathf.Lerp(0f, config.maxJumpForce - config.minJumpForce, holdProgress);
            
            // Apply additional upward force for variable jump height
            float frameForce = additionalForce * (Time.deltaTime / config.jumpHoldTime);
            rb.AddForce(Vector2.up * frameForce, ForceMode2D.Force);
        }
        
        // Stop applying extra force if jump button is released or max time reached
        if (!states.jumpHeld || states.jumpHoldTimer >= config.jumpHoldTime)
        {
            states.isJumpCut = true;
        }
        
        // Reset jumping state when landing
        if (states.isGrounded && states.isJumping)
        {
            states.ResetJumpState();
        }
    }

    private void UpdateJumpPhysics()
    {
        if (config == null) return;

        states.isFalling = rb.linearVelocity.y < -0.1f;
        
        if (states.isWallRunning)
        {
            rb.gravityScale = config.wallRunGravityScale;
        }
        else if (states.isFalling || states.isJumpCut)
        {
            rb.gravityScale = (states.crouchHeld && !states.isGrounded) ? config.fastFallGravityScale : config.fallGravityScale;
        }
        else
        {
            rb.gravityScale = originalGravityScale;
        }
    }

    private void WallJump()
    {
        if (config == null) return;

        Vector2 wallJumpDirection = new Vector2(-states.wallDirection, 1).normalized;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(wallJumpDirection * config.maxJumpForce * config.wallJumpMultiplier, ForceMode2D.Impulse);
        states.isWallRunning = false;
        states.StartJump();
    }
}

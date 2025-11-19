using MoreMountains.Feedbacks;
using UnityEngine;

public class PlayerJump : MonoBehaviour
{
    [Header("References")]
    public PlayerConfig config;
    
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerStates states;
    private float originalGravityScale;
    public MMF_Player jumpFeedbacks;

    private void Awake()
    {
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
        bool shouldLongJump = ShouldPerformLongJump();
        
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        
        if (shouldLongJump)
        {
            PerformLongJump();
        }
        else
        {
            rb.AddForce(Vector2.up * config.minJumpForce, ForceMode2D.Impulse);
        }
        
        states.StartJump();

        jumpFeedbacks.PlayFeedbacks();
        
        if (AnalyticsManager.Instance != null)
        {
            var context = AnalyticsEvents.CreatePlayerActionContext(
                shouldLongJump ? "long_jump" : "jump", 
                transform.position.x, 
                rb.linearVelocity.x
            );
            
            if (shouldLongJump)
            {
                context["horizontal_boost"] = config.longJumpHorizontalBoost;
                context["vertical_multiplier"] = config.longJumpVerticalMultiplier;
            }
            
            AnalyticsManager.Instance.TrackPlayerAction(
                shouldLongJump ? "long_jump" : AnalyticsEvents.PLAYER_JUMP, 
                context
            );
        }
    }

    private bool ShouldPerformLongJump()
    {
        bool crouchIsHeld = states.crouchHeld;
        bool crouchWasRecentlyPressed = states.crouchInputBuffered;
        bool isMoving = Mathf.Abs(states.moveInput.x) > 0.1f;
        
        return (crouchIsHeld || crouchWasRecentlyPressed) && isMoving;
    }

    private void PerformLongJump()
    {
        float verticalForce = config.minJumpForce * config.longJumpVerticalMultiplier;
        float horizontalDirection = Mathf.Sign(states.moveInput.x);
        float horizontalForce = config.longJumpHorizontalBoost * horizontalDirection;
        
        rb.AddForce(Vector2.up * verticalForce, ForceMode2D.Impulse);
        rb.AddForce(Vector2.right * horizontalForce, ForceMode2D.Impulse);
        
        states.isLongJumping = true;
        states.crouchInputBuffered = false;
        
        if (states.isCrouching || states.isSliding)
        {
            CancelCrouchSlideStates();
        }
        
        GameLogger.LogMovement($"LONG JUMP! Vertical: {verticalForce:F2}, Horizontal: {horizontalForce:F2}, Direction: {horizontalDirection}");
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

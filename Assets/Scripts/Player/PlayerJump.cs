using MoreMountains.Feedbacks;
using System;
using UnityEngine;

public class PlayerJump : MonoBehaviour, ITimedPowerUp
{
    [Header("References")]
    public PlayerConfig config;
    
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerStates states;
    private float originalGravityScale;
    public MMF_Player jumpFeedbacks;

    private float currentTime = 0f;
    private float maxTime = 10f;
    private bool allowDoubleJump = false;

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
        HandleDoubleJumpPower();
    }

    public void InvokeDoubleJump()
    {
        allowDoubleJump = true;
        currentTime = 0f;
        Debug.Log("Double Jump Invoked!");
    }

    private void HandleDoubleJumpPower()
    {
        if (states.isGrounded) jumpCt = 0;

        if (currentTime < maxTime && allowDoubleJump)
        {
            currentTime += Time.deltaTime;
        }
        else
        {
            allowDoubleJump = false;
        }
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
        if (states.jumpInputBuffered && (states.isGrounded || states.canCoyoteJump || (allowDoubleJump && jumpCt < 1)))
        {
            // Cancel crouch, slide, dive, and roll states when jumping
            if (states.isCrouching || states.isSliding || states.isDiving || states.isRolling)
            {
                CancelCrouchSlideStates();
            }
            
            // Reset dive ability on double jump
            bool isDoubleJump = !states.isGrounded && !states.canCoyoteJump && allowDoubleJump && jumpCt < 1;
            if (isDoubleJump)
            {
                PlayerDive dive = GetComponent<PlayerDive>();
                if (dive != null)
                {
                    dive.OnDoubleJump();
                }
            }
            
            PerformJump();
        }
        else if (states.jumpInputBuffered && states.isWallRunning)
        {
            WallJump();
        }
    }

    int jumpCt = 0;

    private void PerformJump()
    {
        jumpCt++;
        bool shouldLongJump = ShouldPerformLongJump();
        
        if (shouldLongJump)
        {
            PerformLongJump();
        }
        else
        {
            PerformNormalJump();
        }
        
        states.StartJump();

        AudioManager.Instance?.Play(AudioManager.SoundType.Jump);
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

    private void PerformNormalJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        rb.AddForce(Vector2.up * config.minJumpForce, ForceMode2D.Impulse);
        
        GameLogger.LogMovement($"NORMAL JUMP! Vertical: {config.minJumpForce:F2}, Horizontal preserved: {rb.linearVelocity.x:F2}");
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
        float currentHorizontalVelocity = rb.linearVelocity.x;
        
        float verticalForce = config.minJumpForce * config.longJumpVerticalMultiplier;
        float horizontalDirection = Mathf.Sign(states.moveInput.x);
        float horizontalForce = config.longJumpHorizontalBoost * horizontalDirection;
        
        rb.linearVelocity = new Vector2(currentHorizontalVelocity, 0);
        rb.AddForce(Vector2.up * verticalForce, ForceMode2D.Impulse);
        rb.AddForce(Vector2.right * horizontalForce, ForceMode2D.Impulse);
        
        states.isLongJumping = true;
        states.crouchInputBuffered = false;
        states.longJumpGraceTimer = config.longJumpGracePeriod;
        
        if (states.isCrouching || states.isSliding)
        {
            CancelCrouchSlideStates();
        }
        
        GameLogger.LogMovement($"LONG JUMP! Vertical: {verticalForce:F2}, Horizontal boost: {horizontalForce:F2}, Base velocity: {currentHorizontalVelocity:F2}, Grace period: {config.longJumpGracePeriod:F2}s");
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
            bool inLongJumpGrace = states.longJumpGraceTimer > 0f;
            bool shouldFastFall = states.crouchHeld && !states.isGrounded && !inLongJumpGrace;
            rb.gravityScale = shouldFastFall ? config.fastFallGravityScale : config.fallGravityScale;
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

    public bool IsActive()
    {
        return allowDoubleJump;
    }

    public float GetRemainingTime()
    {
        return allowDoubleJump ? (maxTime - currentTime) : 0f;
    }

    public float GetMaxTime()
    {
        return maxTime;
    }

    public string GetPowerUpName()
    {
        return "DoubleJump";
    }
}

using MoreMountains.Feedbacks;
using System;
using System.Collections;
using UnityEngine;

public class PlayerJump : MonoBehaviour
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

    private float minJumpForce;
    private float maxJumpForce;

    private bool canSlowFall;

    private Coroutine activeHeightCoroutine;
    private Coroutine activeFloatCoroutine;
    
    [SerializeField] private PlayerMovement playerMovement;


    private void Awake()
    {
        originalGravityScale = rb.gravityScale;
        minJumpForce = config.minJumpForce;
        maxJumpForce = config.maxJumpForce;
        canSlowFall = false;
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

    public void InvokeDoubleJump(float duration)
    {
        allowDoubleJump = true;
        currentTime = 0f;
        maxTime = duration;
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

    public void InvokeExtraJumpHeight(float extraHeight, float duration)
    {
        if (activeHeightCoroutine != null)
        {
            StopCoroutine(activeHeightCoroutine);
            minJumpForce = config.minJumpForce;
            maxJumpForce = config.maxJumpForce;
        }
        activeHeightCoroutine = StartCoroutine(ExtraHeightCoroutine(extraHeight, duration));
    }

    private IEnumerator ExtraHeightCoroutine(float extraHeight, float duration)
    {
        minJumpForce = config.minJumpForce + extraHeight;
        maxJumpForce = config.maxJumpForce + extraHeight;
        yield return new WaitForSeconds(duration);
        minJumpForce = config.minJumpForce;
        maxJumpForce = config.maxJumpForce;
        activeHeightCoroutine = null;
    }

    public void InvokeAirFloat(float duration)
    {
        if (activeFloatCoroutine != null)
        {
            StopCoroutine(activeFloatCoroutine);
        }
        activeFloatCoroutine = StartCoroutine(AirFloatCoroutine(duration));
    }

    private IEnumerator AirFloatCoroutine(float duration)
    {
        canSlowFall = true;
        yield return new WaitForSeconds(duration);
        canSlowFall = false;
        activeFloatCoroutine = null;
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
        else if (states.jumpInputBuffered && states.isWallRunning && !states.isVaultingWall)
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
        // Desactivar slope snap
        if (playerMovement != null)
        {
            playerMovement.DisableSlopeSnapForJump();
        }
    
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        rb.AddForce(Vector2.up * minJumpForce, ForceMode2D.Impulse);

        GameLogger.LogMovement($"NORMAL JUMP! Vertical: {minJumpForce:F2}, Horizontal preserved: {rb.linearVelocity.x:F2}\")");
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

        float verticalForce = minJumpForce * config.longJumpVerticalMultiplier;
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
        // Force exit crouch, slide, and roll states when jumping
        states.isCrouching = false;
        states.isSliding = false;

        if (states.isRolling)
        {
            states.EndRoll();
        }

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
            float additionalForce = Mathf.Lerp(0f, maxJumpForce - minJumpForce, holdProgress);

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

        if (states.isVaultingWall)
        {
            return;
        }

        float velocityY = rb.linearVelocity.y;
        
        bool isOnValidSlope = states.isOnSlope && !states.isSlopeTooSteep;
        bool isActuallyFalling = velocityY < -0.01f && !isOnValidSlope && !states.isGrounded;
        states.isFalling = isActuallyFalling;
        states.isGraceFalling = velocityY < 1f && !isOnValidSlope && !states.isGrounded;

        bool isDoingAction = states.isJumping || states.isSliding || states.isRolling || states.isDiving || states.isKicking;
        bool shouldDisableGravity = states.isOnSlope && !states.isSlopeTooSteep && velocityY <= 0.01f && !isDoingAction;
        
        if (shouldDisableGravity)
        {
            bool isGentleSlope = states.slopeAngle < 20f;
            rb.gravityScale = isGentleSlope ? (originalGravityScale * 0.4f) : 0f;
        }
        else if (states.isWallRunning)
        {
            rb.gravityScale = config.wallRunGravityScale;
        }
        else if (states.isFalling || states.isJumpCut)
        {
            bool inLongJumpGrace = states.longJumpGraceTimer > 0f;
            bool shouldFastFall = states.crouchHeld && !states.isGrounded && !inLongJumpGrace;
            bool shouldSlowFall = states.jumpHeld && !states.isGrounded && states.isFalling && canSlowFall;
            rb.gravityScale = shouldSlowFall ? (config.fallGravityScale * config.slowFallGravityMultiplier) : (shouldFastFall ? config.fastFallGravityScale : config.fallGravityScale);
        }
        else if (velocityY >= 0f && velocityY < config.apexThreshold && !states.isGrounded)
        {
            rb.gravityScale = config.apexGravityScale;
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
        rb.AddForce(wallJumpDirection * (maxJumpForce * config.wallJumpMultiplier), ForceMode2D.Impulse);
        states.isWallRunning = false;
        states.StartJump();
    }
}

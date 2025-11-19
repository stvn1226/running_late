using System.Collections;
using UnityEngine;

public class PlayerStates : MonoBehaviour
{
    private static readonly int IsKicking = Animator.StringToHash("isKicking");
    private static readonly int IsSliding = Animator.StringToHash("isSliding");
    private static readonly int IsWallRunning = Animator.StringToHash("isWallRunning");
    private static readonly int IsCrouching = Animator.StringToHash("isCrouching");
    private static readonly int IsStaggered = Animator.StringToHash("isStaggered");

    [Header("Animator")]
    public Animator animator;

    [Header("Input States")]
    public Vector2 moveInput;
    public bool jumpPressed;
    public bool jumpHeld;
    public bool crouchHeld;
    public bool wallRunHeld;
    public bool kickPressed;
    //public bool dropDownPressed;
    public bool dropDownHeld;
    public bool airDodgePressed;

    [Header("Movement States")]
    public bool isGrounded;
    public bool wasGrounded;
    public bool isWallRunning;
    public bool isSliding;
    public bool isKicking;
    public bool isCrouching;
    public bool isDroppingDown;
    public bool isStaggered;
    public bool isAirDodging;
    public bool canAirDodge;

    [Header("Jump States")]
    public bool jumpInputBuffered = false;
    public float jumpBufferTimer = 0f;
    public float coyoteTimer = 0f;
    public bool canCoyoteJump = false;
    public bool isJumpCut = false;
    public bool isFalling = false;
    public float jumpHoldTimer = 0f;
    public bool isJumping = false;

    [Header("Wall Run States")]
    public bool canWallRun;
    public int wallDirection; // -1 left wall, 1 right wall

    [Header("Landing States")]
    public bool justLanded = false;
    public float landingGraceTimer = 0f;
    public float landingHorizontalVelocity = 0f;
    public float landingVerticalVelocity = 0f;

    [Header("Air Dodge States")]
    public float airDodgeCooldownTimer = 0f;
    public Vector2 airDodgeDirection;

    [Header("Kick States")]
    public float kickTimer;

    [Header("Stagger States")]
    public float staggerTimer;

    // Logging variables for wall run states
    private bool prevCanWallRun;
    private int prevWallDirection;
    private bool prevWallRunHeld;
    private bool prevIsWallRunning;
    private Vector2 prevMoveInput;
    private float lastWallRunLog = 0f;
    private const float WALL_RUN_LOG_INTERVAL = 0.2f;

    // Public properties for external access
    public bool IsWallRunHeld => wallRunHeld;

    // Events for state changes
    public System.Action OnGroundedChanged;
    public System.Action OnWallRunChanged;
    public System.Action OnJumpStateChanged;
    public System.Action OnStaggerStateChanged;

    private void Update()
    {
        // Log wall run state changes
        LogWallRunStates();

        // Update timers
        UpdateTimers();

        // Check for state changes and trigger events
        CheckStateChanges();

        UpdateAnimator();

        // Update previous values
        UpdatePreviousValues();
    }

    private void LogWallRunStates()
    {
        bool shouldLog = Time.time - lastWallRunLog > WALL_RUN_LOG_INTERVAL;

        // Log when wall run capability changes
        if (canWallRun != prevCanWallRun)
        {
            GameLogger.LogState($"WALL RUN CAPABILITY CHANGE: {prevCanWallRun} -> {canWallRun}, WallDirection: {wallDirection}, Input.x: {moveInput.x:F2}");
        }

        // Log when wall direction changes
        if (wallDirection != prevWallDirection)
        {
            GameLogger.LogState($"WALL DIRECTION CHANGE: {prevWallDirection} -> {wallDirection}, CanWallRun: {canWallRun}, Input.x: {moveInput.x:F2}");
        }

        // Log when wall run input changes
        if (wallRunHeld != prevWallRunHeld)
        {
            GameLogger.LogState($"WALL RUN INPUT CHANGE: {prevWallRunHeld} -> {wallRunHeld}, CanWallRun: {canWallRun}, IsWallRunning: {isWallRunning}");
        }

        // Log when wall running state changes
        if (isWallRunning != prevIsWallRunning)
        {
            GameLogger.LogState($"WALL RUNNING STATE CHANGE: {prevIsWallRunning} -> {isWallRunning}, WallRunHeld: {wallRunHeld}, CanWallRun: {canWallRun}, WallDirection: {wallDirection}");
        }

        // Log when move input changes significantly
        if (Vector2.Distance(moveInput, prevMoveInput) > 0.5f)
        {
            GameLogger.LogVerbose("STATE", $"MOVE INPUT CHANGE: {prevMoveInput} -> {moveInput}, CanWallRun: {canWallRun}, WallDirection: {wallDirection}");
        }

        // Periodic logging during wall running
        if (shouldLog && (canWallRun || isWallRunning))
        {
            GameLogger.LogVerbose("STATE", $"WALL RUN STATUS: CanWallRun={canWallRun}, IsWallRunning={isWallRunning}, WallDirection={wallDirection}, WallRunHeld={wallRunHeld}, Input.x={moveInput.x:F2}");
            lastWallRunLog = Time.time;
        }
    }

    private void UpdatePreviousValues()
    {
        prevCanWallRun = canWallRun;
        prevWallDirection = wallDirection;
        prevWallRunHeld = wallRunHeld;
        prevIsWallRunning = isWallRunning;
        prevMoveInput = moveInput;
    }

    private void UpdateAnimator()
    {
        animator.SetBool(IsSliding, isSliding);
        animator.SetBool(IsWallRunning, isWallRunning);
        animator.SetBool(IsCrouching, isCrouching);
        animator.SetBool(IsStaggered, isStaggered);

    }

    private void UpdateTimers()
    {
        // Jump buffer timer
        if (jumpInputBuffered)
        {
            jumpBufferTimer -= Time.deltaTime;
            if (jumpBufferTimer <= 0)
                jumpInputBuffered = false;
        }

        // Coyote timer
        if (canCoyoteJump)
        {
            coyoteTimer -= Time.deltaTime;
            if (coyoteTimer <= 0 || isGrounded)
                canCoyoteJump = false;
        }

        // Landing grace timer
        if (justLanded)
        {
            landingGraceTimer -= Time.deltaTime;
            if (landingGraceTimer <= 0f)
                justLanded = false;
        }

        if (isStaggered)
        {
            staggerTimer -= Time.deltaTime;
            if (staggerTimer <= 0)
            {
                isStaggered = false;

                if (isKicking)
                {
                    isKicking = false;
                    GameLogger.LogState("STAGGER END: Also clearing stuck kick state");
                }
            }
        }

        // Jump hold timer
        if (isJumping && jumpHeld)
        {
            jumpHoldTimer += Time.deltaTime;
        }

        // Reset pressed flags after one frame
        if (jumpPressed)
            jumpPressed = false;

        if (airDodgeCooldownTimer > 0f)
        {
            airDodgeCooldownTimer -= Time.deltaTime;
        }

        if (airDodgePressed)
        {
            airDodgePressed = false;
        }

    }

    private void CheckStateChanges()
    {
        bool previousGrounded = wasGrounded;
        bool previousWallRunning = isWallRunning;
        bool previousStaggered = isStaggered;

        // Update wasGrounded at the end of frame
        if (previousGrounded != isGrounded)
        {
            OnGroundedChanged?.Invoke();
        }

        if (previousWallRunning != isWallRunning)
        {
            OnWallRunChanged?.Invoke();
        }

        if (previousStaggered != isStaggered)
        {
            OnStaggerStateChanged?.Invoke();
        }

    }

    public void SetJumpPressed()
    {
        jumpPressed = true;
        jumpHeld = true;
        jumpInputBuffered = true;
    }

    public void SetJumpReleased()
    {
        jumpHeld = false;
        jumpPressed = false;
    }

    public void SetKickPressed()
    {
        if (!isKicking)
        {
            animator.SetTrigger(IsKicking);

            kickPressed = true;
            isKicking = true;
        }
    }

    public void EndKick()
    {
        if (isKicking)
        {
            isKicking = false;
            GameLogger.LogState("KICK: Ended kick state");
        }
    }

    public void SetDropDownPressed()
    {
        dropDownHeld = true;
        isDroppingDown = true;
    }

    public void SetDropDownReleased()
    {
        dropDownHeld = false;
        isDroppingDown = false;
    }

    public void SetAirDodgePressed()
    {
        airDodgePressed = true;
    }


    public void StartStagger(float duration)
    {
        isStaggered = true;
        staggerTimer = duration;

        if (isKicking)
        {
            isKicking = false;
            animator.ResetTrigger(IsKicking);
            GameLogger.LogState("STAGGER: Forcibly ended active kick to prevent movement lock");
        }
    }

    public IEnumerator StartSlowdown(float duration)
    {
        Time.timeScale = 0.5f;
        yield return new WaitForSeconds(duration);
        Time.timeScale = 1f;
    }

    public void StartCoyoteTime(float coyoteTime)
    {
        if (wasGrounded && !isGrounded && !isJumping)
        {
            canCoyoteJump = true;
            coyoteTimer = coyoteTime;
        }
    }

    public void ResetJumpState()
    {
        isJumping = false;
        isJumpCut = false;
        jumpHoldTimer = 0f;
        jumpPressed = false;
    }

    public void StartJump()
    {
        isJumping = true;
        isJumpCut = false;
        jumpHoldTimer = 0f;
        jumpInputBuffered = false;
        canCoyoteJump = false;
    }
}

using System.Collections;
using UnityEngine;

public class PlayerStates : MonoBehaviour
{
    private static readonly int IsKicking = Animator.StringToHash("isKicking");
    private static readonly int IsSliding = Animator.StringToHash("isSliding");
    private static readonly int IsWallRunning = Animator.StringToHash("isWallRunning");
    private static readonly int IsCrouching = Animator.StringToHash("isCrouching");
    private static readonly int IsStaggered = Animator.StringToHash("isStaggered");
    private static readonly int IsDiving = Animator.StringToHash("isDiving");
    private static readonly int IsRolling = Animator.StringToHash("isRolling");
    private static readonly int IsVaulting = Animator.StringToHash("isVaulting");

    [Header("Animator")]
    public Animator animator;

    [Header("Input States")]
    public Vector2 moveInput;
    public bool jumpPressed;
    public bool jumpHeld;
    public bool crouchHeld;
    public bool wallRunHeld;
    public bool kickPressed;
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

    [Header("Power-Up States")]
    public bool isInvincible = false;
    public bool isShielded = false;

    [Header("Jump States")]
    public bool jumpInputBuffered = false;
    public float jumpBufferTimer = 0f;
    public float coyoteTimer = 0f;
    public bool canCoyoteJump = false;
    public bool isJumpCut = false;
    public bool isFalling = false;
    public bool isGraceFalling = false;
    public float jumpHoldTimer = 0f;
    public bool isJumping = false;
    public bool crouchInputBuffered = false;
    public float crouchBufferTimer = 0f;
    public bool isLongJumping = false;
    public float longJumpGraceTimer = 0f;

    [Header("Wall Run States")]
    public bool canWallRun;
    public int wallDirection;

    [Header("Wall Climbing Advanced States")]
    public bool isPerfectClimb = false;
    public bool isVaultingWall = false;
    public float wallClimbInputTime = -999f;
    public float wallContactTime = -999f;
    public float storedMomentum = 0f;
    public float distanceToWallTop = 999f;
    public bool isNearWallTop = false;
    public float vaultTimer = 0f;

    [Header("Landing States")]
    public bool justLanded = false;
    public float landingGraceTimer = 0f;
    public float landingHorizontalVelocity = 0f;
    public float landingVerticalVelocity = 0f;

    [Header("Air Dodge States")]
    public float airDodgeCooldownTimer = 0f;
    public Vector2 airDodgeDirection;

    [Header("Dive States")]
    public bool isDiving = false;
    public bool isRolling = false;
    public bool canDive = true;
    public float diveTimer = 0f;
    public float rollTimer = 0f;
    public bool divePressed = false;

    [Header("Kick States")]
    public float kickTimer;

    [Header("Stagger States")]
    public float staggerTimer;

    private bool prevCanWallRun;
    private int prevWallDirection;
    private bool prevWallRunHeld;
    private bool prevIsWallRunning;
    private Vector2 prevMoveInput;
    private float lastWallRunLog = 0f;
    private const float WALL_RUN_LOG_INTERVAL = 0.2f;

    private Coroutine activeInvincibility;

    public bool IsWallRunHeld => wallRunHeld;

    public System.Action OnGroundedChanged;
    public System.Action OnWallRunChanged;
    public System.Action OnJumpStateChanged;
    public System.Action OnStaggerStateChanged;

    private void Update()
    {
        LogWallRunStates();
        UpdateTimers();
        CheckStateChanges();
        UpdateAnimator();
        UpdatePreviousValues();
    }

    public void InvokeInvincibility(float duration)
    {
        if (activeInvincibility != null)
        {
            StopCoroutine(activeInvincibility);
        }
        activeInvincibility = StartCoroutine(InvincibilityCoroutine(duration));
    }

    private IEnumerator InvincibilityCoroutine(float duration)
    {
        isInvincible = true;
        yield return new WaitForSeconds(duration);
        isInvincible = false;
        activeInvincibility = null;
    }

    public void InvokeShield()
    {
        isShielded = true;
    }

    public void RemoveShield()
    {
        isShielded = false;
        UIPowerUp.RemoveShield();
    }

    private void LogWallRunStates()
    {
        bool shouldLog = Time.time - lastWallRunLog > WALL_RUN_LOG_INTERVAL;

        if (canWallRun != prevCanWallRun)
        {
            GameLogger.LogState($"WALL RUN CAPABILITY CHANGE: {prevCanWallRun} -> {canWallRun}, WallDirection: {wallDirection}, Input.x: {moveInput.x:F2}");
        }

        if (wallDirection != prevWallDirection)
        {
            GameLogger.LogState($"WALL DIRECTION CHANGE: {prevWallDirection} -> {wallDirection}, CanWallRun: {canWallRun}, Input.x: {moveInput.x:F2}");
        }

        if (wallRunHeld != prevWallRunHeld)
        {
            GameLogger.LogState($"WALL RUN INPUT CHANGE: {prevWallRunHeld} -> {wallRunHeld}, CanWallRun: {canWallRun}, IsWallRunning: {isWallRunning}");
        }

        if (isWallRunning != prevIsWallRunning)
        {
            GameLogger.LogState($"WALL RUNNING STATE CHANGE: {prevIsWallRunning} -> {isWallRunning}, WallRunHeld: {wallRunHeld}, CanWallRun: {canWallRun}, WallDirection: {wallDirection}");
        }

        if (Vector2.Distance(moveInput, prevMoveInput) > 0.5f)
        {
            GameLogger.LogVerbose("STATE", $"MOVE INPUT CHANGE: {prevMoveInput} -> {moveInput}, CanWallRun: {canWallRun}, WallDirection: {wallDirection}");
        }

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
        animator.SetBool(IsDiving, isDiving);
        animator.SetBool(IsRolling, isRolling);
        animator.SetBool(IsVaulting, isVaultingWall);
    }

    private void UpdateTimers()
    {
        if (jumpInputBuffered)
        {
            jumpBufferTimer -= Time.deltaTime;
            if (jumpBufferTimer <= 0)
                jumpInputBuffered = false;
        }

        if (canCoyoteJump)
        {
            coyoteTimer -= Time.deltaTime;
            if (coyoteTimer <= 0 || isGrounded)
                canCoyoteJump = false;
        }

        // Crouch buffer timer
        if (crouchInputBuffered)
        {
            crouchBufferTimer -= Time.deltaTime;
            if (crouchBufferTimer <= 0)
                crouchInputBuffered = false;
        }

        // Long jump grace timer
        if (longJumpGraceTimer > 0f)
        {
            longJumpGraceTimer -= Time.deltaTime;
        }

        // Landing grace timer
        if (justLanded)
        {
            landingGraceTimer -= Time.deltaTime;
            if (landingGraceTimer <= 0f)
                justLanded = false;
        }

        if (isKicking)
        {
            kickTimer -= Time.deltaTime;
            if (kickTimer <= 0f)
            {
                GameLogger.LogState($"KICK: Timer expired ({kickTimer:F3}s), forcing end");
                EndKick();
            }
        }

        if (isStaggered)
        {
            staggerTimer -= Time.deltaTime;
            if (staggerTimer <= 0)
            {
                isStaggered = false;

                if (isKicking)
                {
                    GameLogger.LogWarning("KICK", "STAGGER END: Force clearing stuck kick state");
                    EndKick();
                }
            }
        }

        if (isJumping && jumpHeld)
        {
            jumpHoldTimer += Time.deltaTime;
        }

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

        if (isDiving)
        {
            diveTimer -= Time.deltaTime;
        }

        if (isRolling)
        {
            rollTimer -= Time.deltaTime;
            if (rollTimer <= 0f)
            {
                EndRoll();
            }
        }

        if (divePressed)
        {
            divePressed = false;
        }

        if (isVaultingWall)
        {
            vaultTimer -= Time.deltaTime;
            if (vaultTimer <= 0f)
            {
                EndVault();
            }
        }

    }

    private void CheckStateChanges()
    {
        bool previousGrounded = wasGrounded;
        bool previousWallRunning = isWallRunning;
        bool previousStaggered = isStaggered;

        if (previousGrounded != isGrounded)
        {
            OnGroundedChanged?.Invoke();
        }

        if (previousWallRunning != isWallRunning)
        {
            OnWallRunChanged?.Invoke();

            if (isWallRunning && isKicking)
            {
                GameLogger.LogWarning("KICK", "CRITICAL FIX: Wall run started during kick! Forcing kick end.");
                EndKick();
            }
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

    public void SetKickPressed(float kickDuration)
    {
        if (!isKicking)
        {
            if (isRolling)
            {
                EndRoll();
            }
            
            animator.SetTrigger(IsKicking);
            kickPressed = true;
            isKicking = true;
            kickTimer = kickDuration;

            GameLogger.LogState($"KICK: Started - Timer set to {kickTimer:F2}s");
        }
        else
        {
            GameLogger.LogWarning("KICK", "Kick already active, ignoring new kick request");
        }
    }

    public void EndKick()
    {
        if (isKicking)
        {
            isKicking = false;
            kickTimer = 0f;
            animator.ResetTrigger(IsKicking);
            GameLogger.LogState("KICK: Ended");
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

    public void SetDivePressed()
    {
        divePressed = true;
    }

    public void StartDive(float duration)
    {
        isDiving = true;
        diveTimer = duration;
        GameLogger.LogState($"DIVE: Started - Timer set to {diveTimer:F2}s");
    }

    public void EndDive()
    {
        if (isDiving)
        {
            isDiving = false;
            diveTimer = 0f;
            GameLogger.LogState("DIVE: Ended");
        }
    }

    public void StartRoll(float duration)
    {
        isRolling = true;
        rollTimer = duration;
        GameLogger.LogState($"ROLL: Started - Timer set to {rollTimer:F2}s");
    }

    public void EndRoll()
    {
        if (isRolling)
        {
            isRolling = false;
            rollTimer = 0f;
            isCrouching = false;
            isSliding = false;
            
            PlayerPhysics playerPhysics = GetComponent<PlayerPhysics>();
            if (playerPhysics != null)
            {
                playerPhysics.UpdateColliderSize();
            }
            
            GameLogger.LogState("ROLL: Ended - cleared crouch and slide states, reset collider");
        }
    }

    public void ResetDive()
    {
        canDive = true;
        GameLogger.LogState("DIVE: Reset - can dive again");
    }


    public void StartStagger(float duration)
    {
        isStaggered = true;
        staggerTimer = duration;

        if (isKicking)
        {
            GameLogger.LogWarning("KICK", "STAGGER: Force ending active kick to prevent movement lock");
            EndKick();
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

    public void BufferCrouchInput(float bufferTime)
    {
        crouchInputBuffered = true;
        crouchBufferTimer = bufferTime;
    }

    public void StartVault(float duration)
    {
        isVaultingWall = true;
        vaultTimer = duration;
        isWallRunning = false;
        canWallRun = false;
        isPerfectClimb = false;
    }

    public void EndVault()
    {
        if (isVaultingWall)
        {
            isVaultingWall = false;
            vaultTimer = 0f;
        }
    }

    public void RecordWallClimbInput()
    {
        wallClimbInputTime = Time.time;
    }

    public void RecordWallContact()
    {
        wallContactTime = Time.time;
    }
}

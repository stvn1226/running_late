using System.Collections;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class PlayerStates : MonoBehaviour
{
    private PlayerConfig config;
    [SerializeField] private CapsuleCollider2D playerCollider;
    private new Transform transform;
    private SpriteRenderer spriteRenderer;

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

    [Header("Slope States")]
    public bool isOnSlope = false;
    public float slopeAngle = 0f;
    public Vector2 slopeNormal = Vector2.up;
    public bool isSlopeTooSteep = false;
    public Vector2 slopeTangent = Vector2.right;
    public float slopeBuffer = 0f;

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
    public float kickStartTime;

    [Header("Stagger States")]
    public float staggerTimer;

    [Header("Corner Climb States")]
    public bool isCornerClimbing = false;
    public float cornerClimbTimer = 0f;
    
    private bool prevCanWallRun;
    private int prevWallDirection;
    private bool prevWallRunHeld;
    private bool prevIsWallRunning;
    private Vector2 prevMoveInput;
    private float lastWallRunLog = 0f;
    private const float WALL_RUN_LOG_INTERVAL = 0.2f;

    [Header("Cable Sliding")]
    public bool isSlidingOnCable = false;
    public EdgeCollider2D currentCable;
    public float cableSlideSpeed = 0f;

    private Coroutine activeInvincibility;

    public bool IsWallRunHeld => wallRunHeld;

    public System.Action OnGroundedChanged;
    public System.Action OnWallRunChanged;
    public System.Action OnJumpStateChanged;
    public System.Action OnStaggerStateChanged;
    public System.Action OnLanded;

    private void OnDisable()
    {
        if (activeInvincibility != null)
        {
            StopCoroutine(activeInvincibility);
            activeInvincibility = null;
        }
    }

    private void Awake()
    {
        transform = GetComponent<Transform>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        config = GetComponent<PlayerMovement>()?.config;
    }

    private void Update()
    {
        LogWallRunStates();
        UpdateTimers();
        CheckStateChanges();
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
        GameLogger.Log($"[PLAYER INVULNERABILITY] ENABLED for {duration:F2}s - Can stomp but cannot take damage");
        
        yield return new WaitForSeconds(duration);
        
        isInvincible = false;
        GameLogger.Log("[PLAYER INVULNERABILITY] DISABLED - Player is vulnerable again");
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

        if (isWallRunning && isKicking)
        {
            float timeSinceKick = Time.time - kickStartTime;
            if (timeSinceKick < config.kickInterruptLock)
            {
                if (config.kickDebug) GameLogger.Log("KICK", $"INTERRUPTION PREVENTED: Still in lock gate ({timeSinceKick:F3}s < {config.kickInterruptLock}s)");
                return;
            }
            
            if (config.kickDebug) GameLogger.Log("KICK", "INTERRUPTED: Wall run started after lock gate.");
            EndKick();
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
        
        // Corner climb timer
        if (cornerClimbTimer > 0f)
        {
            cornerClimbTimer -= Time.deltaTime;
            if (cornerClimbTimer <= 0f)
            {
                isCornerClimbing = false;
            }
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

        if (isKicking)
        {
            kickTimer -= Time.deltaTime;
            if (kickTimer <= 0f)
            {
                EndKick();
            }
        }

        if (divePressed)
        {
            divePressed = false;
        }

        // Vault timer with early exit detection
        if (isVaultingWall)
        {
            vaultTimer -= Time.deltaTime;
            
            // End vault early if player leaves wall contact during vault
            if (!IsStillTouchingWall(wallDirection))
            {
                EndVault();
            }
            else if (vaultTimer <= 0f)
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
            
            kickPressed = true;
            isKicking = true;
            kickTimer = kickDuration;
            kickStartTime = Time.time;

            if (config.kickDebug) GameLogger.Log("KICK", $"STATE: isKicking SET TO TRUE - Duration={kickDuration:F2}s, isGrounded={isGrounded}");
        }
    }

    public void EndKick(string reason = "finished")
    {
        if (isKicking)
        {
            isKicking = false;
            kickTimer = 0f;
            if (config.kickDebug) GameLogger.Log("KICK", $"STATE: isKicking SET TO FALSE - Reason: {reason}");
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

        float invulnerabilityDuration = duration * 1.5f;
        InvokeInvincibility(invulnerabilityDuration);
        StartCoroutine(BlinkSprite(invulnerabilityDuration));
        
        GameLogger.Log($"[PLAYER STAGGER] Started - Stagger: {duration:F2}s, Invulnerability: {invulnerabilityDuration:F2}s");
    }

    public IEnumerator BlinkSprite(float duration)
    {
        if (spriteRenderer == null)
        {
            GameLogger.LogWarning("PLAYER", "SpriteRenderer is null, cannot blink sprite");
            yield break;
        }

        float elapsed = 0f;
        float blinkInterval = 0.1f;
        bool isVisible = true;
        Color originalColor = spriteRenderer.color;

        while (elapsed < duration)
        {
            isVisible = !isVisible;
            float alpha = isVisible ? 1f : 0.1f;
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            
            yield return new WaitForSeconds(blinkInterval);
            elapsed += blinkInterval;
        }

        spriteRenderer.color = originalColor;
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

    public void StartCableSlide(EdgeCollider2D cableCollider)
    {
        isSlidingOnCable = true;
        currentCable = cableCollider;
        isSliding = true;
    }

    public void EndCableSlide()
    {
        isSlidingOnCable = false;
        currentCable = null;
        isSliding = false;
    }

    public void RecordWallClimbInput()
    {
        wallClimbInputTime = Time.time;
    }

    public void RecordWallContact()
    {
        wallContactTime = Time.time;
    }

    /// <summary>
    /// Checks if player is still touching a wall in the specified direction.
    /// Used for vault early-exit detection - ends vault immediately when player leaves wall.
    /// </summary>
    private bool IsStillTouchingWall(int direction)
    {
        if (config == null || playerCollider == null) return false;

        Vector2 boxCenter = (Vector2)transform.position + playerCollider.offset;
        Vector2 boxSize = new Vector2(config.wallCheckDistance, playerCollider.size.y);
        Vector2 checkDirection = direction > 0 ? Vector2.right : Vector2.left;

        RaycastHit2D hit = Physics2D.BoxCast(boxCenter, boxSize, 0f, checkDirection, config.wallCheckDistance, config.wallLayer);
        return hit.collider != null;
    }
}

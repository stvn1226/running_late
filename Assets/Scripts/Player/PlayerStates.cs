using UnityEngine;
using UnityEngine.Rendering;

public class PlayerStates : MonoBehaviour
{
    [Header("Input States")]
    public Vector2 moveInput;
    public bool jumpPressed;
    public bool jumpHeld;
    public bool crouchHeld;
    public bool wallRunHeld;
    public bool kickPressed;

    [Header("Movement States")]
    public bool isGrounded;
    public bool wasGrounded;
    public bool isWallRunning;
    public bool isSliding;
    public bool isKicking;
    public bool isCrouching;

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

    [Header("Kick States")]
    public float kickTimer;

    // Public properties for external access
    public bool IsWallRunHeld => wallRunHeld;

    // Events for state changes
    public System.Action OnGroundedChanged;
    public System.Action OnWallRunChanged;
    public System.Action OnJumpStateChanged;

    private void Update()
    {
        // Update timers
        UpdateTimers();
        
        // Check for state changes and trigger events
        CheckStateChanges();
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

        // Kick timer
        if (isKicking)
        {
            kickTimer -= Time.deltaTime;
            if (kickTimer <= 0)
                isKicking = false;
        }

        // Jump hold timer
        if (isJumping && jumpHeld)
        {
            jumpHoldTimer += Time.deltaTime;
        }
    }

    private void CheckStateChanges()
    {
        bool previousGrounded = wasGrounded;
        bool previousWallRunning = isWallRunning;

        // Update wasGrounded at the end of frame
        if (previousGrounded != isGrounded)
        {
            OnGroundedChanged?.Invoke();
        }

        if (previousWallRunning != isWallRunning)
        {
            OnWallRunChanged?.Invoke();
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
            kickPressed = true;
            isKicking = true;
        }
    }

    public void SetDropDownPressed()
    {
        
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
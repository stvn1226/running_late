using UnityEngine;
using UnityEngine.Rendering;
using static UnityEditor.VersionControl.Asset;

public class PlayerStates : MonoBehaviour
{
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

    [Header("Movement States")]
    public bool isGrounded;
    public bool wasGrounded;
    public bool isWallRunning;
    public bool isSliding;
    public bool isKicking;
    public bool isCrouching;
    public bool isDroppingDown;
    public bool isStaggered;

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

    [Header("Stagger States")]
    public float staggerTimer;


    // Public properties for external access
    public bool IsWallRunHeld => wallRunHeld;

    // Events for state changes
    public System.Action OnGroundedChanged;
    public System.Action OnWallRunChanged;
    public System.Action OnJumpStateChanged;
    public System.Action OnStaggerStateChanged;

    private void Update()
    {
        // Update timers
        UpdateTimers();
        
        // Check for state changes and trigger events
        CheckStateChanges();
        
        UpdateAnimator();
    }

    private void UpdateAnimator()
    {
        animator.SetBool("isSliding", isSliding);
        animator.SetBool("isWallRunning", isWallRunning);
        animator.SetBool("isCrouching", isCrouching);
        animator.SetBool("isStaggered", isStaggered);

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
            if (kickPressed) kickPressed = false;
            kickTimer -= Time.deltaTime;
            if (kickTimer <= 0)
                isKicking = false;
        }

        if (isStaggered)
        {
            staggerTimer -= Time.deltaTime;
            if (staggerTimer <= 0)
                isStaggered = false;
        }

        // Jump hold timer
        if (isJumping && jumpHeld)
        {
            jumpHoldTimer += Time.deltaTime;
        }
        
        // Reset pressed flags after one frame
        if (jumpPressed)
            jumpPressed = false;
        //if (kickPressed)
        //    kickPressed = false;
        //if (dropDownPressed)
        //    dropDownPressed = false;
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
            animator.SetTrigger("isKicking");

            kickPressed = true;
            isKicking = true;
        }
    }

    public void SetDropDownPressed()
    {
        //dropDownPressed = true;
        dropDownHeld = true;
        isDroppingDown = true;
    }

    public void SetDropDownReleased()
    {
        //dropDownPressed = false;
        dropDownHeld = false;
        isDroppingDown = false;
    }

    public void StartStagger(float duration)
    {
        isStaggered = true;
        staggerTimer = duration;
        //animator.SetTrigger("isStaggered");
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
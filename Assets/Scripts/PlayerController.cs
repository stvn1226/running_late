using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Forces")]
    public float runForce = 3000f;
    public float maxRunSpeed = 12f;
    public float jumpForce = 18f;
    public float airControlMultiplier = 0.6f;
    public float wallRunForce = 2000f;
    public float slideForce = 4000f;

    [Header("Jump Settings")]
    public float minJumpForce = 12f;      // Short tap jump height
    public float maxJumpForce = 18f;      // Full hold jump height  
    public float jumpHoldTime = 0.3f;     // Time to reach max jump
    public float fallGravityScale = 3f;
    public float fastFallGravityScale = 5f;
    public float jumpBufferTime = 0.12f;
    public float coyoteTime = 0.1f;

    [Header("Physics Tuning")]
    public float groundDrag = 50f;        // Ground friction resistance
    public float airDrag = 50f;           // Air resistance (similar to ground for realistic feel)
    public float slideDrag = 25f;         // Reduced drag while sliding for momentum
    public float wallRunGravityScale = 0.3f;

    [Header("Physics Settings")]
    public float groundCheckDistance = 0.1f;
    public float wallCheckDistance = 0.5f;
    public LayerMask groundLayer = 1;
    public LayerMask wallLayer = 1;

    [Header("Kick Settings")]
    public float kickDuration = 0.3f;
    public float kickForceReduction = 0.5f;

    // Components
    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;
    private PlayerInputActions inputActions;

    // Input
    private Vector2 moveInput;
    private bool jumpPressed;
    private bool jumpHeld;
    private bool crouchHeld;
    private bool wallRunHeld;
    private bool kickPressed;

    // States
    private bool isGrounded;
    private bool wasGrounded;
    private bool isWallRunning;
    private bool isSliding;
    private bool isKicking;
    private bool isCrouching;
    
    // Landing momentum preservation
    private bool justLanded = false;
    private float landingGraceTimer = 0f;
    private const float landingGracePeriod = 0.25f; // Quarter second grace period
    private float landingHorizontalVelocity = 0f;
    
    // Slam landing momentum conversion
    private float landingVerticalVelocity = 0f;
    private const float slamLandingThreshold = -8f; // Minimum downward speed for slam landing
    private const float momentumConversionRatio = 0.2f; // 60% of downward speed converts to horizontal

    // Jump mechanics
    private bool jumpInputBuffered = false;
    private float jumpBufferTimer = 0f;
    private float coyoteTimer = 0f;
    private bool canCoyoteJump = false;
    private bool isJumpCut = false;
    private bool isFalling = false;
    private float jumpHoldTimer = 0f;
    private bool isJumping = false;

    // Wall running
    private bool canWallRun;
    private int wallDirection; // -1 left wall, 1 right wall

    // Physics state tracking
    private float originalGravityScale;

    // Collider size management
    private Vector2 originalColliderSize;
    private Vector2 originalColliderOffset;
    private Vector2 crouchColliderSize;
    private Vector2 crouchColliderOffset;

    // Kick timer
    private float kickTimer;

    private void Awake()
    {
        // Use GetComponentInChildren as per your existing setup
        rb = GetComponentInChildren<Rigidbody2D>();
        boxCollider = GetComponentInChildren<BoxCollider2D>();
        inputActions = new PlayerInputActions();

        // Store original physics settings
        originalGravityScale = rb.gravityScale;
        
        // Store original collider settings
        originalColliderSize = boxCollider.size;
        originalColliderOffset = boxCollider.offset;

        // Calculate crouch collider settings - expands X while reducing Y as per your implementation
        crouchColliderSize = new Vector2(originalColliderSize.x * 1.33f, originalColliderSize.y * 0.33f);
        crouchColliderOffset = new Vector2(originalColliderOffset.x, originalColliderOffset.y - (originalColliderSize.y - crouchColliderSize.y) * 0.5f);
    }

    private void Start()
    {
        // Set physics settings for optimal momentum preservation
        rb.linearDamping = 0f;
        rb.linearDamping = 0f;
        
        Debug.Log($"PlayerController: Physics settings initialized - Drag: {rb.linearDamping}, LinearDamping: {rb.linearDamping}");
    }

    private void OnEnable()
    {
        inputActions.Gameplay.Enable();

        // Subscribe to input events - using your existing input action names
        inputActions.Gameplay.Run.performed += OnMove;
        inputActions.Gameplay.Run.canceled += OnMove;
        inputActions.Gameplay.Jump.performed += OnJumpStart;
        inputActions.Gameplay.Jump.canceled += OnJumpEnd;
        inputActions.Gameplay.Crouch.performed += OnCrouchStart;
        inputActions.Gameplay.Crouch.canceled += OnCrouchEnd;
        inputActions.Gameplay.WallRun.performed += OnWallRunStart;
        inputActions.Gameplay.WallRun.canceled += OnWallRunEnd;
        inputActions.Gameplay.Kick.performed += OnKick;
    }

    private void OnDisable()
    {
        inputActions.Gameplay.Disable();

        // Unsubscribe from input events
        inputActions.Gameplay.Run.performed -= OnMove;
        inputActions.Gameplay.Run.canceled -= OnMove;
        inputActions.Gameplay.Jump.performed -= OnJumpStart;
        inputActions.Gameplay.Jump.canceled -= OnJumpEnd;
        inputActions.Gameplay.Crouch.performed -= OnCrouchStart;
        inputActions.Gameplay.Crouch.canceled -= OnCrouchEnd;
        inputActions.Gameplay.WallRun.performed -= OnWallRunStart;
        inputActions.Gameplay.WallRun.canceled -= OnWallRunEnd;
        inputActions.Gameplay.Kick.performed -= OnKick;
    }

    private void Update()
    {
        CheckGrounded();
        CheckWallRun();
        HandleJumpInput();
        HandleJumpCut();
        HandleStates();
        UpdateKickTimer();
        UpdateJumpPhysics();
                
        // Store the last air velocity when we're in the air (for momentum preservation)
        if (!isGrounded && Mathf.Abs(rb.linearVelocity.x) > 1f)
        {
            landingHorizontalVelocity = rb.linearVelocity.x;
        }
        
        // Detect landing and handle momentum conversion
        if (wasGrounded != isGrounded && isGrounded)
        {
            // Capture the landing velocities
            landingVerticalVelocity = rb.linearVelocity.y;
            float currentHorizontalVel = rb.linearVelocity.x;
            
            // Check for slam landing (fast downward velocity)
            bool isSlamLanding = landingVerticalVelocity <= slamLandingThreshold;
            
            // Check if player is inputting horizontal movement on landing
            bool hasHorizontalInput = Mathf.Abs(moveInput.x) > 0.1f;
            
            // Check if player is still holding crouch button during landing
            bool isHoldingCrouchButton = crouchHeld;
            
            Debug.Log($"Landing detected - VerticalSpeed: {landingVerticalVelocity:F2}, StillHoldingCrouch: {isHoldingCrouchButton}, HasInput: {hasHorizontalInput}, IsSlamLanding: {isSlamLanding}");
            
            // SLAM LANDING: Player was holding crouch button when airborne AND still holding it on landing
            if (isSlamLanding && hasHorizontalInput && isHoldingCrouchButton)
            {
                // Convert downward momentum to horizontal momentum
                float downwardSpeed = Mathf.Abs(landingVerticalVelocity);
                float horizontalBoost = downwardSpeed * momentumConversionRatio;
                
                // Apply horizontal boost in the direction of input
                float inputDirection = Mathf.Sign(moveInput.x);
                float newHorizontalVelocity = currentHorizontalVel + (horizontalBoost * inputDirection);
                
                // Apply the slam landing momentum conversion
                rb.linearVelocity = new Vector2(newHorizontalVelocity, 0f); // Reset vertical to prevent bouncing
                
                // Set the target velocity for preservation system
                landingHorizontalVelocity = newHorizontalVelocity;
                
                Debug.Log($"SLAM LANDING! Downward speed: {downwardSpeed:F2} -> Horizontal boost: {horizontalBoost:F2} -> New velocity: {newHorizontalVelocity:F2}");
                
            }
            else
            {                
                if (isSlamLanding && hasHorizontalInput && !isHoldingCrouchButton)
                {
                    Debug.Log($"Fast landing detected, but crouch button conditions not met - no momentum conversion");
                }
                else
                {
                    Debug.Log($"Normal landing. Preserving horizontal velocity: {landingHorizontalVelocity:F2} (current: {currentHorizontalVel:F2})");
                }
                
            }
            
            // Start momentum preservation grace period
            justLanded = true;
            landingGraceTimer = landingGracePeriod;
        }
        
        // Update landing grace timer
        if (justLanded)
        {
            landingGraceTimer -= Time.deltaTime;
            if (landingGraceTimer <= 0f)
            {
                justLanded = false;
                Debug.Log("Landing grace period ended");
            }
        }
        
        // Update wasGrounded for next frame
        wasGrounded = isGrounded;
    }

    private void FixedUpdate()
    {
        HandleMovement();
        ApplyDrag();
    }

    #region Input Callbacks
    private void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnJumpStart(InputAction.CallbackContext context)
    {
        jumpPressed = true;
        jumpHeld = true;
    }

    private void OnJumpEnd(InputAction.CallbackContext context)
    {
        jumpHeld = false;
    }

    private void OnCrouchStart(InputAction.CallbackContext context)
    {
        crouchHeld = true;
    }

    private void OnCrouchEnd(InputAction.CallbackContext context)
    {
        crouchHeld = false;
    }

    private void OnWallRunStart(InputAction.CallbackContext context)
    {
        wallRunHeld = true;
    }

    private void OnWallRunEnd(InputAction.CallbackContext context)
    {
        wallRunHeld = false;
    }

    private void OnKick(InputAction.CallbackContext context)
    {
        if (!isKicking)
        {
            StartKick();
        }
    }
    #endregion

    #region Physics-Based Movement
    private void HandleMovement()
    {
        float forceMultiplier = isKicking ? kickForceReduction : 1f;

        if (isWallRunning)
        {
            HandleWallRunMovement(forceMultiplier);
        }
        else if (isSliding)
        {
            HandleSlideMovement(forceMultiplier);
        }
        else
        {
            HandleNormalMovement(forceMultiplier);
        }
    }

    private void HandleNormalMovement(float forceMultiplier)
    {
        // Prevent movement input during kick mode
        if (isKicking)
        {
            return;
        }

        float horizontalInput = moveInput.x;

        if (Mathf.Abs(horizontalInput) > 0.1f)
        {
            // Calculate desired velocity and force needed
            float targetSpeed = horizontalInput * maxRunSpeed;
            float speedDifference = targetSpeed - rb.linearVelocity.x;

            // Apply different force based on ground/air state
            float force = isGrounded ? runForce : runForce * airControlMultiplier;
            force *= forceMultiplier;

            // Apply force proportional to speed difference for more responsive control
            float appliedForce = speedDifference * force;

            // Clamp force to prevent overpowered acceleration
            appliedForce = Mathf.Clamp(appliedForce, -force, force);

            rb.AddForce(Vector2.right * appliedForce * Time.fixedDeltaTime, ForceMode2D.Force);
        }

        // Fast fall when crouching in air
        if (crouchHeld && !isGrounded)
        {
            rb.AddForce(Vector2.down * runForce * Time.fixedDeltaTime, ForceMode2D.Force);
        }
    }

    private void HandleSlideMovement(float forceMultiplier)
    {
        // During slide mode, player cannot control horizontal movement - sliding uses momentum only
        // No movement input is processed during sliding
    }

    private void HandleWallRunMovement(float forceMultiplier)
    {
        // Prevent movement input during kick mode
        if (isKicking)
        {
            return;
        }

        // Apply upward force for wall running
        rb.AddForce(Vector2.up * wallRunForce * forceMultiplier * Time.fixedDeltaTime, ForceMode2D.Force);

        // Allow some horizontal movement while wall running
        float horizontalInput = moveInput.x;
        if (Mathf.Abs(horizontalInput) > 0.1f)
        {
            float horizontalForce = horizontalInput * runForce * 0.5f * forceMultiplier;
            rb.AddForce(Vector2.right * horizontalForce * Time.fixedDeltaTime, ForceMode2D.Force);
        }
    }

    private void ApplyDrag()
    {
        // Never apply drag when in air, sliding, or kicking
        if (!isGrounded || isSliding || isKicking)
        {
            return;
        }
        
        Vector2 velocity = rb.linearVelocity;
        float horizontalInput = moveInput.x;
        
        // CRITICAL: Preserve horizontal momentum during landing grace period
        if (justLanded)
        {
            // During landing grace period, actively maintain the landing velocity
            float currentHorizontalVelocity = velocity.x;
            float velocityDifference = landingHorizontalVelocity - currentHorizontalVelocity;
            
            // Only preserve momentum if there's a significant difference (momentum loss)
            if (Mathf.Abs(velocityDifference) > 0.5f)
            {
                // Apply force to restore the original landing velocity
                float preservationForce = velocityDifference * 50f; // Strong correction force
                rb.AddForce(Vector2.right * preservationForce, ForceMode2D.Force);
                
                Debug.Log($"Preserving momentum: current={currentHorizontalVelocity:F2}, target={landingHorizontalVelocity:F2}, force={preservationForce:F2}");
            }
            
            // Don't apply normal drag during grace period
            return;
        }
        
        // Normal drag system after grace period
        float targetDrag = 0f;
        
        if (Mathf.Abs(horizontalInput) < 0.1f)
        {
            // No input - apply ground drag to stop naturally
            targetDrag = groundDrag;
        }
        else
        {
            // Check if input opposes current movement for quick direction changes
            bool inputOpposesMovement = (horizontalInput * velocity.x) < 0;
            if (inputOpposesMovement)
            {
                // Apply extra drag for quick turns
                targetDrag = groundDrag * 1.5f;
            }
            else
            {
                // Moving with input - minimal drag
                targetDrag = groundDrag * 0.1f;
            }
        }
        
        // Apply drag as a force
        Vector2 dragForce = -velocity * targetDrag * Time.fixedDeltaTime;
        rb.AddForce(new Vector2(dragForce.x, 0), ForceMode2D.Force);
    }

    private void UpdatePhysicsSettings()
    {
        // This method is now integrated into UpdateJumpPhysics for better jump control
        // Left here for compatibility - UpdateJumpPhysics handles all physics now
    }
    #endregion

    #region Enhanced Jump System
    private void HandleJumpInput()
    {
        // Prevent jump input during kick mode
        if (isKicking)
        {
            return;
        }

        // Handle jump buffering
        if (jumpPressed)
        {
            jumpInputBuffered = true;
            jumpBufferTimer = jumpBufferTime;
            jumpPressed = false; // Reset for next frame
        }
        
        if (jumpInputBuffered)
        {
            jumpBufferTimer -= Time.deltaTime;
            if (jumpBufferTimer <= 0)
            {
                jumpInputBuffered = false;
            }
        }
        
        // Handle coyote time
        if (wasGrounded && !isGrounded && !isJumping)
        {
            canCoyoteJump = true;
            coyoteTimer = coyoteTime;
        }
        
        if (canCoyoteJump)
        {
            coyoteTimer -= Time.deltaTime;
            if (coyoteTimer <= 0 || isGrounded)
            {
                canCoyoteJump = false;
            }
        }
        
        // Execute jump
        if (jumpInputBuffered && (isGrounded || canCoyoteJump) && !isSliding)
        {
            PerformJump();
            jumpInputBuffered = false;
            canCoyoteJump = false;
        }
        else if (jumpInputBuffered && isWallRunning)
        {
            WallJump();
            jumpInputBuffered = false;
        }
        
        // Handle variable jump height - track how long jump is held
        if (isJumping && jumpHeld)
        {
            jumpHoldTimer += Time.deltaTime;
        }
    }

    private void PerformJump()
    {
        // Reset vertical velocity for consistent jump height
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        
        // Apply minimum jump force initially
        rb.AddForce(Vector2.up * minJumpForce, ForceMode2D.Impulse);
        
        // Reset jump state
        isJumpCut = false;
        isJumping = true;
        jumpHoldTimer = 0f;
    }

    private void HandleJumpCut()
    {
        // Variable jump: apply additional upward force while jump is held
        if (jumpHeld && isJumping && !isGrounded && jumpHoldTimer < jumpHoldTime)
        {
            // Calculate additional force based on how long jump has been held
            float holdProgress = jumpHoldTimer / jumpHoldTime;
            float additionalForce = Mathf.Lerp(0f, maxJumpForce - minJumpForce, holdProgress);
            
            // Apply additional upward force for variable jump height
            float frameForce = additionalForce * (Time.deltaTime / jumpHoldTime);
            rb.AddForce(Vector2.up * frameForce, ForceMode2D.Force);
        }
        
        // Stop applying extra force if jump button is released or max time reached
        if (!jumpHeld || jumpHoldTimer >= jumpHoldTime)
        {
            isJumpCut = true;
        }
        
        // Reset jumping state when landing
        if (isGrounded && isJumping)
        {
            isJumping = false;
            isJumpCut = false;
            jumpHoldTimer = 0f;
        }
    }

    private void UpdateJumpPhysics()
    {
        // Determine if we're falling
        isFalling = rb.linearVelocity.y < -0.1f;
        
        // Apply different gravity based on jump state
        if (isWallRunning)
        {
            rb.gravityScale = wallRunGravityScale;
        }
        else if (isFalling || isJumpCut)
        {
            // Fast fall when falling or jump was cut
            if (crouchHeld && !isGrounded)
            {
                // Extra fast fall when holding crouch/down
                rb.gravityScale = fastFallGravityScale;
            }
            else
            {
                // Normal fast fall
                rb.gravityScale = fallGravityScale;
            }
        }
        else
        {
            // Normal gravity during jump ascent
            rb.gravityScale = originalGravityScale;
        }
    }
    #endregion

    #region Movement Actions
    private void Jump()
    {
        PerformJump();
    }

    private void WallJump()
    {
        // Calculate wall jump direction and force
        Vector2 wallJumpDirection = new Vector2(-wallDirection, 1).normalized;

        // Reset velocity for consistent wall jump
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(wallJumpDirection * jumpForce * 1.2f, ForceMode2D.Impulse);

        isWallRunning = false;
    }

    private void StartKick()
    {
        isKicking = true;
        kickTimer = kickDuration;

        // TODO: Trigger kick animation
        // TODO: Create kick hitbox to destroy barriers
    }

    private void UpdateKickTimer()
    {
        if (isKicking)
        {
            kickTimer -= Time.deltaTime;
            if (kickTimer <= 0)
            {
                isKicking = false;
            }
        }
    }
    #endregion

    #region State Management
    private void HandleStates()
    {
        // Store previous states to detect changes
        bool previousCrouching = isCrouching;
        bool previousSliding = isSliding;

        // Both crouching and sliding require the crouch button to be held
        if (crouchHeld && isGrounded && !isWallRunning)
        {
            // Determine if we should crouch or slide based on velocity
            if (Mathf.Abs(rb.linearVelocity.x) > 1f)
            {
                // Moving fast while crouching = sliding
                isCrouching = false;
                isSliding = true;
            }
            else
            {
                // Stationary or slow while crouching = crouching
                isCrouching = true;
                isSliding = false;
            }
        }
        else
        {
            // No crouch button held or not grounded = no crouching or sliding
            isCrouching = false;
            isSliding = false;
        }

        // Update collider size if crouching or sliding state changed
        if (previousCrouching != isCrouching || previousSliding != isSliding)
        {
            UpdateColliderSize();
        }

        // Update wall running state
        bool shouldWallRun = wallRunHeld && canWallRun && !isGrounded && Mathf.Abs(moveInput.x) > 0.1f;
        isWallRunning = shouldWallRun;
    }
    #endregion

    #region Physics Checks
    private void CheckGrounded()
    {
        Vector2 boxCenter = (Vector2)transform.position + boxCollider.offset;
        Vector2 boxSize = boxCollider.size;

        RaycastHit2D hit = Physics2D.BoxCast(boxCenter, boxSize, 0f, Vector2.down, groundCheckDistance, groundLayer);
        isGrounded = hit.collider != null && rb.linearVelocity.y <= 0.1f; // Only grounded when moving down or stationary
    }

    private void CheckWallRun()
    {
        if (isGrounded)
        {
            canWallRun = false;
            return;
        }

        Vector2 boxCenter = (Vector2)transform.position + boxCollider.offset;
        Vector2 boxSize = new Vector2(wallCheckDistance, boxCollider.size.y);

        // Check right wall
        RaycastHit2D rightHit = Physics2D.BoxCast(boxCenter, boxSize, 0f, Vector2.right, wallCheckDistance, wallLayer);
        // Check left wall
        RaycastHit2D leftHit = Physics2D.BoxCast(boxCenter, boxSize, 0f, Vector2.left, wallCheckDistance, wallLayer);

        if (rightHit.collider != null && moveInput.x > 0.1f)
        {
            canWallRun = true;
            wallDirection = 1; // Right wall
        }
        else if (leftHit.collider != null && moveInput.x < -0.1f)
        {
            canWallRun = true;
            wallDirection = -1; // Left wall
        }
        else
        {
            canWallRun = false;
        }
    }

    private void UpdateColliderSize()
    {
        if (isCrouching || isSliding)
        {
            boxCollider.size = crouchColliderSize;
            boxCollider.offset = crouchColliderOffset;
        }
        else
        {
            boxCollider.size = originalColliderSize;
            boxCollider.offset = originalColliderOffset;
        }
    }
    #endregion

    #region Debug
    private void OnDrawGizmosSelected()
    {
        if (boxCollider == null) return;

        Vector2 boxCenter = (Vector2)transform.position + boxCollider.offset;
        Vector2 boxSize = boxCollider.size;

        // Draw ground check
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireCube(boxCenter + Vector2.down * groundCheckDistance, boxSize);

        // Draw wall check
        Gizmos.color = canWallRun ? Color.blue : Color.gray;
        Vector2 wallCheckSize = new Vector2(wallCheckDistance, boxSize.y);
        Gizmos.DrawWireCube(boxCenter + Vector2.right * wallCheckDistance, wallCheckSize);
        Gizmos.DrawWireCube(boxCenter + Vector2.left * wallCheckDistance, wallCheckSize);
    }
    #endregion
}

using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public PlayerConfig config;
    
    private Rigidbody2D rb;
    private PlayerStates states;
    [SerializeField] private BoxCollider2D kickBox;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        states = GetComponent<PlayerStates>();
    }

    private void Start()
    {
        rb.linearDamping = 0f;
        rb.linearDamping = 0f;
    }

    private void FixedUpdate()
    {
        HandleLanding();

        ApplyDrag();
        ClampFallSpeed();

        if (states.isStaggered) return;

        if (rb.linearVelocity.x < 0)
        {
            gameObject.GetComponentInChildren<SpriteRenderer>().flipX = true;
            kickBox.offset = new Vector2(-Mathf.Abs(kickBox.offset.x), kickBox.offset.y);
        }
        else if (rb.linearVelocity.x > 0)
        {
            gameObject.GetComponentInChildren<SpriteRenderer>().flipX = false;
            kickBox.offset = new Vector2(Mathf.Abs(kickBox.offset.x), kickBox.offset.y);
        }

        HandleMovement();
    }

    public void StaggerPlayer(Vector2 knockbackDirection)
    {
        if (config == null || states == null) return;

        states.StartStagger(config.staggerDuration);

        Vector2 knockbackForce = knockbackDirection.normalized * config.staggerKnockbackForce;
        rb.linearVelocity = new Vector2(knockbackForce.x, Mathf.Max(knockbackForce.y, rb.linearVelocity.y));
    }

    private void HandleMovement()
    {
        if (config == null) return;

        float forceMultiplier = states.isKicking ? config.kickForceReduction : 1f;

        if (states.isStaggered)
        {
            forceMultiplier *= config.staggerMovementReduction;
        }

        states.animator.SetFloat("xVelocity", Mathf.Abs(rb.linearVelocity.x));
        states.animator.SetFloat("yVelocity", rb.linearVelocity.y);

        if (states.isWallRunning)
        {
            HandleWallRunMovement(forceMultiplier);
        }
        else if (states.isSliding)
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

        if (states.isKicking) return;

        if (states.isCrouching || states.isSliding) return;


        float horizontalInput = states.moveInput.x;

        if (Mathf.Abs(horizontalInput) > 0.1f)
        {
            float targetSpeed = horizontalInput * config.maxRunSpeed;
            float speedDifference = targetSpeed - rb.linearVelocity.x;

            float force = states.isGrounded ? config.runForce : config.runForce * config.airControlMultiplier;
            force *= forceMultiplier;

            float appliedForce = Mathf.Clamp(speedDifference * force, -force, force);
            rb.AddForce(Vector2.right * appliedForce * Time.fixedDeltaTime, ForceMode2D.Force);
        }

        // Fast fall when crouching in air
        if (states.crouchHeld && !states.isGrounded)
        {
            rb.AddForce(Vector2.down * config.runForce * Time.fixedDeltaTime, ForceMode2D.Force);
        }
    }

    private void HandleSlideMovement(float forceMultiplier)
    {
        // Sliding uses momentum only - no horizontal input processed
        // Could add slide steering here if needed
    }

    private void HandleWallRunMovement(float forceMultiplier)
    {
        if (states.isKicking) return;

        rb.AddForce(Vector2.up * config.wallRunForce * forceMultiplier * Time.fixedDeltaTime, ForceMode2D.Force);

        float horizontalInput = states.moveInput.x;
        if (Mathf.Abs(horizontalInput) > 0.1f)
        {
            float horizontalForce = horizontalInput * config.runForce * 0.5f * forceMultiplier;
            rb.AddForce(Vector2.right * horizontalForce * Time.fixedDeltaTime, ForceMode2D.Force);
        }
    }

    private void ApplyDrag()
    {
        Vector2 velocity = rb.linearVelocity;
        float horizontalInput = states.moveInput.x;
        
        // Handle momentum preservation during landing grace period FIRST, regardless of other states
        if (states.justLanded && states.isGrounded)
        {
            if (Mathf.Abs(velocity.x - states.landingHorizontalVelocity) > 0.1f)
            {
                rb.linearVelocity = new Vector2(states.landingHorizontalVelocity, velocity.y);
            }
            
            // Continue to apply appropriate drag based on current state
        }
        
        // Early returns for non-ground states
        if (!states.isGrounded || states.isKicking)
            return;
            
        // Special case: if sliding, use reduced drag but still apply some
        if (states.isSliding)
        {
            Vector2 dragForces = -velocity * config.slideDrag * Time.fixedDeltaTime;
            rb.AddForce(new Vector2(dragForces.x, 0), ForceMode2D.Force);
            return;
        }
        
        // Normal drag system for regular ground movement
        float targetDrag = 0f;
        
        if (Mathf.Abs(horizontalInput) < 0.1f)
        {
            targetDrag = config.groundDrag;
        }
        else
        {
            bool inputOpposesMovement = (horizontalInput * velocity.x) < 0;
            targetDrag = inputOpposesMovement ? config.groundDrag * 1.5f : config.groundDrag * 0.1f;
        }
        
        Vector2 dragForce = -velocity * targetDrag * Time.fixedDeltaTime;
        rb.AddForce(new Vector2(dragForce.x, 0), ForceMode2D.Force);
    }

    public void HandleLanding()
    {
        if (config == null) return;

        // Store air velocity for momentum preservation - capture ALL horizontal momentum
        if (!states.isGrounded)
        {
            states.landingHorizontalVelocity = rb.linearVelocity.x;
        }
        
        // Detect landing and handle momentum conversion
        if (states.wasGrounded != states.isGrounded && states.isGrounded)
        {
            // Prevent multiple landing detections within a short timeframe
            if (states.justLanded && states.landingGraceTimer > config.landingGracePeriod * 0.8f)
            {
                Debug.Log("Ignoring duplicate landing detection");
                return;
            }
            
            states.landingVerticalVelocity = rb.linearVelocity.y;
            float currentHorizontalVel = rb.linearVelocity.x;
            
            bool isSlamLanding = states.landingVerticalVelocity <= config.slamLandingThreshold;
            bool hasHorizontalInput = Mathf.Abs(states.moveInput.x) > 0.1f;
            bool isHoldingCrouchButton = states.crouchHeld;
            
            Debug.Log($"Landing detected! Vertical velocity: {states.landingVerticalVelocity}, Horizontal: {states.landingHorizontalVelocity}, Input: {states.moveInput.x}, Crouch: {isHoldingCrouchButton}, Slam: {isSlamLanding}");
            
            // Slam landing: fast fall + crouch button + horizontal input
            if (isSlamLanding && hasHorizontalInput && isHoldingCrouchButton)
            {
                float downwardSpeed = Mathf.Abs(states.landingVerticalVelocity);
                float horizontalBoost = downwardSpeed * config.momentumConversionRatio;
                float inputDirection = Mathf.Sign(states.moveInput.x);
                
                // Apply the boost on top of preserved momentum
                float preservedMomentum = states.landingHorizontalVelocity;
                float newHorizontalVelocity = preservedMomentum + (horizontalBoost * inputDirection);
                
                rb.linearVelocity = new Vector2(newHorizontalVelocity, 0f);
                states.landingHorizontalVelocity = newHorizontalVelocity;
                
                Debug.Log($"SLAM LANDING! Preserved: {preservedMomentum}, Boost: {horizontalBoost}, Final: {newHorizontalVelocity}");
            }
            else
            {
                // Regular landing: preserve horizontal momentum without boost
                rb.linearVelocity = new Vector2(states.landingHorizontalVelocity, 0f);
                Debug.Log($"Regular landing: Preserving momentum {states.landingHorizontalVelocity}");
            }
            
            // Start momentum preservation grace period
            states.justLanded = true;
            states.landingGraceTimer = config.landingGracePeriod;
        }
    }

    private void ClampFallSpeed()
    {
        if (config == null) return;

        // Clamp downward velocity to prevent phasing through platforms
        if (rb.linearVelocity.y < -config.maxFallSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -config.maxFallSpeed);
        }
    }
}

using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public PlayerConfig config;
    
    private Rigidbody2D rb;
    private PlayerStates states;

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
        if (rb.linearVelocity.x < 0)
        {
            gameObject.GetComponentInChildren<SpriteRenderer>().flipX = true;
        }
        else if (rb.linearVelocity.x > 0)
        {
            gameObject.GetComponentInChildren<SpriteRenderer>().flipX = false;
        }

        HandleMovement();
        ApplyDrag();
        ClampFallSpeed();
    }

    private void HandleMovement()
    {
        if (config == null) return;

        float forceMultiplier = states.isKicking ? config.kickForceReduction : 1f;

        states.animator.SetFloat("xVelocity", Mathf.Abs(rb.linearVelocity.x));
        states.animator.SetFloat("yVelocity", rb.linearVelocity.y);

        states.animator.SetBool("isSliding", states.isSliding);
        states.animator.SetBool("isWallRunning", states.isWallRunning);
        states.animator.SetBool("isCrouching", states.isCrouching);

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
        if (!states.isGrounded || states.isSliding || states.isKicking)
            return;
        
        Vector2 velocity = rb.linearVelocity;
        float horizontalInput = states.moveInput.x;
        
        // Preserve horizontal momentum during landing grace period
        if (states.justLanded)
        {
            float currentHorizontalVelocity = velocity.x;
            float velocityDifference = states.landingHorizontalVelocity - currentHorizontalVelocity;
            
            if (Mathf.Abs(velocityDifference) > 0.5f)
            {
                float preservationForce = velocityDifference * 50f;
                rb.AddForce(Vector2.right * preservationForce, ForceMode2D.Force);
            }
            return;
        }
        
        // Normal drag system
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

        // Store air velocity for momentum preservation
        if (!states.isGrounded && Mathf.Abs(rb.linearVelocity.x) > 1f)
        {
            states.landingHorizontalVelocity = rb.linearVelocity.x;
        }
        
        // Detect landing and handle momentum conversion
        if (states.wasGrounded != states.isGrounded && states.isGrounded)
        {
            states.landingVerticalVelocity = rb.linearVelocity.y;
            float currentHorizontalVel = rb.linearVelocity.x;
            
            bool isSlamLanding = states.landingVerticalVelocity <= config.slamLandingThreshold;
            bool hasHorizontalInput = Mathf.Abs(states.moveInput.x) > 0.1f;
            bool isHoldingCrouchButton = states.crouchHeld;
            
            // Slam landing: fast fall + crouch button + horizontal input
            if (isSlamLanding && hasHorizontalInput && isHoldingCrouchButton)
            {
                float downwardSpeed = Mathf.Abs(states.landingVerticalVelocity);
                float horizontalBoost = downwardSpeed * config.momentumConversionRatio;
                float inputDirection = Mathf.Sign(states.moveInput.x);
                float newHorizontalVelocity = currentHorizontalVel + (horizontalBoost * inputDirection);
                
                rb.linearVelocity = new Vector2(newHorizontalVelocity, 0f);
                states.landingHorizontalVelocity = newHorizontalVelocity;
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

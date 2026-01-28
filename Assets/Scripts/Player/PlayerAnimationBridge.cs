using UnityEngine;

public class PlayerAnimationBridge : MonoBehaviour
{
    private static class AnimParams
    {
        public static readonly int IsKicking = Animator.StringToHash("isKicking");
        public static readonly int IsSliding = Animator.StringToHash("isSliding");
        public static readonly int IsWallRunning = Animator.StringToHash("isWallRunning");
        public static readonly int IsCrouching = Animator.StringToHash("isCrouching");
        public static readonly int IsStaggered = Animator.StringToHash("isStaggered");
        public static readonly int IsDiving = Animator.StringToHash("isDiving");
        public static readonly int IsRolling = Animator.StringToHash("isRolling");
        public static readonly int IsVaulting = Animator.StringToHash("isVaulting");
        public static readonly int XVelocity = Animator.StringToHash("xVelocity");
        public static readonly int YVelocity = Animator.StringToHash("yVelocity");
        public static readonly int IsGrounded = Animator.StringToHash("IsGrounded");
        public static readonly int IsOnSlope = Animator.StringToHash("isOnSlope");
    }

    [Header("Dependencies")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerStates states;

    [Header("Velocity Settings")]
    [SerializeField] private float velocityDeadzone = 0.2f;
    [SerializeField] private float maxClampedVelocity = 15f;

    private bool previousKickState = false;

    private void Awake()
    {
        ValidateDependencies();
    }

    private void LateUpdate()
    {
        if (animator == null || states == null) return;

        UpdateVelocityParameters();
        UpdateBooleanStates();
        HandleKickTrigger();
    }

    private void UpdateVelocityParameters()
    {
        if (rb == null) return;

        float xVel = CalculateXVelocity();
        float yVel = CalculateYVelocity();

        animator.SetFloat(AnimParams.XVelocity, xVel);
        animator.SetFloat(AnimParams.YVelocity, yVel);
    }

    private float CalculateXVelocity()
    {
        float xVel = Mathf.Abs(rb.linearVelocity.x);

        if (xVel < velocityDeadzone)
        {
            return 0f;
        }

        if (states.isDiving || states.isRolling || states.isSliding)
        {
            return Mathf.Clamp(xVel, 0f, maxClampedVelocity);
        }

        return xVel;
    }

    private float CalculateYVelocity()
    {
        if (states.isOnSlope || states.isSliding)
        {
            return 0f;
        }

        return rb.linearVelocity.y;
    }

    private void UpdateBooleanStates()
    {
        animator.SetBool(AnimParams.IsGrounded, states.isGrounded);
        animator.SetBool(AnimParams.IsOnSlope, states.isOnSlope);
        animator.SetBool(AnimParams.IsSliding, states.isSliding);
        animator.SetBool(AnimParams.IsWallRunning, states.isWallRunning);
        animator.SetBool(AnimParams.IsCrouching, states.isCrouching);
        animator.SetBool(AnimParams.IsStaggered, states.isStaggered);
        animator.SetBool(AnimParams.IsDiving, states.isDiving);
        animator.SetBool(AnimParams.IsRolling, states.isRolling);
        animator.SetBool(AnimParams.IsVaulting, states.isVaultingWall);
    }

    private void HandleKickTrigger()
    {
        bool currentKickState = states.isKicking;

        if (currentKickState && !previousKickState)
        {
            animator.SetTrigger(AnimParams.IsKicking);
            GameLogger.LogVerbose("ANIM", $"Kick trigger activated - isGrounded={states.isGrounded}");
        }
        else if (!currentKickState && previousKickState)
        {
            animator.ResetTrigger(AnimParams.IsKicking);
            GameLogger.LogVerbose("ANIM", "Kick trigger reset");
        }

        previousKickState = currentKickState;
    }

    private void ValidateDependencies()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                GameLogger.LogError("ANIM", "PlayerAnimationBridge requires an Animator component!");
            }
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                GameLogger.LogError("ANIM", "PlayerAnimationBridge requires a Rigidbody2D component!");
            }
        }

        if (states == null)
        {
            states = GetComponent<PlayerStates>();
            if (states == null)
            {
                GameLogger.LogError("ANIM", "PlayerAnimationBridge requires a PlayerStates component!");
            }
        }
    }
}

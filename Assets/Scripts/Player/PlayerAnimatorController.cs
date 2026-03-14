using UnityEngine;

public class PlayerAnimatorController : MonoBehaviour
{
    private static readonly int IsKicking = Animator.StringToHash("Kicking");
    private static readonly int IsSliding = Animator.StringToHash("isSliding");
    private static readonly int IsWallRunning = Animator.StringToHash("isWallRunning");
    private static readonly int IsCrouching = Animator.StringToHash("isCrouching");
    private static readonly int IsStaggered = Animator.StringToHash("isStaggered");
    private static readonly int IsDiving = Animator.StringToHash("isDiving");
    private static readonly int IsRolling = Animator.StringToHash("isRolling");
    private static readonly int IsVaulting = Animator.StringToHash("isVaulting");
    private static readonly int XVelocity = Animator.StringToHash("xVelocity");
    private static readonly int YVelocity = Animator.StringToHash("yVelocity");
    private static readonly int IsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int IsOnSlope = Animator.StringToHash("isOnSlope");

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerStates states;
    [SerializeField] private PlayerConfig config;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (states == null)
        {
            states = GetComponent<PlayerStates>();
        }

        if (config == null)
        {
            config = GetComponent<PlayerMovement>()?.config;
        }
    }

    private void LateUpdate()
    {
        UpdateAnimator();
    }

    private void UpdateAnimator()
    {
        if (animator == null || states == null) return;

        float xVel = 0f;
        
        if (rb != null)
        {
            xVel = Mathf.Abs(rb.linearVelocity.x);
            
            if (xVel < 0.2f)
            {
                xVel = 0f;
            }
            
            if (states.isDiving || states.isRolling || states.isSliding)
            {
                xVel = Mathf.Clamp(xVel, 0f, 15f);
            }
            
            animator.SetFloat(XVelocity, xVel);
            
            float yVelocityToSet = rb.linearVelocity.y;
            
            if (states.isOnSlope)
            {
                yVelocityToSet = 0f;
            }
            else if (states.isSliding)
            {
                yVelocityToSet = 0f;
            }
            
            animator.SetFloat(YVelocity, yVelocityToSet);
        }
        
        animator.SetBool(IsGrounded, states.isGrounded);
        animator.SetBool(IsOnSlope, states.isOnSlope);
        animator.SetBool(IsSliding, states.isSliding);
        animator.SetBool(IsWallRunning, states.isWallRunning);
        animator.SetBool(IsCrouching, states.isCrouching);
        animator.SetBool(IsStaggered, states.isStaggered);
        animator.SetBool(IsDiving, states.isDiving);
        animator.SetBool(IsRolling, states.isRolling);
        animator.SetBool(IsVaulting, states.isVaultingWall);
    }

    public void TriggerKick()
    {
        if (animator != null)
        {
            animator.SetBool(IsKicking, true);
            GameLogger.Log("KICK", $"Animator Bool SET TRUE - isGrounded={states?.isGrounded}, yVel={rb?.linearVelocity.y:F2}");
            
            float duration = config != null ? config.kickDuration : 0.2f;
            Invoke(nameof(ResetKickBool), duration);
        }
    }
    
    private void ResetKickBool()
    {
        if (animator != null)
        {
            animator.SetBool(IsKicking, false);
            GameLogger.Log("KICK", "Animator Bool RESET to FALSE");
        }
    }
}

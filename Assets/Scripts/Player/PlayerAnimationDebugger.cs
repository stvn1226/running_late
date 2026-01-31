using UnityEngine;

public class PlayerAnimationDebugger : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PlayerStates states;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerAnimator playerAnimator;
    [SerializeField] private Animator animator;

    [Header("State Monitoring")]
    [SerializeField] private bool isGrounded;
    [SerializeField] private bool isKicking;
    [SerializeField] private bool isRolling;
    [SerializeField] private bool isDiving;
    [SerializeField] private bool isWallRunning;
    [SerializeField] private bool isSliding;
    [SerializeField] private bool isCrouching;
    [SerializeField] private bool isStaggered;
    [SerializeField] private bool isVaulting;

    [Header("Timers")]
    [SerializeField] private float kickTimer;
    [SerializeField] private float rollTimer;
    [SerializeField] private float diveTimer;
    [SerializeField] private float staggerTimer;

    [Header("Physics")]
    [SerializeField] private Vector2 velocity;
    [SerializeField] private float velocityX;
    [SerializeField] private float velocityY;
    [SerializeField] private float speed;

    [Header("Animator Info")]
    [SerializeField] private string currentAnimationState;
    [SerializeField] private int currentStateHash;
    [SerializeField] private float normalizedTime;
    [SerializeField] private bool isAnimatorEnabled;

    [Header("Logs")]
    [SerializeField] private bool logStateChanges = false;
    [SerializeField] private bool logEveryFrame = false;

    private bool wasKicking = false;
    private bool wasRolling = false;
    private bool wasDiving = false;
    private bool wasGrounded = false;
    private bool wasWallRunning = false;

    private void Awake()
    {
        ValidateDependencies();
    }

    private void Update()
    {
        if (states == null || rb == null) return;

        UpdateStateMonitoring();
        UpdatePhysicsMonitoring();
        UpdateAnimatorMonitoring();
        LogStateChanges();

        if (logEveryFrame)
        {
            LogFrameState();
        }
    }

    private void UpdateStateMonitoring()
    {
        isGrounded = states.isGrounded;
        isKicking = states.isKicking;
        isRolling = states.isRolling;
        isDiving = states.isDiving;
        isWallRunning = states.isWallRunning;
        isSliding = states.isSliding;
        isCrouching = states.isCrouching;
        isStaggered = states.isStaggered;
        isVaulting = states.isVaultingWall;

        kickTimer = states.kickTimer;
        rollTimer = states.rollTimer;
        diveTimer = states.diveTimer;
        staggerTimer = states.staggerTimer;
    }

    private void UpdatePhysicsMonitoring()
    {
        velocity = rb.linearVelocity;
        velocityX = velocity.x;
        velocityY = velocity.y;
        speed = velocity.magnitude;
    }

    private void UpdateAnimatorMonitoring()
    {
        if (animator != null)
        {
            isAnimatorEnabled = animator.enabled;
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            currentStateHash = stateInfo.shortNameHash;
            normalizedTime = stateInfo.normalizedTime;
            currentAnimationState = playerAnimator != null ? playerAnimator.GetCurrentStateName() : "Unknown";
        }
    }

    private void LogStateChanges()
    {
        if (!logStateChanges) return;

        if (isKicking != wasKicking)
        {
            GameLogger.Log("ANIM_DEBUG", $"[KICK] {wasKicking} → {isKicking} | isGrounded={isGrounded} | anim={currentAnimationState}");
        }

        if (isRolling != wasRolling)
        {
            GameLogger.Log("ANIM_DEBUG", $"[ROLL] {wasRolling} → {isRolling} | anim={currentAnimationState}");
        }

        if (isDiving != wasDiving)
        {
            GameLogger.Log("ANIM_DEBUG", $"[DIVE] {wasDiving} → {isDiving} | anim={currentAnimationState}");
        }

        if (isGrounded != wasGrounded)
        {
            GameLogger.Log("ANIM_DEBUG", $"[GROUND] {wasGrounded} → {isGrounded} | velY={velocityY:F2} | anim={currentAnimationState}");
        }

        if (isWallRunning != wasWallRunning)
        {
            GameLogger.Log("ANIM_DEBUG", $"[WALL] {wasWallRunning} → {isWallRunning} | anim={currentAnimationState}");
        }

        wasKicking = isKicking;
        wasRolling = isRolling;
        wasDiving = isDiving;
        wasGrounded = isGrounded;
        wasWallRunning = isWallRunning;
    }

    private void LogFrameState()
    {
        GameLogger.LogVerbose("ANIM_DEBUG", 
            $"[FRAME] anim={currentAnimationState} | kick={isKicking} | ground={isGrounded} | " +
            $"vel=({velocityX:F2}, {velocityY:F2}) | kickTimer={kickTimer:F3}"
        );
    }

    private void ValidateDependencies()
    {
        if (states == null)
        {
            states = GetComponent<PlayerStates>();
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (playerAnimator == null)
        {
            playerAnimator = GetComponent<PlayerAnimator>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    [ContextMenu("Force Play Kick")]
    public void DebugForceKick()
    {
        if (playerAnimator != null)
        {
            playerAnimator.ForcePlayAnimation("KickA");
            GameLogger.Log("ANIM_DEBUG", "FORCED KickA animation via debug tool");
        }
    }

    [ContextMenu("Log Current State")]
    public void DebugLogCurrentState()
    {
        GameLogger.Log("ANIM_DEBUG", 
            $"=== CURRENT STATE ===\n" +
            $"Animation: {currentAnimationState}\n" +
            $"isKicking: {isKicking} (timer={kickTimer:F3})\n" +
            $"isGrounded: {isGrounded}\n" +
            $"isRolling: {isRolling}\n" +
            $"isDiving: {isDiving}\n" +
            $"Velocity: ({velocityX:F2}, {velocityY:F2})\n" +
            $"===================="
        );
    }

    [ContextMenu("Toggle State Change Logs")]
    public void ToggleStateChangeLogs()
    {
        logStateChanges = !logStateChanges;
        GameLogger.Log("ANIM_DEBUG", $"State change logs: {logStateChanges}");
    }

    [ContextMenu("Toggle Frame Logs")]
    public void ToggleFrameLogs()
    {
        logEveryFrame = !logEveryFrame;
        GameLogger.Log("ANIM_DEBUG", $"Frame logs: {logEveryFrame}");
    }
}

using UnityEngine;

/// <summary>
/// Code-based animation system that replaces Animator Controller logic.
/// Determines animation states based on PlayerStates and plays them via Animator.CrossFade.
/// </summary>
public class PlayerAnimator : MonoBehaviour
{
    /// <summary>
    /// Animation state hashes - cached for performance.
    /// Maps state names to their hashed values for Animator.CrossFade/Play.
    /// </summary>
    private static class AnimHash
    {
        public static readonly int Idle = Animator.StringToHash("Idle");
        public static readonly int Run = Animator.StringToHash("Run");
        public static readonly int JumpRise = Animator.StringToHash("JumpRise");
        public static readonly int JumpMid = Animator.StringToHash("JumpMid");
        public static readonly int JumpFall = Animator.StringToHash("JumpFall");
        public static readonly int Land = Animator.StringToHash("JumpFall");
        public static readonly int WallSlide = Animator.StringToHash("WallSprint");
        public static readonly int WallClimb = Animator.StringToHash("WallSprint");
        public static readonly int Slide = Animator.StringToHash("Slide");
        public static readonly int Roll = Animator.StringToHash("Roll");
        public static readonly int Crouch = Animator.StringToHash("Crouch");
        public static readonly int KickA = Animator.StringToHash("Kick");
        public static readonly int KickB = Animator.StringToHash("Kick");
        public static readonly int KickC = Animator.StringToHash("Kick");
        public static readonly int GroundSlam = Animator.StringToHash("Dive");
        public static readonly int HitDamage = Animator.StringToHash("Knockback");
        public static readonly int WallJump = Animator.StringToHash("WallSprint");
        public static readonly int WallSideRun = Animator.StringToHash("WallSideRun");
    }

    [Header("Dependencies")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerStates states;

    [Header("Animation Settings")]
    [SerializeField] private float crossFadeDuration = 0.1f;
    [SerializeField] private float velocityDeadzone = 0.2f;
    [SerializeField] private float runSpeedThreshold = 0.5f;
    [SerializeField] private float fallThreshold = -0.5f;
    [SerializeField] private float riseThreshold = 2.0f;

    [Header("Kick Settings")]
    [SerializeField] private bool randomizeKickVariations = true;
    [SerializeField] private int lastKickIndex = -1;

    [Header("Performance")]
    [SerializeField] private bool performanceMode = true;
    [Tooltip("Skip frames when in Idle/Run to reduce CPU usage")]
    [SerializeField] private int skipFramesInLocomotion = 2;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    [SerializeField] private string currentStateName = "None";
    [SerializeField] private int currentStateHash = 0;

    private bool wasKicking = false;
    private bool wasRolling = false;
    private bool wasDiving = false;
    private bool wasGrounded = false;
    private bool wasWallRunning = false;
    private bool wasWallSliding = false;
    private bool wasStaggered = false;
    private bool wasVaulting = false;
    private bool wasSlidingOnCable = false;
    private float lastVelocityY = 0f;
    private float lastAnimationChangeTime = 0f;
    private const float MIN_ANIMATION_CHANGE_INTERVAL = 0.05f;
    private int frameCounter = 0;

    private void Awake()
    {
        ValidateDependencies();
    }

    private void LateUpdate()
    {
        if (animator == null || states == null) return;

        if (performanceMode)
        {
            frameCounter++;
            
            bool isInLocomotion = (currentStateHash == AnimHash.Idle || currentStateHash == AnimHash.Run);
            
            if (isInLocomotion && frameCounter % (skipFramesInLocomotion + 1) != 0)
            {
                return;
            }
        }

        UpdateAnimation();
    }

    private void UpdateAnimation()
    {
        int targetHash = DetermineTargetAnimation();

        if (targetHash != currentStateHash)
        {
            // Critical transitions (wall run exit, vault, landing, stagger) bypass cooldown for instant response
            bool isCritical = IsCriticalTransition(currentStateHash, targetHash);
            float timeSinceLastChange = Time.time - lastAnimationChangeTime;
            
            // Apply cooldown to prevent animation flickering, except for critical transitions
            if (isCritical || timeSinceLastChange >= MIN_ANIMATION_CHANGE_INTERVAL)
            {
                PlayAnimation(targetHash);
                lastAnimationChangeTime = Time.time;
            }
        }

        // Cache previous states for transition detection (e.g., detecting kick start, landing, etc.)
        lastVelocityY = rb.linearVelocity.y;
        wasGrounded = states.isGrounded;
        wasKicking = states.isKicking;
        wasRolling = states.isRolling;
        wasDiving = states.isDiving;
        wasWallRunning = states.isWallRunning;
        wasWallSliding = states.isWallSliding;
        wasStaggered = states.isStaggered;
        wasVaulting = states.isVaultingWall;
        wasSlidingOnCable = states.isSlidingOnCable;
    }
    
    /// <summary>
    /// Determines if a transition should bypass the cooldown for instant response.
    /// Used for critical gameplay moments (wall exit, vault, landing, damage, kick recovery).
    /// </summary>
    private bool IsCriticalTransition(int fromHash, int toHash)
    {
        if (fromHash == AnimHash.WallSlide || fromHash == AnimHash.WallClimb)
            return true;

        if (fromHash == AnimHash.WallSideRun)
            return true;

        if (toHash == AnimHash.WallJump)
            return true;
        
        if (toHash == AnimHash.Land)
            return true;
        
        if (toHash == AnimHash.HitDamage)
            return true;
        
        if ((fromHash == AnimHash.KickA || fromHash == AnimHash.KickB || fromHash == AnimHash.KickC) 
            && toHash == AnimHash.JumpFall)
            return true;
        
        return false;
    }

    /// <summary>
    /// Animation decision logic - determines which animation should play based on player state.
    /// Priority order (top = highest):
    /// 1. Stagger/Damage (highest priority - always shows)
    /// 2. Kick (continues until kick ends)
    /// 3. Dive/Roll
    /// 4. Wall vault
    /// 5. Cable slide
    /// 6. Wall run (climb/slide)
    /// 7. Slope locomotion (prioritized to avoid jump flicker on slopes)
    /// 8. Air states (rise/mid/fall)
    /// 9. Landing
    /// 10. Ground locomotion (slide/crouch/run/idle)
    /// </summary>
    private int DetermineTargetAnimation()
    {
        if (states.isStaggered)
            return AnimHash.HitDamage;

        if (states.isKicking && !wasKicking)
            return GetRandomKickVariation();

        if (states.isKicking)
            return currentStateHash;

        if (states.isDiving)
            return AnimHash.GroundSlam;

        if (states.isRolling)
            return AnimHash.Roll;

        if (states.isVaultingWall)
            return AnimHash.WallJump;

        if (states.isSlidingOnCable)
            return AnimHash.Slide;

        if (states.isWallRunning)
        {
            float absVelY = Mathf.Abs(rb.linearVelocity.y);
            if (absVelY > velocityDeadzone)
                return AnimHash.WallClimb;
            else
                return AnimHash.WallSlide;
        }

        if (states.isWallSliding)
            return AnimHash.WallSideRun;

        // IMPORTANT: Check slope BEFORE air states to prevent jump flicker on slopes
        // Even if isGrounded flickers, isOnSlope remains stable and prioritizes locomotion
        if (states.isOnSlope)
        {
            if (states.isSliding)
                return AnimHash.Slide;
            
            if (states.isCrouching)
                return AnimHash.Crouch;
            
            float slopeAbsVelX = Mathf.Abs(rb.linearVelocity.x);
            if (slopeAbsVelX > runSpeedThreshold)
                return AnimHash.Run;
            
            return AnimHash.Idle;
        }

        if (!states.isGrounded)
        {
            float velY = rb.linearVelocity.y;

            if (velY > riseThreshold)
                return AnimHash.JumpRise;
            else if (velY < fallThreshold)
                return AnimHash.JumpFall;
            else
                return AnimHash.JumpMid;
        }

        if (wasGrounded != states.isGrounded && states.isGrounded)
            return AnimHash.Land;

        if (states.isSliding)
            return AnimHash.Slide;

        if (states.isCrouching && !states.isSliding)
            return AnimHash.Crouch;

        float absVelX = Mathf.Abs(rb.linearVelocity.x);
        if (absVelX > runSpeedThreshold)
            return AnimHash.Run;

        return AnimHash.Idle;
    }

    private int GetRandomKickVariation()
    {
        if (!randomizeKickVariations)
            return AnimHash.KickA;

        int[] kickVariations = { AnimHash.KickA, AnimHash.KickB, AnimHash.KickC };
        int randomIndex;

        do
        {
            randomIndex = Random.Range(0, kickVariations.Length);
        }
        while (randomIndex == lastKickIndex && kickVariations.Length > 1);

        lastKickIndex = randomIndex;
        return kickVariations[randomIndex];
    }

    private void PlayAnimation(int stateHash)
    {
        if (crossFadeDuration > 0f)
            animator.CrossFade(stateHash, crossFadeDuration, 0);
        else
            animator.Play(stateHash, 0, 0f);

        currentStateHash = stateHash;
        currentStateName = GetStateNameFromHash(stateHash);

        if (enableDebugLogs)
            GameLogger.Log("ANIM", $"Playing: {currentStateName} | isGrounded={states.isGrounded} | velY={rb.linearVelocity.y:F2}");
    }

    private string GetStateNameFromHash(int hash)
    {
        if (hash == AnimHash.Idle) return "Idle";
        if (hash == AnimHash.Run) return "Run";
        if (hash == AnimHash.JumpRise) return "JumpRise";
        if (hash == AnimHash.JumpMid) return "JumpMid";
        if (hash == AnimHash.JumpFall) return "JumpFall";
        if (hash == AnimHash.Land) return "JumpFall (Land)";
        if (hash == AnimHash.WallSlide) return "WallSprint (Slide)";
        if (hash == AnimHash.WallClimb) return "WallSprint (Climb)";
        if (hash == AnimHash.Slide) return "Slide";
        if (hash == AnimHash.Roll) return "Roll";
        if (hash == AnimHash.Crouch) return "Crouch";
        if (hash == AnimHash.KickA) return "Kick (A)";
        if (hash == AnimHash.KickB) return "Kick (B)";
        if (hash == AnimHash.KickC) return "Kick (C)";
        if (hash == AnimHash.GroundSlam) return "Dive (GroundSlam)";
        if (hash == AnimHash.HitDamage) return "Knockback (HitDamage)";
        if (hash == AnimHash.WallJump) return "WallSprint (WallJump)";
        if (hash == AnimHash.WallSideRun) return "WallSideRun";
        return "Unknown";
    }

    private void ValidateDependencies()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
                GameLogger.LogError("ANIM", "Animator component not found!");
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
                GameLogger.LogError("ANIM", "Rigidbody2D component not found!");
        }

        if (states == null)
        {
            states = GetComponent<PlayerStates>();
            if (states == null)
                GameLogger.LogError("ANIM", "PlayerStates component not found!");
        }
    }

    public void ForcePlayAnimation(string animationName)
    {
        int hash = Animator.StringToHash(animationName);
        PlayAnimation(hash);
    }

    public void SetCrossFadeDuration(float duration)
    {
        crossFadeDuration = Mathf.Max(0f, duration);
    }

    public bool IsPlayingKick()
    {
        return currentStateHash == AnimHash.KickA || 
               currentStateHash == AnimHash.KickB || 
               currentStateHash == AnimHash.KickC;
    }

    public float GetKickNormalizedTime()
    {
        if (!IsPlayingKick()) return 0f;
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.normalizedTime % 1f;
    }

    public string GetCurrentStateName()
    {
        return currentStateName;
    }
}

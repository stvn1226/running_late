using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
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
            // Check if this is a critical transition that should bypass cooldown
            bool isCritical = IsCriticalTransition(currentStateHash, targetHash);
            
            float timeSinceLastChange = Time.time - lastAnimationChangeTime;
            
            // LOG: Animation change blocked by cooldown
            if (!isCritical && timeSinceLastChange < MIN_ANIMATION_CHANGE_INTERVAL)
            {
                string fromName = GetStateNameFromHash(currentStateHash);
                string toName = GetStateNameFromHash(targetHash);
                Debug.LogWarning($"<color=yellow>[ANIM BLOCKED]</color> {fromName} → {toName} | " +
                    $"Cooldown: {timeSinceLastChange:F3}s < 0.05s | " +
                    $"isGrounded={states.isGrounded} | isOnSlope={states.isOnSlope} | " +
                    $"isWallRun={states.isWallRunning} | velY={rb.linearVelocity.y:F2}");
            }
            
            if (isCritical || timeSinceLastChange >= MIN_ANIMATION_CHANGE_INTERVAL)
            {
                PlayAnimation(targetHash);
                lastAnimationChangeTime = Time.time;
            }
        }

        lastVelocityY = rb.linearVelocity.y;
        wasGrounded = states.isGrounded;
        wasKicking = states.isKicking;
        wasRolling = states.isRolling;
        wasDiving = states.isDiving;
        wasWallRunning = states.isWallRunning;
        wasStaggered = states.isStaggered;
        wasVaulting = states.isVaultingWall;
        wasSlidingOnCable = states.isSlidingOnCable;
    }
    
    private bool IsCriticalTransition(int fromHash, int toHash)
    {
        // Wall run exit → ALWAYS immediate
        if (fromHash == AnimHash.WallSlide || fromHash == AnimHash.WallClimb)
        {
            return true;
        }
        
        // Landing → ALWAYS immediate
        if (toHash == AnimHash.Land)
        {
            return true;
        }
        
        // Stagger/damage → ALWAYS immediate
        if (toHash == AnimHash.HitDamage)
        {
            return true;
        }
        
        // Kick end in air → ALWAYS immediate
        if ((fromHash == AnimHash.KickA || fromHash == AnimHash.KickB || fromHash == AnimHash.KickC) 
            && toHash == AnimHash.JumpFall)
        {
            return true;
        }
        
        return false;
    }

    private int DetermineTargetAnimation()
    {
        if (states.isStaggered)
        {
            return AnimHash.HitDamage;
        }

        if (states.isKicking && !wasKicking)
        {
            return GetRandomKickVariation();
        }

        if (states.isKicking)
        {
            return currentStateHash;
        }

        if (states.isDiving)
        {
            return AnimHash.GroundSlam;
        }

        if (states.isRolling)
        {
            return AnimHash.Roll;
        }

        if (states.isVaultingWall)
        {
            return AnimHash.WallJump;
        }

        if (states.isSlidingOnCable)
        {
            return AnimHash.Slide;
        }

        if (states.isWallRunning)
        {
            float absVelY = Mathf.Abs(rb.linearVelocity.y);
            if (absVelY > velocityDeadzone)
            {
                return AnimHash.WallClimb;
            }
            else
            {
                return AnimHash.WallSlide;
            }
        }
        
        // LOG: WallRun exit detection
        if (wasWallRunning && !states.isWallRunning)
        {
            Debug.Log($"<color=magenta>[WALLRUN EXIT]</color> wasWallRun=TRUE → isWallRun=FALSE | " +
                $"isGrounded={states.isGrounded} | velY={rb.linearVelocity.y:F2} | " +
                $"timeSinceLastChange={(Time.time - lastAnimationChangeTime):F3}s");
        }

        // Check slope FIRST - even if isGrounded flickers, prioritize slope locomotion
        if (states.isOnSlope)
        {
            Debug.Log($"<color=cyan>[SLOPE DETECTED]</color> isOnSlope=TRUE | isGrounded={states.isGrounded} | " +
                $"slopeAngle={states.slopeAngle:F1}° | velY={rb.linearVelocity.y:F2} | " +
                $"velX={rb.linearVelocity.x:F2} → Using LOCOMOTION");
            
            if (states.isSliding)
            {
                return AnimHash.Slide;
            }
            
            if (states.isCrouching)
            {
                return AnimHash.Crouch;
            }
            
            float slopeAbsVelX = Mathf.Abs(rb.linearVelocity.x);
            if (slopeAbsVelX > runSpeedThreshold)
            {
                return AnimHash.Run;
            }
            
            return AnimHash.Idle;
        }

        if (!states.isGrounded)
        {
            float velY = rb.linearVelocity.y;
            
            // LOG: Air state with slope info
            if (states.isOnSlope)
            {
                Debug.LogWarning($"<color=red>[SLOPE BUG]</color> isOnSlope=TRUE but isGrounded=FALSE! | " +
                    $"slopeAngle={states.slopeAngle:F1}° | velY={velY:F2} → Will show JUMP animation!");
            }

            if (velY > riseThreshold)
            {
                return AnimHash.JumpRise;
            }
            else if (velY < fallThreshold)
            {
                return AnimHash.JumpFall;
            }
            else
            {
                return AnimHash.JumpMid;
            }
        }

        if (wasGrounded != states.isGrounded && states.isGrounded)
        {
            return AnimHash.Land;
        }

        if (states.isSliding)
        {
            return AnimHash.Slide;
        }

        if (states.isCrouching && !states.isSliding)
        {
            return AnimHash.Crouch;
        }

        float absVelX = Mathf.Abs(rb.linearVelocity.x);
        if (absVelX > runSpeedThreshold)
        {
            return AnimHash.Run;
        }

        return AnimHash.Idle;
    }

    private int GetRandomKickVariation()
    {
        if (!randomizeKickVariations)
        {
            return AnimHash.KickA;
        }

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
        {
            animator.CrossFade(stateHash, crossFadeDuration, 0);
        }
        else
        {
            animator.Play(stateHash, 0, 0f);
        }

        currentStateHash = stateHash;
        currentStateName = GetStateNameFromHash(stateHash);

        if (enableDebugLogs)
        {
            GameLogger.Log("ANIM", $"Playing: {currentStateName} | isGrounded={states.isGrounded} | velY={rb.linearVelocity.y:F2}");
        }
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
        return "Unknown";
    }

    private void ValidateDependencies()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                GameLogger.LogError("ANIM", "Animator component not found!");
            }
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                GameLogger.LogError("ANIM", "Rigidbody2D component not found!");
            }
        }

        if (states == null)
        {
            states = GetComponent<PlayerStates>();
            if (states == null)
            {
                GameLogger.LogError("ANIM", "PlayerStates component not found!");
            }
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

    public string GetCurrentStateName()
    {
        return currentStateName;
    }
}

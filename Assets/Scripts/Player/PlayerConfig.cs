using UnityEngine;

[CreateAssetMenu(fileName = "PlayerConfig", menuName = "Player/Player Config")]
public class PlayerConfig : ScriptableObject
{
    // ====================================
    // HORIZONTAL MOVEMENT
    // ====================================
    
    [Header("Horizontal Movement")]
    [Tooltip("Acceleration force when running. Higher = faster acceleration. Recommended: 2500-4000")]
    [Range(1000f, 6000f)]
    public float runForce = 3000f;
    
    [Tooltip("Maximum horizontal speed the player can reach. Units: m/s. Recommended: 20-35")]
    [Range(10f, 50f)]
    public float maxRunSpeed = 30f;
    
    [Tooltip("Movement control multiplier while airborne. 1.0 = full control, 0.5 = half control. Recommended: 0.7-0.9")]
    [Range(0.1f, 1f)]
    public float airControlMultiplier = 0.8f;

    // ====================================
    // JUMP MECHANICS
    // ====================================
    
    [Header("Jump Forces")]
    [Tooltip("Minimum jump force (tap jump). This is the instant impulse applied. Recommended: 10-15")]
    [Range(5f, 25f)]
    public float minJumpForce = 12f;
    
    [Tooltip("Maximum jump force (held jump). Total force when holding jump button. Recommended: 15-22")]
    [Range(10f, 30f)]
    public float maxJumpForce = 18f;
    
    [Tooltip("How long the player can hold jump to gain extra height. Units: seconds. Recommended: 0.2-0.4")]
    [Range(0.1f, 0.6f)]
    public float jumpHoldTime = 0.3f;

    [Header("Jump Physics")]
    [Tooltip("Gravity scale when falling or after releasing jump. Higher = faster fall. Recommended: 4-8")]
    [Range(2f, 12f)]
    public float fallGravityScale = 6f;
    
    [Tooltip("Gravity scale when crouch is held mid-air (fast fall). Higher = faster plummet. Recommended: 7-12")]
    [Range(4f, 15f)]
    public float fastFallGravityScale = 9f;

    [Header("Jump Input Forgiveness")]
    [Tooltip("Jump buffer time. Allows pressing jump slightly before landing. Units: seconds. Recommended: 0.1-0.15")]
    [Range(0.05f, 0.3f)]
    public float jumpBufferTime = 0.12f;
    
    [Tooltip("Coyote time. Allows jumping briefly after leaving ground. Units: seconds. Recommended: 0.08-0.15")]
    [Range(0.05f, 0.25f)]
    public float coyoteTime = 0.1f;

    [Header("Long Jump")]
    [Tooltip("Vertical force multiplier for long jumps. Lower = less height. Recommended: 0.5-0.7")]
    [Range(0.3f, 1f)]
    public float longJumpVerticalMultiplier = 0.6f;

    [Tooltip("Horizontal force boost for long jumps. Units: force. Recommended: 15-30")]
    [Range(10f, 50f)]
    public float longJumpHorizontalBoost = 20f;

    [Tooltip("Buffer time to detect crouch+jump input. Units: seconds. Recommended: 0.1-0.2")]
    [Range(0.05f, 0.3f)]
    public float longJumpBufferTime = 0.15f;

    [Tooltip("Grace period after long jump where fast-falling is disabled. Recommended: 0.3-0.5")]
    [Range(0.1f, 1f)]
    public float longJumpGracePeriod = 0.4f;

    // ====================================
    // AIR DODGE MECHANICS
    // ====================================

    [Header("Air Dodge")]
    [Tooltip("Force applied when performing an air dodge. Units: m/s. Recommended: 15-25")]
    [Range(10f, 40f)]
    public float airDodgeForce = 20f;

    [Tooltip("Duration of the air dodge movement. Units: seconds. Recommended: 0.2-0.4")]
    [Range(0.1f, 0.6f)]
    public float airDodgeDuration = 0.3f;

    [Tooltip("Cooldown before air dodge can be used again after landing. Units: seconds. Recommended: 0.1-0.3")]
    [Range(0.05f, 0.5f)]
    public float airDodgeCooldown = 0.15f;

    // ====================================
    // DIVE MECHANICS
    // ====================================

    [Header("Dive & Roll")]
    [Tooltip("Horizontal boost force applied when diving. Units: m/s. Recommended: 20-35")]
    [Range(10f, 50f)]
    public float diveHorizontalBoost = 25f;

    [Tooltip("Vertical velocity reduction multiplier when diving. 0 = no vertical velocity. Recommended: 0.3-0.6")]
    [Range(0f, 1f)]
    public float diveVerticalReduction = 0.4f;

    [Tooltip("Duration of the dive state. Units: seconds. Recommended: 0.3-0.6")]
    [Range(0.1f, 1f)]
    public float diveDuration = 0.4f;

    [Tooltip("Duration of the roll animation. Units: seconds. Recommended: 0.4-0.8")]
    [Range(0.2f, 1.5f)]
    public float rollDuration = 0.5f;

    [Tooltip("Minimum horizontal velocity required to dive. Units: m/s. Recommended: 2-5")]
    [Range(0f, 10f)]
    public float minDiveVelocity = 3f;

    // ====================================
    // WALL RUN MECHANICS
    // ====================================

    [Header("Wall Run")]
    [Tooltip("Force applied when running on walls. Higher = faster wall climb. Recommended: 2500-4000")]
    [Range(1000f, 6000f)]
    public float wallRunForce = 3000f;
    
    [Tooltip("Gravity scale while wall running. Lower = slower slide down. 0 = no fall. Recommended: 0.5-2")]
    [Range(0f, 4f)]
    public float wallRunGravityScale = 1f;
    
    [Tooltip("Force multiplier for wall jumps. 1.0 = same as normal jump. Recommended: 0.9-1.2")]
    [Range(0.5f, 2f)]
    public float wallJumpMultiplier = 1f;

    // ====================================
    // DRAG & FRICTION
    // ====================================
    
    [Header("Drag & Friction")]
    [Tooltip("Ground friction coefficient. Higher = faster deceleration on ground. Recommended: 40-70")]
    [Range(10f, 100f)]
    public float groundDrag = 50f;
    
    [Tooltip("Air friction coefficient. Usually same as ground drag. Recommended: 40-70")]
    [Range(10f, 100f)]
    public float airDrag = 50f;
    
    [Tooltip("Friction while sliding. Lower = slide goes further. Recommended: 5-20")]
    [Range(1f, 50f)]
    public float slideDrag = 10f;

    // ====================================
    // SPEED LIMITS
    // ====================================
    
    [Header("Speed Limits")]
    [Tooltip("Maximum downward velocity. Prevents infinite fall speed. Units: m/s. Recommended: 15-25")]
    [Range(10f, 100f)]
    public float maxFallSpeed = 20f;

    // ====================================
    // PHYSICS DETECTION
    // ====================================
    
    [Header("Ground Detection")]
    [Tooltip("Distance to cast ground check ray. Should be slightly larger than collider skin. Recommended: 0.1-0.2")]
    [Range(0.05f, 0.5f)]
    public float groundCheckDistance = 0.1f;
    
    [Tooltip("Layer(s) considered as ground. Usually 'Ground' layer.")]
    public LayerMask groundLayer = 1;

    [Header("Wall Detection")]
    [Tooltip("Distance to cast wall check rays. Should reach nearby walls. Recommended: 0.3-0.7")]
    [Range(0.1f, 1.5f)]
    public float wallCheckDistance = 0.5f;
    
    [Tooltip("Layer(s) considered as walls for wall running. Usually 'HardWall' + 'SoftWall'.")]
    public LayerMask wallLayer = 1;

    // ====================================
    // COMBAT
    // ====================================
    
    [Header("Kick Attack")]
    [Tooltip("How long the kick hitbox stays active. Units: seconds. Recommended: 0.3-0.6")]
    [Range(0.2f, 1f)]
    public float kickDuration = 0.5f;
    
    [Tooltip("Movement speed reduction during kick. 0.5 = half speed. Recommended: 0.3-0.7")]
    [Range(0f, 1f)]
    public float kickForceReduction = 0.5f;

    [Header("Stagger (Enemy Hit)")]
    [Tooltip("How long the player is staggered after enemy hit. Units: seconds. Recommended: 0.5-1.0")]
    [Range(0.2f, 2f)]
    public float staggerDuration = 0.8f;
    
    [Tooltip("Knockback force when hit by enemy. Higher = pushed further. Recommended: 15-30")]
    [Range(5f, 50f)]
    public float staggerKnockbackForce = 20f;
    
    [Tooltip("Movement control during stagger. 0.2 = 20% control. Recommended: 0.1-0.3")]
    [Range(0f, 0.5f)]
    public float staggerMovementReduction = 0.2f;

    // ====================================
    // LANDING MECHANICS
    // ====================================
    
    [Header("Landing Momentum System")]
    [Tooltip("Time after landing where drag is NOT applied, preserving momentum. Units: seconds. Recommended: 0.3-0.7")]
    [Range(0.1f, 1f)]
    public float landingGracePeriod = 0.5f;
    
    [Tooltip("Minimum vertical velocity to trigger slam landing. Negative value. Recommended: -8 to -15")]
    [Range(-20f, 0f)]
    public float slamLandingThreshold = -0.1f;
    
    [Tooltip("Percentage of vertical speed converted to horizontal on slam. 0.6 = 60% conversion. Recommended: 0.4-0.8")]
    [Range(0f, 1f)]
    public float momentumConversionRatio = 0.6f;

    // ====================================
    // COLLIDER ADJUSTMENTS
    // ====================================
    
    [Header("Crouch & Slide Collider")]
    [Tooltip("Size multiplier when crouching/sliding. (x=width, y=height). Recommended: (1.3-1.5, 0.3-0.4)")]
    public Vector2 crouchSizeMultiplier = new Vector2(1.33f, 0.33f);

    // ====================================
    // PLATFORM INTERACTION
    // ====================================
    
    [Header("One-Way Platforms")]
    [Tooltip("How long platform collision is disabled after dropping through. Units: seconds. Recommended: 0.2-0.4")]
    [Range(0.1f, 1f)]
    public float dropDownDisableTime = 0.3f;
    
    [Tooltip("Layer(s) considered as one-way platforms. Usually 'OneWayPlatform' layer.")]
    public LayerMask platformLayerMask = -1;

    // ====================================
    // CAMERA CONTROLS
    // ====================================
    
    [Header("Camera Pan (Right Stick)")]
    [Tooltip("Maximum distance the camera can pan from player. Units: Unity units. Recommended: 3-8")]
    [Range(1f, 15f)]
    public float panAmount = 5f;
    
    [Tooltip("Speed of camera panning movement. Higher = snappier. Recommended: 3-8")]
    [Range(1f, 15f)]
    public float panSpeed = 5f;
    
    [Tooltip("Speed of camera returning to center. Usually faster than panSpeed. Recommended: 5-12")]
    [Range(1f, 20f)]
    public float returnSpeed = 8f;
    
    [Tooltip("Invert horizontal camera pan axis")]
    public bool invertXAxis = false;
    
    [Tooltip("Invert vertical camera pan axis")]
    public bool invertYAxis = false;


    // ====================================
    // VALIDATION (Editor Only)
    // ====================================
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        // Ensure max jump is always greater than min jump
        if (maxJumpForce < minJumpForce)
        {
            maxJumpForce = minJumpForce + 1f;
            Debug.LogWarning($"[PlayerConfig] maxJumpForce cannot be less than minJumpForce. Adjusted to {maxJumpForce}");
        }

        // Ensure fast fall is faster than normal fall
        if (fastFallGravityScale < fallGravityScale)
        {
            fastFallGravityScale = fallGravityScale + 1f;
            Debug.LogWarning($"[PlayerConfig] fastFallGravityScale should be higher than fallGravityScale. Adjusted to {fastFallGravityScale}");
        }

        // Ensure ground check distance is reasonable
        if (groundCheckDistance < 0.05f)
        {
            groundCheckDistance = 0.05f;
            Debug.LogWarning("[PlayerConfig] groundCheckDistance too small. Adjusted to 0.05");
        }

        // Ensure slam threshold is negative
        if (slamLandingThreshold > 0f)
        {
            slamLandingThreshold = -Mathf.Abs(slamLandingThreshold);
            Debug.LogWarning($"[PlayerConfig] slamLandingThreshold must be negative. Adjusted to {slamLandingThreshold}");
        }

        // Ensure crouch height is smaller than width
        if (crouchSizeMultiplier.y > crouchSizeMultiplier.x)
        {
            Debug.LogWarning("[PlayerConfig] Crouch height (y) is larger than width (x). This may look odd.");
        }

        // Ensure camera return is faster than pan (recommended)
        if (returnSpeed < panSpeed)
        {
            Debug.LogWarning("[PlayerConfig] returnSpeed is slower than panSpeed. Camera may feel sluggish returning to center.");
        }
    }
#endif
}

using UnityEngine;

[CreateAssetMenu(fileName = "PlayerConfig", menuName = "Player/Player Config")]
public class PlayerConfig : ScriptableObject
{
    [Header("Movement Forces")]
    public float runForce = 3000f;
    public float maxRunSpeed = 12f;
    public float airControlMultiplier = 0.6f;
    //public float slideForce = 4000f;

    [Header("Jump Settings")]
    public float minJumpForce = 12f;
    public float maxJumpForce = 18f;
    public float jumpHoldTime = 0.3f;
    public float fallGravityScale = 3f;
    public float fastFallGravityScale = 5f;
    public float jumpBufferTime = 0.12f;
    public float coyoteTime = 0.1f;

    [Header("Wall Running")]
    public float wallRunForce = 2000f;
    public float wallRunGravityScale = 0.3f;
    public float wallJumpMultiplier = 1.2f;

    [Header("Physics Tuning")]
    public float groundDrag = 50f;
    public float airDrag = 50f;
    public float slideDrag = 25f;

    [Header("Physics Detection")]
    public float groundCheckDistance = 0.1f;
    public float wallCheckDistance = 0.5f;
    public LayerMask groundLayer = 1;
    public LayerMask wallLayer = 1;

    [Header("Kick Settings")]
    public float kickDuration = 0.3f;
    public float kickForceReduction = 0.5f;

    [Header("Landing Mechanics")]
    public float landingGracePeriod = 0.25f;
    public float slamLandingThreshold = -8f;
    public float momentumConversionRatio = 0.2f;

    [Header("Collider Settings")]
    public Vector2 crouchSizeMultiplier = new Vector2(1.33f, 0.33f);
    
    [Header("Drop-Down Platform Settings")]
    public float dropDownDisableTime = 0.2f;
    public LayerMask platformLayerMask = -1;

    [Header("Pan Amount")]
    public float panAmount = 0.5f;
    public float panSpeed = 5f;
    public float returnSpeed = 8f;
    public bool invertXAxis = false;
    public bool invertYAxis = false;

}
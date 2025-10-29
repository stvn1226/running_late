using UnityEngine;
using static UnityEngine.Rendering.DebugUI;
using System.Collections;
using Utils;

[RequireComponent(typeof(Rigidbody2D))]
public class Pigeon : Animal
{
    private enum FlightState
    {
        Idle,
        TakingOff,
        Gliding,
        Staggered,
        Finished
    }

    [Header("Take Off")]
    [Tooltip("Time in seconds for the pigeon to finish its take off movement.")]
    [SerializeField] private float takeOffDuration = 0.6f;
    [Tooltip("Target height relative to the starting point that the pigeon will reach during take off.")]
    [SerializeField] private float takeOffHeight = 1.25f;
    [SerializeField] private AnimationCurve takeOffHeightCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve takeOffSpeedCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float maxTakeOffVerticalSpeed = 5f;

    [Header("Glide")]
    [SerializeField] private float glideSpeed = 4.5f;
    [Tooltip("How high above the start point the pigeon prefers to glide.")]
    [SerializeField] private float glideHeightOffset = 0.75f;
    [SerializeField] private float glideVerticalSnappiness = 4f;
    [SerializeField] private float maxGlideVerticalSpeed = 2.5f;
    [SerializeField] private bool continueGlidingIndefinitely = true;
    [SerializeField] private float glideDuration = 3f;

    private FlightState state = FlightState.Idle;
    private float takeOffTimer;
    private float glideTimer;
    private Vector2 takeOffStartPosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        initialGravityScale = rb.gravityScale;
        takeOffStartPosition = rb.position;
    }

    private void Start()
    {
        mainCamera = GameReferences.Instance.MainCamera;
        CachePlayerTransform();
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        state = FlightState.Idle;
        takeOffTimer = 0f;
        glideTimer = 0f;
        isStaggered = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = initialGravityScale;
        }
    }

    private void Update()
    {
        EnableDisableDistance();
        if (state == FlightState.Idle && PlayerWithinDetectionRadius())
        {
            BeginTakeOff();
        }
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        switch (state)
        {
            case FlightState.TakingOff:
                HandleTakeOff();
                break;
            case FlightState.Gliding:
                HandleGlide();
                break;
            case FlightState.Staggered:
                HandleStagger();
                break;
        }
    }

    public override void TriggerStagger()
    {
        if (state == FlightState.Idle || state == FlightState.Staggered || state == FlightState.Finished)
            return;

        GameLogger.Log("[Pigeon] Pigeon staggered and knocked out of the sky!");
        
        state = FlightState.Staggered;
        isStaggered = true;

        animator.SetBool(IsStaggered, true);

        rb.gravityScale = staggerGravityScale;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(staggerForce, ForceMode2D.Impulse);

        hitbox.enabled = false;
        solidCollider.enabled = false;
    }

    private void HandleStagger()
    {
    }

    private void BeginTakeOff()
    {
        if (state != FlightState.Idle) return;

        state = FlightState.TakingOff;
        takeOffTimer = 0f;
        takeOffStartPosition = rb.position;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
    }

    private void HandleTakeOff()
    {
        takeOffTimer += Time.fixedDeltaTime;
        float clampedDuration = Mathf.Max(0.01f, takeOffDuration);
        float t = Mathf.Clamp01(takeOffTimer / clampedDuration);

        float heightOffset = takeOffHeightCurve.Evaluate(t) * takeOffHeight;
        float targetY = takeOffStartPosition.y + heightOffset;
        float verticalVelocity = (targetY - rb.position.y) / Time.fixedDeltaTime;
        verticalVelocity = Mathf.Clamp(verticalVelocity, -maxTakeOffVerticalSpeed, maxTakeOffVerticalSpeed);

        float horizontalSpeed = Mathf.Lerp(0f, glideSpeed, Mathf.Clamp01(takeOffSpeedCurve.Evaluate(t)));
        rb.linearVelocity = new Vector2(-horizontalSpeed, verticalVelocity);

        if (Mathf.Approximately(t, 1f))
        {
            state = FlightState.Gliding;
            glideTimer = 0f;
        }
    }

    private void HandleGlide()
    {
        glideTimer += Time.fixedDeltaTime;
        float targetY = takeOffStartPosition.y + glideHeightOffset;
        float verticalDelta = targetY - rb.position.y;
        float verticalVelocity = verticalDelta * glideVerticalSnappiness;
        verticalVelocity = Mathf.Clamp(verticalVelocity, -maxGlideVerticalSpeed, maxGlideVerticalSpeed);

        rb.linearVelocity = new Vector2(-glideSpeed, verticalVelocity);

        if (!continueGlidingIndefinitely && glideTimer >= glideDuration)
        {
            state = FlightState.Finished;
        }
    }


    private void UpdateAnimator()
    {
        if (animator != null && rb != null)
        {
            float currentVelocityMagnitude = rb.linearVelocity.magnitude;
            animator.SetFloat(Velocity, currentVelocityMagnitude);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        Gizmos.color = Color.cyan;
        Vector3 origin = detectionOrigin != null ? detectionOrigin.position : transform.position;
        Gizmos.DrawWireSphere(origin, detectionRadius);
    }
}

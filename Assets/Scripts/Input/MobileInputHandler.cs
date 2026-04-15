using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// Translates single-finger touch gestures into PlayerStates inputs.
///
/// Gesture scheme (swipe direction from initial touch point):
///   Hold still  → WallRun (held while finger is down)
///   Swipe Up    → Jump
///   Swipe Down  → Slide (crouch)
///   Swipe Left  → Kick
///   Swipe Right → Dive
///
/// Difficulty balancing:
///   - swipeThresholdCm is device-independent: the same physical distance on any phone.
///   - holdActivationDelay is the window the player has to start a swipe before
///     WallRun is committed. Swipe always wins if it fires first within that window.
///   - Both gestures require a comparable amount of deliberate intent.
/// </summary>
[RequireComponent(typeof(PlayerStates))]
public class MobileInputHandler : MonoBehaviour
{
    // ─── Swipe Thresholds ─────────────────────────────────────────────────────

    [Header("Gesture Thresholds")]
    [Tooltip("Physical swipe distance in centimeters before a swipe is recognized. " +
             "Device-independent. Recommended: 0.4–0.8 cm.")]
    [Range(0.2f, 2f)]
    [SerializeField] private float swipeThresholdCm = 0.5f;

    [Tooltip("Fallback pixel threshold used when Screen.dpi is unavailable (editor Device Simulator, etc.).")]
    [Range(20f, 150f)]
    [SerializeField] private float swipeThresholdFallbackPx = 40f;

    [Tooltip("How much more dominant one axis must be over the other for a clean directional swipe. " +
             "Lower = more lenient. Recommended: 1.2–1.5.")]
    [Range(1f, 3f)]
    [SerializeField] private float axialBias = 1.2f;

    // ─── Hold Timing ──────────────────────────────────────────────────────────

    [Header("Hold Timing")]
    [Tooltip("Seconds the finger must stay still before WallRun is committed. " +
             "This is the window the player has to start a swipe instead. " +
             "Recommended: 0.12–0.2 s.")]
    [Range(0.05f, 0.5f)]
    [SerializeField] private float holdActivationDelay = 0.15f;

    // ─── References ───────────────────────────────────────────────────────────

    [Header("References")]
    [SerializeField] private PlayerStates states;
    [SerializeField] private PlayerConfig config;

    // ─── Runtime ──────────────────────────────────────────────────────────────

    private float resolvedSwipeThresholdPx;

    // ─── Internal state ───────────────────────────────────────────────────────

    private enum GesturePhase  { Idle, Tracking, Committed }
    private enum GestureAction { None, WallRun, Jump, Slide, Kick, Dive }

    private GesturePhase  phase           = GesturePhase.Idle;
    private GestureAction committedAction = GestureAction.None;
    private Vector2       touchStartPosition;
    private float         touchStartTime;

    // Track the Finger object reference — finger.index is unreliable across
    // devices and the Device Simulator, so we compare by reference instead.
    private Finger trackedFinger = null;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (states == null) states = GetComponent<PlayerStates>();
        ResolveSwipeThreshold();
    }

    private void OnEnable()
    {
        ResolveSwipeThreshold();
        EnhancedTouchSupport.Enable();
        Touch.onFingerDown += OnFingerDown;
        Touch.onFingerMove += OnFingerMove;
        Touch.onFingerUp   += OnFingerUp;
    }

    private void OnDisable()
    {
        Touch.onFingerDown -= OnFingerDown;
        Touch.onFingerMove -= OnFingerMove;
        Touch.onFingerUp   -= OnFingerUp;
        EnhancedTouchSupport.Disable();
        ResetGestureState();
        ReleaseAllInputs();
    }

    private void Update()
    {
        // Inject auto-run every frame, unless the player is staggered so knockback physics work smoothly
        if (states != null)
        {
            if (states.isStaggered)
                states.moveInput = Vector2.zero;
            else
                states.moveInput = Vector2.right;
        }

        // Promote Tracking → WallRun once the hold window expires with no swipe
        if (phase == GesturePhase.Tracking && committedAction == GestureAction.None)
        {
            if (Time.unscaledTime - touchStartTime >= holdActivationDelay)
                CommitAction(GestureAction.WallRun);
        }
    }

    // ─── DPI resolution ───────────────────────────────────────────────────────

    /// <summary>
    /// Converts the physical swipe threshold (cm) into screen pixels for this device.
    /// Falls back to a fixed value when DPI is unavailable.
    /// </summary>
    private void ResolveSwipeThreshold()
    {
        const float CmPerInch = 2.54f;
        float dpi = Screen.dpi;

        resolvedSwipeThresholdPx = dpi > 0f
            ? swipeThresholdCm / CmPerInch * dpi
            : swipeThresholdFallbackPx;

        GameLogger.LogInput($"[Mobile] Swipe threshold: {resolvedSwipeThresholdPx:F1}px " +
                            $"(DPI={dpi:F0}, target={swipeThresholdCm}cm)");
    }

    // ─── Touch callbacks ──────────────────────────────────────────────────────

    private void OnFingerDown(Finger finger)
    {
        // Ignore additional fingers if already tracking one
        if (trackedFinger != null) return;

        trackedFinger      = finger;
        touchStartPosition = finger.screenPosition;
        touchStartTime     = Time.unscaledTime;
        phase              = GesturePhase.Tracking;
        committedAction    = GestureAction.None;

        GameLogger.LogInput($"[Mobile] FingerDown — index={finger.index} pos={finger.screenPosition}");
    }

    private void OnFingerMove(Finger finger)
    {
        // Only process the finger we're tracking
        if (finger != trackedFinger) return;
        if (phase != GesturePhase.Tracking) return;

        Vector2 delta = finger.screenPosition - touchStartPosition;

        GameLogger.LogInput($"[Mobile] FingerMove — delta={delta} mag={delta.magnitude:F1} threshold={resolvedSwipeThresholdPx:F1}");

        if (delta.magnitude < resolvedSwipeThresholdPx) return;

        GestureAction swipeAction = ClassifySwipe(delta);
        if (swipeAction != GestureAction.None)
            CommitAction(swipeAction);
    }

    private void OnFingerUp(Finger finger)
    {
        if (finger != trackedFinger) return;

        ReleaseCurrentAction();
        ResetGestureState();

        GameLogger.LogInput($"[Mobile] FingerUp — index={finger.index}");
    }

    // ─── Gesture classification ───────────────────────────────────────────────

    private GestureAction ClassifySwipe(Vector2 delta)
    {
        float absX = Mathf.Abs(delta.x);
        float absY = Mathf.Abs(delta.y);

        if (absY > absX * axialBias)
            return delta.y > 0f ? GestureAction.Jump : GestureAction.Slide;

        if (absX > absY * axialBias)
            return delta.x > 0f ? GestureAction.Dive : GestureAction.Kick;

        return GestureAction.None;
    }

    // ─── Action dispatch ──────────────────────────────────────────────────────

    private void CommitAction(GestureAction action)
    {
        if (committedAction != GestureAction.None) return; // Already locked in

        committedAction = action;
        phase           = GesturePhase.Committed;
        ApplyActionStart(action);
        GameLogger.LogInput($"[Mobile] Committed: {action}");
    }

    private void ApplyActionStart(GestureAction action)
    {
        if (states == null) return;

        switch (action)
        {
            case GestureAction.WallRun:
                states.wallRunHeld = true;
                break;

            case GestureAction.Jump:
                states.SetJumpPressed();
                if (config != null)
                    states.jumpBufferTimer = config.jumpBufferTime;
                break;

            case GestureAction.Slide:
                states.crouchHeld = true;
                if (config != null && !states.isGrounded)
                    states.BufferCrouchInput(config.longJumpBufferTime);
                break;

            case GestureAction.Kick:
                if (config != null)
                    states.SetKickPressed(config.kickDuration);
                break;

            case GestureAction.Dive:
                states.SetDivePressed();
                break;
        }
    }

    private void ReleaseCurrentAction()
    {
        if (states == null) return;

        switch (committedAction)
        {
            case GestureAction.WallRun:
                states.wallRunHeld = false;
                break;

            case GestureAction.Slide:
                states.crouchHeld = false;
                break;

            case GestureAction.Jump:
                states.SetJumpReleased();
                break;

            // Kick and Dive are fire-and-forget — no release needed
        }

        if (committedAction != GestureAction.None)
            GameLogger.LogInput($"[Mobile] Released: {committedAction}");
    }

    private void ReleaseAllInputs()
    {
        if (states == null) return;

        states.wallRunHeld = false;
        states.crouchHeld  = false;
        states.moveInput   = Vector2.zero;
        states.SetJumpReleased();
    }

    private void ResetGestureState()
    {
        phase           = GesturePhase.Idle;
        committedAction = GestureAction.None;
        trackedFinger   = null;
    }
}

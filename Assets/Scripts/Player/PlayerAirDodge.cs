using UnityEngine;

public class PlayerAirDodge : MonoBehaviour
{
    [Header("References")]
    public PlayerConfig config;

    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerStates states;

    private float dodgeTimer = 0f;
    private bool isDodging = false;
    private Vector2 preDodgeVelocity;

    private void Update()
    {
        UpdateAirDodgeAvailability();
        HandleAirDodgeInput();
        UpdateDodgeTimer();
    }

    private void UpdateAirDodgeAvailability()
    {
        if (states.isGrounded)
        {
            if (!states.canAirDodge && states.airDodgeCooldownTimer <= 0f)
            {
                states.canAirDodge = true;
                GameLogger.LogState("AIR DODGE: Available again (grounded)");
            }
        }
    }

    private void HandleAirDodgeInput()
    {
        if (config == null || states.isStaggered || states.isKicking) return;

        if (states.airDodgePressed && CanPerformAirDodge())
        {
            PerformAirDodge();
        }
    }

    private bool CanPerformAirDodge()
    {
        bool hasCharges = states.canAirDodge;
        bool notAlreadyDodging = !states.isAirDodging;
        bool cooldownReady = states.airDodgeCooldownTimer <= 0f;

        if (!hasCharges)
        {
            GameLogger.LogVerbose("AIRDODGE", "Cannot dodge: No charges available");
            return false;
        }

        if (!notAlreadyDodging)
        {
            GameLogger.LogVerbose("AIRDODGE", "Cannot dodge: Already dodging");
            return false;
        }

        if (!cooldownReady)
        {
            GameLogger.LogVerbose("AIRDODGE", $"Cannot dodge: Cooldown active ({states.airDodgeCooldownTimer:F2}s remaining)");
            return false;
        }

        return true;
    }

    private void PerformAirDodge()
    {
        Vector2 dodgeDirection = GetDodgeDirection();

        if (dodgeDirection.magnitude < 0.1f)
        {
            GameLogger.LogWarning("AIRDODGE", "No input direction for air dodge");
            return;
        }

        dodgeDirection = dodgeDirection.normalized;
        states.airDodgeDirection = dodgeDirection;

        if (states.isRolling)
        {
            states.EndRoll();
        }

        preDodgeVelocity = rb.linearVelocity;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(dodgeDirection * config.airDodgeForce, ForceMode2D.Impulse);

        states.isAirDodging = true;
        states.canAirDodge = false;
        dodgeTimer = config.airDodgeDuration;

        GameLogger.LogMovement($"AIR DODGE PERFORMED! Direction: {dodgeDirection}, Force: {config.airDodgeForce}, Velocity: {rb.linearVelocity}, Stored velocity: {preDodgeVelocity}");

        if (AnalyticsManager.Instance != null)
        {
            Vector3 playerPos = transform.position;
            var context = AnalyticsEvents.CreatePlayerActionContext(
                "air_dodge",
                playerPos.x,
                rb.linearVelocity.x
            );
            context["dodge_direction_x"] = dodgeDirection.x;
            context["dodge_direction_y"] = dodgeDirection.y;
            context["dodge_force"] = config.airDodgeForce;

            AnalyticsManager.Instance.TrackPlayerAction("air_dodge", context);
        }
    }

    private Vector2 GetDodgeDirection()
    {
        Vector2 inputDirection = states.moveInput;

        if (inputDirection.magnitude < 0.1f)
        {
            float facingDirection = rb.linearVelocity.x >= 0 ? 1f : -1f;
            inputDirection = new Vector2(facingDirection, 0f);
            GameLogger.LogVerbose("AIRDODGE", $"No input detected, using facing direction: {facingDirection}");
        }

        return inputDirection;
    }

    private void UpdateDodgeTimer()
    {
        if (states.isAirDodging)
        {
            dodgeTimer -= Time.deltaTime;

            if (dodgeTimer <= 0f)
            {
                rb.linearVelocity = new Vector2(preDodgeVelocity.x, 0f);
                states.isAirDodging = false;
                states.airDodgeCooldownTimer = config.airDodgeCooldown;
                GameLogger.LogState($"AIR DODGE: Ended, restored horizontal velocity to {preDodgeVelocity.x}, vertical set to 0");
            }
        }
    }

    public void OnStompBounce()
    {
        states.canAirDodge = true;
        GameLogger.LogState("AIR DODGE: Recharged from stomp bounce!");
    }

    public void OnLanding()
    {
        if (states.isAirDodging)
        {
            rb.linearVelocity = new Vector2(preDodgeVelocity.x, 0f);
            states.isAirDodging = false;
            GameLogger.LogState($"AIR DODGE: Cancelled by landing, restored horizontal velocity to {preDodgeVelocity.x}, vertical set to 0");
        }
    }

    public void OnWallRunStart()
    {
        states.canAirDodge = true;
        if (states.isAirDodging)
        {
            states.isAirDodging = false;
            dodgeTimer = 0f;
        }
        GameLogger.LogState("AIR DODGE: Recharged from wall run!");
    }
}

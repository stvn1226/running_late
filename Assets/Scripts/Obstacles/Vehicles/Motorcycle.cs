using UnityEngine;

public class Motorcycle : KickableVehicle
{
    private static readonly int IsStaggeredParam = Animator.StringToHash("isStaggered");

    [Header("Wheel Spin")]
    [SerializeField] private Transform wheel1;
    [SerializeField] private Transform wheel2;
    [SerializeField] private float wheelSpinSpeed = 90f;
    [SerializeField] private float ticksPerSecond = 6f;

    private float tickTimer;
    private Quaternion wheel1InitialRotation;
    private Quaternion wheel2InitialRotation;
    private Animator animator;

    protected override void Start()
    {
        base.Start();
        tickTimer = 0f;
        animator = GetComponent<Animator>();

        if (wheel1 != null) wheel1InitialRotation = wheel1.localRotation;
        if (wheel2 != null) wheel2InitialRotation = wheel2.localRotation;
    }

    /// <summary>
    /// Spins Wheel1 and Wheel2 around their local Z axis at a fixed tick rate while the motorcycle is moving.
    /// </summary>
    protected override void Update()
    {
        base.Update();
        SpinWheels();
    }

    /// <summary>
    /// Sets the animator's isStaggered parameter and resets wheels to their initial rotation when staggered.
    /// </summary>
    protected override void TriggerStagger()
    {
        base.TriggerStagger();

        if (animator != null)
            animator.SetBool(IsStaggeredParam, true);

        if (wheel1 != null) wheel1.localRotation = wheel1InitialRotation;
        if (wheel2 != null) wheel2.localRotation = wheel2InitialRotation;
    }

    private void SpinWheels()
    {
        if (isStaggered) return;
        if (rb == null) return;
        if (wheel1 == null && wheel2 == null) return;
        if (Mathf.Approximately(rb.linearVelocityX, 0f)) return;

        tickTimer += Time.deltaTime;

        float tickInterval = 1f / ticksPerSecond;
        if (tickTimer < tickInterval) return;

        tickTimer = 0f;

        // Negate velocityX so leftward movement produces a forward (positive Z) spin.
        float deltaRotation = Mathf.Sign(-rb.linearVelocityX) * wheelSpinSpeed * tickInterval;

        if (wheel1 != null)
            wheel1.Rotate(0f, 0f, deltaRotation, Space.Self);

        if (wheel2 != null)
            wheel2.Rotate(0f, 0f, deltaRotation, Space.Self);
    }
}

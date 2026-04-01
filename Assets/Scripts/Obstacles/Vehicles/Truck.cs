using UnityEngine;

public class Truck : Vehicle
{
    [Header("Tire Spin")]
    [SerializeField] private Transform tire1;
    [SerializeField] private Transform tire2;
    [SerializeField] private float tireSpinSpeed = 90f;
    [SerializeField] private float ticksPerSecond = 6f;

    private float tickTimer;

    protected override void Start()
    {
        base.Start();
        tickTimer = 0f;
    }

    protected override void Update()
    {
        base.Update();
        SpinTires();
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        base.OnTriggerEnter2D(collision);
    }

    /// <summary>
    /// Spins Tire1 and Tire2 counter-clockwise around their local Z axis at a fixed tick rate while the truck is moving.
    /// </summary>
    private void SpinTires()
    {
        if (rb == null) return;
        if (tire1 == null && tire2 == null) return;
        if (Mathf.Approximately(rb.linearVelocityX, 0f)) return;

        tickTimer += Time.deltaTime;

        float tickInterval = 1f / ticksPerSecond;
        if (tickTimer < tickInterval) return;

        tickTimer = 0f;

        float deltaRotation = tireSpinSpeed * tickInterval;

        if (tire1 != null)
            tire1.Rotate(0f, 0f, deltaRotation, Space.Self);

        if (tire2 != null)
            tire2.Rotate(0f, 0f, deltaRotation, Space.Self);
    }
}

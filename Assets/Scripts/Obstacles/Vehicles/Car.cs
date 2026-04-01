using UnityEngine;
using System.Collections;

public class Car : Vehicle
{
    private static readonly int IsCrashedParam = Animator.StringToHash("isCrashed");

    [Header("Appearance")]
    [SerializeField] private SpriteRenderer carSpriteRenderer;
    [SerializeField] private Material[] materials;

    [Header("Tire Spin")]
    [SerializeField] private Transform tire1;
    [SerializeField] private Transform tire2;
    [SerializeField] private float tireSpinSpeed = 90f;
    [SerializeField] private float ticksPerSecond = 6f;

    [Header("Crash")]
    [SerializeField] private Vector2 crashHopForce = new Vector2(3f, 2f);
    [SerializeField] private float crashSettleTime = 0.4f;

    private float tickTimer;
    private bool isCrashed;
    private Animator carAnimator;

    protected override void Start()
    {
        base.Start();
        tickTimer = 0f;
        isCrashed = false;
        carAnimator = GetComponent<Animator>();
        ApplyRandomMaterial();
    }

    protected override void Update()
    {
        base.Update();
        SpinTires();
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        if (isCrashed) return;
        base.OnTriggerEnter2D(collision);
    }

    protected override void OnPlayerDamageDealt()
    {
        TriggerCrash();
    }

    /// <summary>
    /// Stops the car, triggers the crash animation, applies a backwards hop, then kills all remaining velocity.
    /// </summary>
    public void TriggerCrash()
    {
        if (isCrashed) return;

        isCrashed = true;
        movementOverridden = true;

        // Zero velocity then apply a small backwards hop.
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(crashHopForce, ForceMode2D.Impulse);

        // Trigger crash animation.
        if (carAnimator != null)
            carAnimator.SetBool(IsCrashedParam, true);

        // Disable all hitboxes so the car no longer interacts with the player.
        foreach (Collider2D c in hitbox)
        {
            if (c != null) c.enabled = false;
        }

        StartCoroutine(SettleAfterCrash());
    }

    private IEnumerator SettleAfterCrash()
    {
        yield return new WaitForSeconds(crashSettleTime);
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;
    }

    /// <summary>
    /// Picks a random material from the materials list and applies it to the car's SpriteRenderer.
    /// </summary>
    private void ApplyRandomMaterial()
    {
        if (carSpriteRenderer == null || materials == null || materials.Length == 0) return;

        Material picked = materials[Random.Range(0, materials.Length)];
        if (picked != null)
            carSpriteRenderer.material = picked;
    }

    /// <summary>
    /// Spins Tire1 and Tire2 counter-clockwise around their local Z axis at a fixed tick rate while the car is moving.
    /// </summary>
    private void SpinTires()
    {
        if (isCrashed) return;
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

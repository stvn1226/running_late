using UnityEngine;
using Utils;

public abstract class KickableVehicle : Vehicle
{
    public Collider2D solidCollider;

    [Header("Stagger Settings")]
    [SerializeField] protected Vector2 staggerForce = new Vector2(-5f, 3f);
    [SerializeField] protected float staggerGravity = 2f;
    
    protected bool isStaggered = false;

    protected override void Start()
    {
        base.Start();
        rb = GetComponent<Rigidbody2D>();
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag(TagUtils.PLAYERKICK_TAG))
        {
            TriggerStagger();
            return;
        }

        base.OnTriggerEnter2D(collision);
    }

    protected virtual void TriggerStagger()
    {
        if (isStaggered) return;

        GameLogger.Log("KICKABLE_VEHICLE", $"{gameObject.name} staggered by kick!");
        
        isStaggered = true;

        if (rb != null)
        {
            rb.gravityScale = staggerGravity;
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(staggerForce, ForceMode2D.Impulse);
        }

        foreach (Collider2D c in hitbox)
        {
            if (c != null) c.enabled = false;
        }
        if (solidCollider != null) solidCollider.enabled = false;

    }
}

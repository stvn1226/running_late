using UnityEngine;
using System.Collections;

public class Dog : Animal
{
    private enum DogState
    {
        Idle,
        Running,
        Leaping,
        Staggered,
        Finished
    }

    [Header("Running")]
    [SerializeField] private float runSpeed = 5f;

    [Header("Leap")]
    [SerializeField] private float leapRange = 3f;
    [SerializeField] private Vector2 leapForce = new Vector2(-2f, 8f);
    [SerializeField] private float leapCooldown = 2f;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 0.3f;
    [SerializeField] private LayerMask groundLayer;

    private DogState state = DogState.Idle;
    private float lastLeapTime = -100f;
    private bool hasLeaped = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        initialGravityScale = rb.gravityScale;
    }

    private void Start()
    {
        mainCamera = GameReferences.Instance.MainCamera;
        CachePlayerTransform();
        animator = GetComponent<Animator>();
        
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    private void OnEnable()
    {
        state = DogState.Idle;
        isStaggered = false;
        lastLeapTime = -100f;
        hasLeaped = false;
        
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = initialGravityScale;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
        
        if (solidCollider != null)
        {
            solidCollider.enabled = true;
        }
        
        if (hitbox != null)
        {
            hitbox.enabled = true;
        }
    }

    private void Update()
    {
        EnableDisableDistance();
        if (state == DogState.Idle && PlayerWithinDetectionRadius())
        {
            BeginRunning();
        }

        if (state == DogState.Running && PlayerWithinLeapRange() && CanLeap())
        {
            BeginLeap();
        }

        if (state == DogState.Leaping && rb.linearVelocity.y <= 0f)
        {
            state = DogState.Running;
        }

        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        switch (state)
        {
            case DogState.Running:
                HandleRunning();
                break;
            case DogState.Leaping:
                HandleLeaping();
                break;
            case DogState.Staggered:
                HandleStagger();
                break;
        }
    }

    private void BeginRunning()
    {
        if (state != DogState.Idle) return;

        state = DogState.Running;
    }

    private void HandleRunning()
    {
        rb.linearVelocity = new Vector2(-runSpeed, rb.linearVelocity.y);
    }

    private bool PlayerWithinLeapRange()
    {
        CachePlayerTransform();
        if (player == null) return false;

        Vector2 origin = GetDetectionOrigin();
        float sqrDistance = ((Vector2)player.position - origin).sqrMagnitude;
        return sqrDistance <= leapRange * leapRange;
    }

    private bool CanLeap()
    {
        return Time.time >= lastLeapTime + leapCooldown && IsGrounded();
    }

    private bool IsGrounded()
    {
        if (rb == null || solidCollider == null) return false;

        Vector2 boxCenter = solidCollider.bounds.center;
        Vector2 boxSize = solidCollider.bounds.size;

        RaycastHit2D hit = Physics2D.BoxCast(boxCenter, boxSize, 0f, Vector2.down, groundCheckDistance, groundLayer);

        return hit.collider != null && rb.linearVelocity.y <= 0.1f;
    }

    private void BeginLeap()
    {
        if (state != DogState.Running) return;

        state = DogState.Leaping;
        lastLeapTime = Time.time;
        hasLeaped = true;

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(leapForce, ForceMode2D.Impulse);
    }

    private void HandleLeaping()
    {
        rb.linearVelocity = new Vector2(-runSpeed, rb.linearVelocity.y);
    }

    public override void TriggerStagger()
    {
        if (state == DogState.Staggered) return;

        GameLogger.Log("DOG", "Dog staggered and knocked out!");

        state = DogState.Staggered;
        isStaggered = true;

        if (animator != null)
        {
            animator.SetBool(IsStaggered, true);
        }

        rb.gravityScale = staggerGravityScale;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(staggerForce, ForceMode2D.Impulse);

        if (hitbox != null) hitbox.enabled = false;
        if (solidCollider != null) solidCollider.enabled = false;
    }

    private void HandleStagger()
    {
        if (rb.linearVelocity.x > -1f)
        {
            rb.linearVelocity = new Vector2(-2f, rb.linearVelocity.y);
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

        Vector3 origin = detectionOrigin != null ? detectionOrigin.position : transform.position;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(origin, detectionRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, leapRange);

        if (solidCollider != null)
        {
            Vector2 boxCenter = solidCollider.bounds.center;
            Vector2 boxSize = solidCollider.bounds.size;

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(boxCenter, boxSize);
            Gizmos.DrawWireCube(boxCenter + Vector2.down * groundCheckDistance, boxSize);
            Gizmos.DrawLine(boxCenter, boxCenter + Vector2.down * groundCheckDistance);
        }
    }
}

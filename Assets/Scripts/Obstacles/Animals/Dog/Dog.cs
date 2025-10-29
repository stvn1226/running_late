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

    private DogState state = DogState.Idle;

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
    }

    private void OnEnable()
    {
        state = DogState.Idle;
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
        if (state == DogState.Idle && PlayerWithinDetectionRadius())
        {
            BeginRunning();
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

    public override void TriggerStagger()
    {
        if (state == DogState.Staggered) return;

        GameLogger.Log("[Dog] Dog staggered and knocked out!");

        state = DogState.Staggered;
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

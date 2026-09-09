using UnityEngine;

/// <summary>
/// Attached to the child "Toast" GameObject on the Player.
/// Spawns and launches a physical toast in the world when the player takes damage and loses the toast.
/// </summary>
public class ToastDrop : MonoBehaviour
{
    [Header("Physics Settings")]
    [Tooltip("Forward launch force in the direction the player was running.")]
    [SerializeField] private float launchForceX = 3.5f;
    [Tooltip("Vertical launch force.")]
    [SerializeField] private float launchForceY = 10f;
    [Tooltip("Horizontal random spread variation (+/-).")]
    [SerializeField] private float randomSpread = 1.0f;
    [Tooltip("Max fallback lifetime in seconds if off-screen check doesn't trigger.")]
    [SerializeField] private float maxLifetime = 6f;

    [Header("Visual Settings")]
    [Tooltip("World scale of the dropped toast while flying in the world.")]
    [SerializeField] private float dropScale = 0.5f;

    private Transform initialParent;
    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;
    private Vector3 initialLocalScale;
    private bool hasInitialized = false;

    private Rigidbody2D rb;
    private Collider2D toastCollider;
    private Camera mainCamera;
    private bool isDropped = false;
    private float dropTime = 0f;

    private void Awake()
    {
        InitIfNeeded();
    }

    private void InitIfNeeded()
    {
        if (hasInitialized) return;
        hasInitialized = true;

        initialParent = transform.parent;
        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;
        initialLocalScale = transform.localScale;

        rb = GetComponent<Rigidbody2D>();
        toastCollider = GetComponent<Collider2D>();

        if (rb != null)
        {
            rb.simulated = false;
        }
    }

    private void Update()
    {
        if (!isDropped) return;

        // Check if toast is off-camera after initial launch window
        if (Time.time - dropTime > 0.4f)
        {
            if (IsOffScreen() || (Time.time - dropTime > maxLifetime))
            {
                ReturnToPlayer();
            }
        }
    }

    private bool IsOffScreen()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return false;
        }

        Vector3 viewportPos = mainCamera.WorldToViewportPoint(transform.position);

        // Off left, off right, or below the screen viewport
        return viewportPos.x < -0.15f || viewportPos.x > 1.25f || viewportPos.y < -0.2f;
    }

    /// <summary>
    /// Launches this toast into the world with physics arc and spin.
    /// </summary>
    public void Drop(bool facingRight)
    {
        InitIfNeeded();

        if (isDropped) return;

        isDropped = true;
        dropTime = Time.time;

        transform.SetParent(null);
        transform.localScale = Vector3.one * dropScale;
        gameObject.SetActive(true);

        // Configure collider: ignore player and obstacle layers so enemies/barriers do not block or destroy the toast
        if (toastCollider != null)
        {
            PlayerStates player = FindFirstObjectByType<PlayerStates>();
            if (player != null)
            {
                Collider2D[] playerCols = player.GetComponentsInChildren<Collider2D>();
                foreach (var pCol in playerCols)
                {
                    if (pCol != null && pCol != toastCollider)
                    {
                        Physics2D.IgnoreCollision(toastCollider, pCol, true);
                    }
                }
            }

            toastCollider.excludeLayers = LayerMask.GetMask("ObstacleTrigger", "ObstacleCollider", "BreakableBarrier", "Player");
            toastCollider.includeLayers = LayerMask.GetMask("Ground", "OneWayPlatform", "HardRoof", "Default");
            toastCollider.isTrigger = false;
        }

        // Apply launch impulse and tumbling spin
        if (rb != null)
        {
            rb.simulated = true;
            rb.gravityScale = 2.5f;
            float dirX = facingRight ? 1f : -1f;
            float spreadX = Random.Range(-randomSpread, randomSpread);
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.AddForce(new Vector2((launchForceX + spreadX) * dirX, launchForceY), ForceMode2D.Impulse);
            rb.AddTorque(Random.Range(-25f, 25f), ForceMode2D.Impulse);
        }
    }

    /// <summary>
    /// Resets the toast back to the player's initial coordinates and deactivates it.
    /// </summary>
    public void ReturnToPlayer()
    {
        isDropped = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }

        if (initialParent != null)
        {
            transform.SetParent(initialParent);
        }
        transform.localPosition = initialLocalPosition;
        transform.localRotation = initialLocalRotation;
        transform.localScale = initialLocalScale;

        gameObject.SetActive(false);
    }

    /// <summary>
    /// Reactivates the toast on the player when a Toast power-up is picked up.
    /// </summary>
    public void ResetToPlayer()
    {
        InitIfNeeded();
        ReturnToPlayer();
        gameObject.SetActive(true);
    }
}

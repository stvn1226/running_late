using System.Collections;
using UnityEngine;

public abstract class Animal : MonoBehaviour
{
    protected static readonly int IsStaggered = Animator.StringToHash("isStaggered");
    protected static readonly int Velocity = Animator.StringToHash("velocity");

    public Collider2D hitbox;
    public Collider2D solidCollider;

    [Header("Detection")]
    [SerializeField] protected float detectionRadius = 5f;
    [SerializeField] protected Transform detectionOrigin;

    [Header("Stagger")]
    [Tooltip("Force applied when animal gets staggered by kick.")]
    [SerializeField] protected Vector2 staggerForce = new Vector2(-3f, -2f);
    [Tooltip("Gravity scale applied during stagger to make animal fall.")]
    [SerializeField] protected float staggerGravityScale = 2f;

    [Header("Animator")]
    protected Animator animator;

    [Header("Debug")]
    [SerializeField] protected bool showGizmos = true;

    protected Transform player;
    public PlayerMovement cachedPlayerMovement;
    protected Rigidbody2D rb;

    protected float initialGravityScale;
    protected bool isStaggered = false;

    protected Camera mainCamera;
    protected bool isRunning;
    protected bool hasRan;

    public bool wasHit = false;


    //void Start()
    //{
        
    //}

    //// Update is called once per frame
    //void Update()
    //{
        
    //}

    public abstract void TriggerStagger();

    public IEnumerator DisableHitboxTemp(float duration)
    {
        hitbox.enabled = false;
        yield return new WaitForSeconds(duration);
        hitbox.enabled = true;
    }

    protected virtual bool PlayerWithinDetectionRadius()
    {
        CachePlayerTransform();
        if (player == null) return false;

        Vector2 origin = GetDetectionOrigin();
        float sqrDistance = ((Vector2)player.position - origin).sqrMagnitude;
        return sqrDistance <= detectionRadius * detectionRadius;
    }

    protected virtual void CachePlayerTransform()
    {
        if (player) return;
        player = GameReferences.Instance.PlayerTransform;
    }

    protected virtual Vector2 GetDetectionOrigin()
    {
        if (detectionOrigin != null)
        {
            return detectionOrigin.position;
        }

        if (rb != null)
        {
            return rb.position;
        }

        return transform.position;
    }

    protected virtual void EnableDisableDistance()
    {
        if (mainCamera == null) return;

        if (Mathf.Abs(transform.position.x - mainCamera.transform.position.x) > 20f)
        {
            isRunning = false;
        }
        else
        {
            isRunning = true;
            hasRan = true;
        }

        if (hasRan && Mathf.Abs(transform.position.x - mainCamera.transform.position.x) > 40f)
        {
            Destroy(gameObject);
        }
    }

}

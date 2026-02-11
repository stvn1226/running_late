using System.Collections;
using UnityEngine;

public abstract class Animal : MonoBehaviour, IWarningIndicator
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
    
    [Header("Warning Indicator")]
    [SerializeField] protected SpriteRenderer spriteRenderer;

    protected Transform player;
    public PlayerMovement cachedPlayerMovement;
    protected Rigidbody2D rb;

    protected float initialGravityScale;
    public bool isStaggered = false;

    protected Camera mainCamera;
    protected bool isRunning;
    protected bool hasRan;

    public bool wasHit = false;
    private Coroutine hitboxDisableCoroutine;

    public abstract void TriggerStagger();

    public void ResetHitState()
    {
        wasHit = false;
        GameLogger.LogVerbose("ANIMAL", $"[{gameObject.name}] Hit state RESET");
    }

    public IEnumerator DisableHitboxTemp(float duration)
    {
        if (hitbox == null)
        {
            GameLogger.LogWarning("ANIMAL", $"[{gameObject.name}] Hitbox is null, cannot disable temporarily");
            yield break;
        }

        if (hitboxDisableCoroutine != null)
        {
            StopCoroutine(hitboxDisableCoroutine);
        }

        hitbox.enabled = false;
        GameLogger.LogVerbose("ANIMAL", $"[{gameObject.name}] Hitbox DISABLED for {duration}s");
        
        yield return new WaitForSeconds(duration);
        
        if (hitbox != null && this != null && !isStaggered)
        {
            hitbox.enabled = true;
            ResetHitState();
            GameLogger.LogVerbose("ANIMAL", $"[{gameObject.name}] Hitbox RE-ENABLED and hit state reset");
        }

        hitboxDisableCoroutine = null;
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

    protected virtual void OnDisable()
    {
        if (hitbox != null) hitbox.enabled = true;
        if (solidCollider != null) solidCollider.enabled = true;
        ResetHitState();
    }

    protected virtual void OnDestroy()
    {
        if (hitboxDisableCoroutine != null)
        {
            StopCoroutine(hitboxDisableCoroutine);
        }
        StopAllCoroutines();
    }
    
    public virtual Sprite GetWarningSprite()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            return spriteRenderer.sprite;
        }
        return null;
    }
    
    public virtual Vector3 GetPosition()
    {
        return transform.position;
    }
    
    public virtual bool ShouldShowWarning()
    {
        return !isStaggered && !isRunning;
    }
}

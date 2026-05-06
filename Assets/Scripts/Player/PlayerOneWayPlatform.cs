using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerOneWayPlatform : MonoBehaviour
{
    [Header("Configuration")]
    public PlayerConfig config;
    
    [Header("Settings")]
    [SerializeField] private float safetyDelay = 0.3f;
    [SerializeField] private float baseLookAhead = 2f;
    [SerializeField] private float velocityLookAheadMultiplier = 0.2f;
    
    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;

    private CapsuleCollider2D playerCollider;
    private PlayerStates playerStates;
    private Rigidbody2D playerRb;
    private LayerMask platformLayerMask;
    
    private HashSet<Collider2D> ignoredPlatforms = new HashSet<Collider2D>();
    private List<Collider2D> platformsToRestore = new List<Collider2D>();
    private Coroutine restoreCoroutine;

    private void Awake()
    {
        playerCollider = GetComponent<CapsuleCollider2D>();
        playerStates = GetComponentInParent<PlayerStates>();
        playerRb = GetComponentInParent<Rigidbody2D>();
        
        if (config != null)
        {
            platformLayerMask = config.platformLayerMask;
        }
        
        if (playerCollider == null)
            GameLogger.LogError("PLATFORM", "Missing CapsuleCollider2D!");
        if (playerStates == null)
            GameLogger.LogError("PLATFORM", "Missing PlayerStates in parent!");
        if (playerRb == null)
            GameLogger.LogError("PLATFORM", "Missing Rigidbody2D in parent!");
    }

    private void FixedUpdate()
    {
        if (playerCollider == null || playerStates == null || playerRb == null) return;

        // Only allow drop-through if NOT sliding on a cable (or at least not dropping through the cable itself)
        if (playerStates.dropDownHeld && !playerStates.isSlidingOnCable)
        {
            DropThrough();
        }
        else
        {
            StopDropThrough();
        }
    }

    private void DropThrough()
    {
        if (!playerStates.isDroppingDown)
        {
            playerStates.isDroppingDown = true;
            GameLogger.Log("PLATFORM", "DropThrough STARTED");
        }

        IgnoreCurrentPlatforms();
        IgnorePlatformsAhead();
    }

    private void StopDropThrough()
    {
        if (playerStates.isDroppingDown)
        {
            playerStates.isDroppingDown = false;
            GameLogger.Log("PLATFORM", "DropThrough ENDED - Restoring collisions");
            RestoreAllCollisions();
        }
    }

    private void IgnoreCurrentPlatforms()
    {
        Collider2D[] platforms = GetPlatformsInBounds(playerCollider.bounds);
        
        foreach (Collider2D platform in platforms)
        {
            if (ShouldIgnorePlatform(platform))
            {
                IgnorePlatform(platform);
            }
        }
    }

    private void IgnorePlatformsAhead()
    {
        // Only look ahead if falling or moving down
        if (playerRb.linearVelocity.y > 0.1f) return;

        Bounds lookAheadBounds = CalculateLookAheadBounds();
        Collider2D[] platforms = GetPlatformsInBounds(lookAheadBounds);
        
        foreach (Collider2D platform in platforms)
        {
            if (ShouldIgnorePlatform(platform))
            {
                IgnorePlatform(platform);
            }
        }
    }

    private Bounds CalculateLookAheadBounds()
    {
        Vector2 velocity = playerRb.linearVelocity;
        
        // Only look down
        float lookAheadDistance = Mathf.Max(baseLookAhead, Mathf.Abs(velocity.y) * velocityLookAheadMultiplier);
        Vector3 lookAheadOffset = Vector3.down * (lookAheadDistance * 0.5f);
        
        Bounds bounds = playerCollider.bounds;
        bounds.center += lookAheadOffset;
        bounds.size = new Vector3(bounds.size.x + 0.2f, lookAheadDistance, bounds.size.z);
        
        return bounds;
    }

    private Collider2D[] GetPlatformsInBounds(Bounds bounds)
    {
        LayerMask searchMask = platformLayerMask;
        int oneWayLayer = LayerMask.NameToLayer("OneWayPlatform");
        if (oneWayLayer != -1) searchMask |= (1 << oneWayLayer);

        return Physics2D.OverlapBoxAll(bounds.center, bounds.size, 0f, searchMask);
    }

    private bool ShouldIgnorePlatform(Collider2D platform)
    {
        if (platform == null) return false;
        if (platform == playerCollider) return false;
        if (ignoredPlatforms.Contains(platform)) return false;
        
        // PROTECTION: Never ignore the cable we are currently sliding on!
        if (playerStates.isSlidingOnCable && platform == playerStates.currentCable) return false;

        if (!IsPlatform(platform)) return false;
        
        // Only ignore if we are above it (one-way logic)
        if (!IsPlayerAbove(platform)) return false;
        
        return true;
    }

    private bool IsPlatform(Collider2D collider)
    {
        int layer = collider.gameObject.layer;
        if ((platformLayerMask.value & (1 << layer)) != 0)
            return true;
        
        if (collider.transform.parent != null)
        {
            int parentLayer = collider.transform.parent.gameObject.layer;
            if ((platformLayerMask.value & (1 << parentLayer)) != 0)
                return true;
        }
        
        return collider.GetComponent<PlatformEffector2D>() != null || collider.CompareTag("OneWayPlatform");
    }

    private bool IsPlayerAbove(Collider2D platform)
    {
        float playerBottom = playerCollider.bounds.min.y;
        float platformTop = platform.bounds.max.y;
        
        // Add a small buffer to handle fast falling, but keep it strict
        return playerBottom >= platformTop - 0.5f; 
    }

    private void IgnorePlatform(Collider2D platform)
    {
        Physics2D.IgnoreCollision(playerCollider, platform, true);
        ignoredPlatforms.Add(platform);
    }

    private void RestoreAllCollisions()
    {
        foreach (Collider2D platform in ignoredPlatforms)
        {
            if (platform == null) continue;
            
            // If it has a PlatformEffector2D, we can restore IMMEDIATELY.
            // The effector itself will handle the "one-way" logic if we are still inside it.
            if (platform.GetComponent<PlatformEffector2D>() != null)
            {
                RestorePlatformCollision(platform);
                continue;
            }

            bool isOverlapping = playerCollider.bounds.Intersects(platform.bounds);
            
            if (isOverlapping)
            {
                platformsToRestore.Add(platform);
            }
            else
            {
                RestorePlatformCollision(platform);
            }
        }
        
        ignoredPlatforms.Clear();
        
        if (platformsToRestore.Count > 0)
        {
            if (restoreCoroutine != null)
                StopCoroutine(restoreCoroutine);
            
            restoreCoroutine = StartCoroutine(RestoreDelayedPlatforms());
        }
    }

    private void RestorePlatformCollision(Collider2D platform)
    {
        if (platform != null)
        {
            Physics2D.IgnoreCollision(playerCollider, platform, false);
        }
    }

    private IEnumerator RestoreDelayedPlatforms()
    {
        // Much shorter delay for more responsive collision restoration
        float delay = 0.05f; 
        
        while (platformsToRestore.Count > 0)
        {
            yield return new WaitForSeconds(delay);
            
            for (int i = platformsToRestore.Count - 1; i >= 0; i--)
            {
                Collider2D platform = platformsToRestore[i];
                if (platform == null || !playerCollider.bounds.Intersects(platform.bounds))
                {
                    RestorePlatformCollision(platform);
                    platformsToRestore.RemoveAt(i);
                }
            }
        }
        
        restoreCoroutine = null;
    }

    public int IgnoredPlatformCount => ignoredPlatforms.Count;

    public void ForceStop()
    {
        if (playerStates != null)
            playerStates.SetDropDownReleased();
        
        StopDropThrough();
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos || playerCollider == null || playerRb == null) return;
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(playerCollider.bounds.center, playerCollider.bounds.size);
        
        if (playerStates != null && playerStates.isDroppingDown)
        {
            Gizmos.color = Color.cyan;
            Bounds lookAhead = CalculateLookAheadBounds();
            Gizmos.DrawWireCube(lookAhead.center, lookAhead.size);
            
            Vector2 velocity = playerRb.linearVelocity;
            if (velocity.magnitude > 0.1f)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, (Vector2)transform.position + velocity.normalized * baseLookAhead);
            }
        }
        
        Gizmos.color = Color.red;
        foreach (Collider2D platform in ignoredPlatforms)
        {
            if (platform != null)
                Gizmos.DrawWireCube(platform.bounds.center, platform.bounds.size);
        }
    }
}

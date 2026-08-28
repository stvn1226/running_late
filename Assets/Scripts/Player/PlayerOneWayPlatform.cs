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

        // Only allow drop-through if NOT sliding on a cable and NOT staggered
        if (playerStates.dropDownHeld && !playerStates.isSlidingOnCable && !playerStates.isStaggered)
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
        }

        IgnoreCurrentPlatforms();
        IgnorePlatformsAhead();
    }

    private void StopDropThrough()
    {
        if (playerStates.isDroppingDown || ignoredPlatforms.Count > 0)
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
        // Only skip looking ahead if jumping strongly upwards
        if (playerRb.linearVelocity.y > 1.5f) return;

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
        
        // Look down and also look forward in the direction of horizontal movement
        float lookAheadX = Mathf.Max(1.5f, Mathf.Abs(velocity.x) * 0.25f);
        float lookAheadY = Mathf.Max(baseLookAhead, Mathf.Abs(velocity.y) * velocityLookAheadMultiplier);
        
        float dirX = Mathf.Abs(velocity.x) > 0.1f ? Mathf.Sign(velocity.x) : 1f;
        Vector3 lookAheadOffset = new Vector3(dirX * (lookAheadX * 0.5f), -lookAheadY * 0.5f, 0f);
        
        Bounds bounds = playerCollider.bounds;
        bounds.center += lookAheadOffset;
        bounds.size = new Vector3(bounds.size.x + lookAheadX, lookAheadY, bounds.size.z);
        
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
        // Bug 1 fix: Do NOT accept every PlatformEffector2D blindly — that catches cars too.
        // Only accept colliders that are explicitly on the platform layer OR tagged as OneWayPlatform.
        int layer = collider.gameObject.layer;
        if ((platformLayerMask.value & (1 << layer)) != 0)
            return true;
        
        if (collider.transform.parent != null)
        {
            int parentLayer = collider.transform.parent.gameObject.layer;
            if ((platformLayerMask.value & (1 << parentLayer)) != 0)
                return true;
        }
        
        return collider.CompareTag("OneWayPlatform");
    }

    private bool IsPlayerAbove(Collider2D platform)
    {
        float playerBottom = playerCollider.bounds.min.y;
        float platformTop = platform.bounds.max.y;
        
        // Use a generous buffer (4 units) so that slopes are covered:
        // a player at the base of a tall slope still passes this check.
        return playerBottom >= platformTop - 4.0f;
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

            bool isOverlapping = playerCollider.bounds.Intersects(platform.bounds);

            if (isOverlapping)
                platformsToRestore.Add(platform);
            else
                RestorePlatformCollision(platform);
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
        if (platform != null && playerCollider != null)
        {
            Physics2D.IgnoreCollision(playerCollider, platform, false);
            
            // CRITICAL: Unity does not recalculate contacts if IgnoreCollision is set to false
            // while the objects are already overlapping. We must force a recalculation!
            if (playerCollider.enabled)
            {
                playerCollider.enabled = false;
                playerCollider.enabled = true;
            }
        }
    }

    private IEnumerator RestoreDelayedPlatforms()
    {
        float timeout = 0.5f;
        float elapsed = 0f;

        while (platformsToRestore.Count > 0 && elapsed < timeout)
        {
            for (int i = platformsToRestore.Count - 1; i >= 0; i--)
            {
                Collider2D platform = platformsToRestore[i];
                if (platform == null)
                {
                    platformsToRestore.RemoveAt(i);
                    continue;
                }

                // Restore only once the player is no longer overlapping or has fully fallen below the platform top
                bool isStillOverlapping = playerCollider != null && playerCollider.bounds.Intersects(platform.bounds);
                bool isPlayerBelow = playerCollider != null && playerCollider.bounds.max.y <= platform.bounds.max.y;

                if (!isStillOverlapping || (isPlayerBelow && playerRb.linearVelocity.y <= 0f))
                {
                    RestorePlatformCollision(platform);
                    platformsToRestore.RemoveAt(i);
                }
            }

            if (platformsToRestore.Count == 0) break;

            yield return new WaitForSeconds(0.05f);
            elapsed += 0.05f;
        }

        // Final safety cleanup
        foreach (Collider2D platform in platformsToRestore)
        {
            if (platform != null)
                RestorePlatformCollision(platform);
        }

        platformsToRestore.Clear();
        restoreCoroutine = null;
    }

    public int IgnoredPlatformCount => ignoredPlatforms.Count;

    public void ForceStop()
    {
        if (playerStates != null)
        {
            playerStates.dropDownHeld = false;
            playerStates.isDroppingDown = false;
        }
        
        if (restoreCoroutine != null)
        {
            StopCoroutine(restoreCoroutine);
            restoreCoroutine = null;
        }

        foreach (Collider2D platform in ignoredPlatforms)
        {
            if (platform != null && playerCollider != null)
            {
                Physics2D.IgnoreCollision(playerCollider, platform, false);
            }
        }
        ignoredPlatforms.Clear();

        foreach (Collider2D platform in platformsToRestore)
        {
            if (platform != null && playerCollider != null)
            {
                Physics2D.IgnoreCollision(playerCollider, platform, false);
            }
        }
        platformsToRestore.Clear();

        if (playerCollider != null && playerCollider.enabled)
        {
            playerCollider.enabled = false;
            playerCollider.enabled = true;
        }
    }

    private void OnDisable()
    {
        ForceStop();
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

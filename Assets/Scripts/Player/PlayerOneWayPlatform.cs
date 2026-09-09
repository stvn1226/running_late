using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerOneWayPlatform : MonoBehaviour
{
    [Header("Configuration")]
    public PlayerConfig config;
    
    [Header("Settings")]
    // How far horizontally (each side) we scan for platforms while dropping through.
    // Should cover at least 1 full segment width at max run speed.
    [SerializeField] private float horizontalScanRadius = 6f;
    // How far below the player bottom we scan to pre-ignore platforms.
    [SerializeField] private float verticalScanRadius = 3f;
    
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

        // Scan broadly: the player's full body box + generous horizontal and vertical margins.
        // This ensures that even at high run speed, segments ahead are already ignored
        // before the player's body reaches them, preventing the upward push.
        IgnoreAllPlatformsInScanZone();
    }

    private void IgnoreAllPlatformsInScanZone()
    {
        // Build a wide box: full player width + horizontal radius on each side,
        // and from the player bottom downward by verticalScanRadius.
        Bounds playerBounds = playerCollider.bounds;

        float scanWidth  = playerBounds.size.x + horizontalScanRadius * 2f;
        float scanHeight = playerBounds.size.y + verticalScanRadius;

        // Center the box so that it covers the player body + scan zone below
        Vector3 scanCenter = playerBounds.center;
        scanCenter.y -= verticalScanRadius * 0.5f;

        Vector2 scanSize = new Vector2(scanWidth, scanHeight);

        LayerMask searchMask = platformLayerMask;
        int oneWayLayer = LayerMask.NameToLayer("OneWayPlatform");
        if (oneWayLayer != -1) searchMask |= (1 << oneWayLayer);

        Collider2D[] hits = Physics2D.OverlapBoxAll(scanCenter, scanSize, 0f, searchMask);

        foreach (Collider2D platform in hits)
        {
            if (ShouldIgnorePlatform(platform))
            {
                IgnorePlatform(platform);
            }
        }
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

    private bool ShouldIgnorePlatform(Collider2D platform)
    {
        if (platform == null) return false;
        if (platform == playerCollider) return false;
        if (ignoredPlatforms.Contains(platform)) return false;
        
        // Never ignore the cable we are currently sliding on
        if (playerStates.isSlidingOnCable && platform == playerStates.currentCable) return false;

        if (!IsPlatform(platform)) return false;
        
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
        
        return collider.CompareTag("OneWayPlatform");
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

            // Only restore immediately if the player is not overlapping the platform.
            // If still overlapping, queue it for deferred restore to avoid physics pushback.
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
            
            // Force Unity to recalculate contacts after re-enabling collision.
            // Without this, overlapping objects do not generate new contacts.
            if (playerCollider.enabled)
            {
                playerCollider.enabled = false;
                playerCollider.enabled = true;
            }
        }
    }

    // Wait until the player has fully passed below the platform surface before restoring.
    // This eliminates the upward pushback from Unity's physics solver.
    private IEnumerator RestoreDelayedPlatforms()
    {
        float timeout = 0.8f;
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

                bool isOverlapping = playerCollider != null && playerCollider.bounds.Intersects(platform.bounds);

                if (!isOverlapping)
                {
                    RestorePlatformCollision(platform);
                    platformsToRestore.RemoveAt(i);
                }
            }

            if (platformsToRestore.Count == 0) break;

            yield return new WaitForFixedUpdate();
            elapsed += Time.fixedDeltaTime;
        }

        // Timeout safety: restore everything remaining
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
        if (!showGizmos || playerCollider == null) return;
        
        // Draw player collider bounds
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(playerCollider.bounds.center, playerCollider.bounds.size);
        
        if (playerStates != null && playerStates.isDroppingDown)
        {
            // Draw the scan zone
            Bounds playerBounds = playerCollider.bounds;
            float scanWidth  = playerBounds.size.x + horizontalScanRadius * 2f;
            float scanHeight = playerBounds.size.y + verticalScanRadius;
            Vector3 scanCenter = playerBounds.center;
            scanCenter.y -= verticalScanRadius * 0.5f;
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(scanCenter, new Vector3(scanWidth, scanHeight, 0.1f));
        }
        
        // Highlight ignored platforms
        Gizmos.color = Color.red;
        foreach (Collider2D platform in ignoredPlatforms)
        {
            if (platform != null)
                Gizmos.DrawWireCube(platform.bounds.center, platform.bounds.size);
        }
    }
}

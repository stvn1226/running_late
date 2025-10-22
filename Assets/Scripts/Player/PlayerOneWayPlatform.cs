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

        if (playerStates.dropDownHeld)
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
        if (playerStates.isDroppingDown)
        {
            playerStates.isDroppingDown = false;
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
        float speed = velocity.magnitude;
        
        float lookAheadDistance = Mathf.Max(baseLookAhead, speed * velocityLookAheadMultiplier);
        Vector2 lookAheadOffset = velocity.normalized * lookAheadDistance;
        
        Bounds bounds = playerCollider.bounds;
        bounds.center += (Vector3)lookAheadOffset * 0.5f;
        bounds.size += new Vector3(0.5f, lookAheadDistance, 0f);
        
        return bounds;
    }

    private Collider2D[] GetPlatformsInBounds(Bounds bounds)
    {
        return Physics2D.OverlapBoxAll(bounds.center, bounds.size, 0f, platformLayerMask);
    }

    private bool ShouldIgnorePlatform(Collider2D platform)
    {
        if (platform == null) return false;
        if (platform == playerCollider) return false;
        if (ignoredPlatforms.Contains(platform)) return false;
        if (!IsPlatform(platform)) return false;
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
        
        return collider.GetComponent<PlatformEffector2D>() != null;
    }

    private bool IsPlayerAbove(Collider2D platform)
    {
        float playerBottom = playerCollider.bounds.min.y;
        float platformTop = platform.bounds.max.y;
        return playerBottom >= platformTop - 0.2f;
    }

    private void IgnorePlatform(Collider2D platform)
    {
        Physics2D.IgnoreCollision(playerCollider, platform, true);
        ignoredPlatforms.Add(platform);
    }

    private void RestoreAllCollisions()
    {
        platformsToRestore.Clear();
        
        foreach (Collider2D platform in ignoredPlatforms)
        {
            if (platform == null) continue;
            
            bool isOverlapping = playerCollider.bounds.Intersects(platform.bounds);
            bool isPlayerBelow = IsPlayerBelow(platform);
            
            if (isOverlapping && !isPlayerBelow)
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

    private bool IsPlayerBelow(Collider2D platform)
    {
        float playerTop = playerCollider.bounds.max.y;
        float platformBottom = platform.bounds.min.y;
        return playerTop <= platformBottom + 0.1f;
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
        yield return new WaitForSeconds(safetyDelay);
        
        foreach (Collider2D platform in platformsToRestore)
        {
            RestorePlatformCollision(platform);
        }
        
        platformsToRestore.Clear();
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

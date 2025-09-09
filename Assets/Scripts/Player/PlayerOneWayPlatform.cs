using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerOneWayPlatform : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float safetyDelay = 0.1f;
    [SerializeField] private float lookAheadDistance = 2f; // Distance to check ahead for platforms
    [SerializeField] private LayerMask platformLayerMask = -1;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    private BoxCollider2D playerCollider;
    private PlayerStates playerStates;
    private Rigidbody2D playerRigidbody;
    private List<Collider2D> ignoredPlatforms = new List<Collider2D>();
    private bool isDropping = false;
    
    private void Awake()
    {
        playerCollider = GetComponent<BoxCollider2D>();
        playerStates = GetComponentInParent<PlayerStates>();
        playerRigidbody = GetComponentInParent<Rigidbody2D>();
    }
    
    private void Update()
    {
        HandleDropDown();
    }
    
    private void FixedUpdate()
    {
        // Handle proactive collision ignoring during physics updates
        if (playerStates != null && playerStates.dropDownHeld && isDropping)
        {
            ProactivelyIgnorePlatforms();
        }
    }
    
    private void HandleDropDown()
    {
        if (playerStates == null) 
        {
            if (showDebugInfo)
                Debug.LogError("PlayerStates is null!");
            return;
        }
        
        // Debug input states
        if (showDebugInfo)
        {
            if (playerStates.dropDownPressed)
                Debug.Log("DROP DOWN PRESSED detected!");
            if (playerStates.dropDownHeld)
                Debug.Log("DROP DOWN HELD detected!");
        }
        
        // Start dropping when button is pressed
        if (playerStates.dropDownPressed && !isDropping)
        {
            if (showDebugInfo)
                Debug.Log("Attempting to start drop...");
            StartDropThrough();
        }
        
        // Continue dropping while button is held - find new platforms
        if (playerStates.dropDownHeld && isDropping)
        {
            ContinueDropThrough();
        }
        
        // IMMEDIATELY stop dropping when button is released
        if (!playerStates.dropDownHeld && isDropping)
        {
            StopDropThrough();
        }
    }
    
    private void StartDropThrough()
    {
        isDropping = true;
        playerStates.isDroppingDown = true;
        
        // Find all platforms we're currently touching
        FindAndIgnorePlatforms();
        
        // Immediately check ahead for platforms to prevent momentum loss
        ProactivelyIgnorePlatforms();
        
        if (showDebugInfo)
            Debug.Log($"Started dropping through {ignoredPlatforms.Count} platforms");
    }
    
    private void ContinueDropThrough()
    {
        // Keep finding new platforms to drop through while held
        FindAndIgnorePlatforms();
    }
    
    private void StopDropThrough()
    {
        isDropping = false;
        playerStates.isDroppingDown = false;
        
        // IMMEDIATELY restore collision for all platforms
        RestoreAllCollisions();
        
        if (showDebugInfo)
            Debug.Log("Stopped dropping - restored all collisions immediately");
    }
    
    private void ProactivelyIgnorePlatforms()
    {
        if (playerRigidbody == null) return;
        
        Vector2 velocity = playerRigidbody.linearVelocity;
        Vector2 currentPosition = transform.position;
        
        // Calculate look-ahead based on velocity and minimum distance
        float velocityMagnitude = velocity.magnitude;
        float dynamicLookAhead = Mathf.Max(lookAheadDistance, velocityMagnitude * Time.fixedDeltaTime * 3f);
        
        // For very fast movement, extend the look-ahead even further
        if (velocityMagnitude > 10f)
        {
            dynamicLookAhead = Mathf.Max(dynamicLookAhead, velocityMagnitude * 0.2f);
        }
        
        Vector2 lookAheadPosition = currentPosition + velocity.normalized * dynamicLookAhead;
        
        // Create expanded bounds for detection
        Bounds detectionBounds = playerCollider.bounds;
        detectionBounds.center = Vector3.Lerp(currentPosition, lookAheadPosition, 0.5f);
        detectionBounds.size = new Vector3(
            detectionBounds.size.x + 0.5f, // Slightly wider for edge cases
            detectionBounds.size.y + dynamicLookAhead,
            detectionBounds.size.z
        );
        
        // Find all platform colliders in the expanded area
        Collider2D[] nearbyColliders = Physics2D.OverlapBoxAll(
            detectionBounds.center,
            detectionBounds.size,
            0f,
            platformLayerMask
        );
        
        foreach (Collider2D collider in nearbyColliders)
        {
            if (collider == playerCollider) continue;
            
            // Check if this collider is a platform
            if (IsPlatform(collider.gameObject) && !ignoredPlatforms.Contains(collider))
            {
                // Proactively ignore platforms in our path
                Physics2D.IgnoreCollision(playerCollider, collider, true);
                ignoredPlatforms.Add(collider);
                
                if (showDebugInfo)
                    Debug.Log($"Proactively ignoring: {collider.name} (speed: {velocityMagnitude:F2}, lookahead: {dynamicLookAhead:F2})");
            }
        }
    }
    
    private void FindAndIgnorePlatforms()
    {
        // Get all colliders we're currently overlapping
        Collider2D[] overlappingColliders = Physics2D.OverlapBoxAll(
            playerCollider.bounds.center,
            playerCollider.bounds.size,
            0f,
            platformLayerMask
        );
        
        foreach (Collider2D collider in overlappingColliders)
        {
            if (collider == playerCollider) continue;
            
            // Check if this is a platform we can drop through
            if (IsPlatform(collider.gameObject) && !ignoredPlatforms.Contains(collider))
            {
                // Only ignore if player is above or on the platform
                if (IsPlayerAbovePlatform(collider))
                {
                    Physics2D.IgnoreCollision(playerCollider, collider, true);
                    ignoredPlatforms.Add(collider);
                    
                    if (showDebugInfo)
                        Debug.Log($"Ignoring collision with: {collider.name}");
                }
            }
        }
    }
    
    private bool IsPlayerAbovePlatform(Collider2D platformCollider)
    {
        float playerBottom = playerCollider.bounds.min.y;
        float platformTop = platformCollider.bounds.max.y;
        
        // Allow drop through if player is above or slightly overlapping the top
        return playerBottom >= platformTop - 0.2f;
    }
    
    private void RestoreAllCollisions()
    {
        List<Collider2D> overlappingPlatforms = new List<Collider2D>();
        
        // Check each ignored platform
        for (int i = ignoredPlatforms.Count - 1; i >= 0; i--)
        {
            Collider2D platform = ignoredPlatforms[i];
            if (platform == null)
            {
                ignoredPlatforms.RemoveAt(i);
                continue;
            }
            
            // Check if player is still overlapping this platform
            if (playerCollider.bounds.Intersects(platform.bounds))
            {
                // Player is still inside - delay restoration to prevent clipping
                overlappingPlatforms.Add(platform);
                ignoredPlatforms.RemoveAt(i);
                
                if (showDebugInfo)
                    Debug.Log($"Still overlapping {platform.name}, delaying restoration");
            }
            else
            {
                // Player is clear - immediately restore collision
                Physics2D.IgnoreCollision(playerCollider, platform, false);
                ignoredPlatforms.RemoveAt(i);
                
                if (showDebugInfo)
                    Debug.Log($"Immediately restored collision with: {platform.name}");
            }
        }
        
        // Handle overlapping platforms with short delay
        if (overlappingPlatforms.Count > 0)
        {
            StartCoroutine(RestoreOverlappingPlatforms(overlappingPlatforms));
        }
    }
    
    private IEnumerator RestoreOverlappingPlatforms(List<Collider2D> platforms)
    {
        yield return new WaitForSeconds(safetyDelay);
        
        foreach (Collider2D platform in platforms)
        {
            if (platform != null)
            {
                Physics2D.IgnoreCollision(playerCollider, platform, false);
                
                if (showDebugInfo)
                    Debug.Log($"Restored overlapping collision with: {platform.name}");
            }
        }
    }
    
    private bool IsPlatform(GameObject obj)
    {
        // Check the object itself
        if (obj.CompareTag("Roof") || 
            obj.CompareTag("OneWayPlatform") ||
            obj.name.ToLower().Contains("platform") ||
            obj.name.ToLower().Contains("roof"))
        {
            return true;
        }
        
        // Check parent object (for cases where collider is a child of tagged platform)
        Transform parent = obj.transform.parent;
        if (parent != null)
        {
            GameObject parentObj = parent.gameObject;
            if (parentObj.CompareTag("Roof") || 
                parentObj.CompareTag("OneWayPlatform") ||
                parentObj.name.ToLower().Contains("platform") ||
                parentObj.name.ToLower().Contains("roof"))
            {
                return true;
            }
        }
        
        // Check for PlatformEffector2D component (Unity's built-in one-way platforms)
        return obj.GetComponent<PlatformEffector2D>() != null;
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!showDebugInfo) return;
        
        if (playerCollider != null && playerRigidbody != null)
        {
            // Draw current detection bounds
            Gizmos.color = Color.yellow;
            Bounds bounds = playerCollider.bounds;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
            
            // Draw look-ahead area when moving
            Vector2 velocity = playerRigidbody.linearVelocity;
            if (velocity.magnitude > 0.1f && isDropping)
            {
                float velocityMagnitude = velocity.magnitude;
                float dynamicLookAhead = Mathf.Max(lookAheadDistance, velocityMagnitude * Time.fixedDeltaTime * 3f);
                
                if (velocityMagnitude > 10f)
                    dynamicLookAhead = Mathf.Max(dynamicLookAhead, velocityMagnitude * 0.2f);
                
                Vector2 lookAheadPosition = (Vector2)transform.position + velocity.normalized * dynamicLookAhead;
                
                Gizmos.color = Color.cyan;
                Bounds detectionBounds = playerCollider.bounds;
                detectionBounds.center = Vector3.Lerp(transform.position, lookAheadPosition, 0.5f);
                detectionBounds.size = new Vector3(
                    detectionBounds.size.x + 0.5f,
                    detectionBounds.size.y + dynamicLookAhead,
                    detectionBounds.size.z
                );
                Gizmos.DrawWireCube(detectionBounds.center, detectionBounds.size);
                
                // Draw velocity vector
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, lookAheadPosition);
            }
            
            // Draw ignored colliders
            Gizmos.color = Color.red;
            foreach (Collider2D collider in ignoredPlatforms)
            {
                if (collider != null)
                {
                    Gizmos.DrawWireCube(collider.bounds.center, collider.bounds.size);
                }
            }
        }
    }
    
    // Public properties
    public bool IsDropping => isDropping;
    public int IgnoredPlatformCount => ignoredPlatforms.Count;
    
    // Force stop (useful for other systems)
    public void ForceStop()
    {
        if (playerStates != null)
            playerStates.SetDropDownReleased();
        StopDropThrough();
    }
}
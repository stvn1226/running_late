using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerOneWayPlatform : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float safetyDelay = 0.3f;
    [SerializeField] private float lookAheadDistance = 2f;
    [SerializeField] private LayerMask platformLayerMask;
    
    [Header("Configuration")]
    public PlayerConfig config;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    private CapsuleCollider2D playerCollider;
    private PlayerStates playerStates;
    private Rigidbody2D playerRigidbody;
    private List<Collider2D> ignoredPlatforms = new List<Collider2D>();
    
    private void Awake()
    {
        // Component is on child "Collider" object, so get components from self and parent
        playerCollider = GetComponent<CapsuleCollider2D>();
        if (playerCollider == null)
        {
            Debug.LogError("PlayerOneWayPlatform: No CapsuleCollider2D found on this GameObject!");
            return;
        }
        
        // Get parent components (PlayerStates and Rigidbody should be on parent Player GameObject)
        playerStates = GetComponentInParent<PlayerStates>();
        if (playerStates == null)
        {
            Debug.LogError("PlayerOneWayPlatform: No PlayerStates found in parent!");
        }
        
        playerRigidbody = GetComponentInParent<Rigidbody2D>();
        if (playerRigidbody == null)
        {
            Debug.LogError("PlayerOneWayPlatform: No Rigidbody2D found in parent!");
        }
        
        // Use config's platform layer mask if available
        if (config != null)
        {
            platformLayerMask = config.platformLayerMask;
            if (showDebugInfo)
                Debug.Log($"PlayerOneWayPlatform: Using platform layer mask from config: {platformLayerMask.value} (layers: {GetLayerNames(platformLayerMask)})");
        }
        else
        {
            if (showDebugInfo)
                Debug.LogWarning("PlayerOneWayPlatform: No PlayerConfig assigned, using default platform layer mask");
        }
        
        // Debug component setup
        if (showDebugInfo)
        {
            Debug.Log($"PlayerOneWayPlatform setup on '{gameObject.name}' (parent: '{transform.parent?.name}'):");
            Debug.Log($"  - CapsuleCollider2D: {(playerCollider != null ? "✓" : "✗")}");
            Debug.Log($"  - PlayerStates: {(playerStates != null ? "✓" : "✗")}");
            Debug.Log($"  - Rigidbody2D: {(playerRigidbody != null ? "✓" : "✗")}");
            Debug.Log($"  - Config: {(config != null ? "✓" : "✗")}");
        }
    }
    
    private string GetLayerNames(LayerMask layerMask)
    {
        List<string> layerNames = new List<string>();
        for (int i = 0; i < 32; i++)
        {
            if ((layerMask.value & (1 << i)) != 0)
            {
                layerNames.Add(LayerMask.LayerToName(i));
            }
        }
        return string.Join(", ", layerNames);
    }
    
    private void Update()
    {

    }
    
    private void FixedUpdate()
    {
        HandleDropDown();
    }

    private void HandleDropDown()
    {
        if (playerStates == null) 
        {
            if (showDebugInfo)
                Debug.LogError("PlayerStates is null!");
            return;
        }
        
        // Debug input states every frame for 1 second after detecting input
        if (showDebugInfo && playerStates.dropDownHeld)
        {
            Debug.Log($"[FRAME] dropDownHeld: {playerStates.dropDownHeld}, isDroppingDown: {playerStates.isDroppingDown}");
        }
        
        // Start dropping when button is pressed
        if (playerStates.dropDownHeld)
        {
            if (showDebugInfo)
                Debug.Log("=== DROP DOWN PRESSED - DROP SEQUENCE INITIATED ===");
            DropThrough();
        }
                
        // IMMEDIATELY stop dropping when button is released
        if (!playerStates.dropDownHeld)
        {
            if (showDebugInfo)
                Debug.Log("=== DROP DOWN RELEASED - STOPPING DROP SEQUENCE ===");
            StopDropThrough();
        }
    }
    
    private void DropThrough()
    {
        playerStates.isDroppingDown = true;
        
        // Find all platforms we're currently touching
        FindAndIgnorePlatforms();
        
        // Immediately check ahead for platforms to prevent momentum loss
        ProactivelyIgnorePlatforms();
                
        if (showDebugInfo)
            Debug.Log($"Started dropping through {ignoredPlatforms.Count} platforms.");
    }

    private void StopDropThrough()
    {
        playerStates.isDroppingDown = false;
                
        // IMMEDIATELY restore collision for all platforms
        RestoreAllCollisions();
        
        if (showDebugInfo)
            Debug.Log("=== STOPPED DROPPING - RESTORED ALL COLLISIONS ===");
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
        if (showDebugInfo)
            Debug.Log($"=== FindAndIgnorePlatforms START ===");
            
        if (playerCollider == null)
        {
            if (showDebugInfo)
                Debug.LogError("playerCollider is null! Cannot find platforms to ignore.");
            return;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"Player collider bounds: center={playerCollider.bounds.center}, size={playerCollider.bounds.size}");
            Debug.Log($"Platform layer mask: {platformLayerMask.value} (binary: {System.Convert.ToString(platformLayerMask.value, 2)})");
        }
        
        // Get all colliders we're currently overlapping
        Collider2D[] overlappingColliders = Physics2D.OverlapBoxAll(
            playerCollider.bounds.center,
            playerCollider.bounds.size,
            0f,
            platformLayerMask
        );
        
        if (showDebugInfo)
            Debug.Log($"OverlapBoxAll found {overlappingColliders.Length} colliders on platform layers");
        
        if (overlappingColliders.Length == 0)
        {
            if (showDebugInfo)
                Debug.Log("No colliders found on platform layers - checking all colliders manually...");
                
            // Manual check - find all colliders and see what layers they're on
            Collider2D[] allColliders = FindObjectsByType<Collider2D>(FindObjectsSortMode.None);
            if (showDebugInfo)
                Debug.Log($"Found {allColliders.Length} total colliders in scene");
                
            foreach (Collider2D col in allColliders)
            {
                if (col == playerCollider) continue;
                
                bool isOnPlatformLayer = IsLayerInLayerMask(col.gameObject.layer, platformLayerMask);
                bool isOverlapping = playerCollider.bounds.Intersects(col.bounds);
                
                if (showDebugInfo)
                    Debug.Log($"Collider '{col.name}' on layer {col.gameObject.layer} ({LayerMask.LayerToName(col.gameObject.layer)}): onPlatformLayer={isOnPlatformLayer}, overlapping={isOverlapping}");
                    
                if (isOnPlatformLayer && isOverlapping)
                {
                    if (showDebugInfo)
                        Debug.Log($"*** FOUND OVERLAPPING PLATFORM: {col.name} ***");
                }
            }
        }
        
        foreach (Collider2D collider in overlappingColliders)
        {
            if (collider == playerCollider) continue;
            
            if (showDebugInfo)
                Debug.Log($"Checking collider: {collider.name} on layer {LayerMask.LayerToName(collider.gameObject.layer)}");
            
            // Check if this is a platform we can drop through
            if (IsPlatform(collider.gameObject) && !ignoredPlatforms.Contains(collider))
            {
                // Only ignore if player is above or on the platform
                if (IsPlayerAbovePlatform(collider))
                {
                    Physics2D.IgnoreCollision(playerCollider, collider, true);
                    ignoredPlatforms.Add(collider);
                    
                    if (showDebugInfo)
                        Debug.Log($"*** IGNORING COLLISION WITH: {collider.name} ***");
                }
                else if (showDebugInfo)
                {
                    Debug.Log($"Player not above platform: {collider.name}");
                }
            }
            else if (showDebugInfo)
            {
                Debug.Log($"Not a valid platform or already ignored: {collider.name}");
            }
        }
        
        if (showDebugInfo)
            Debug.Log($"=== FindAndIgnorePlatforms END - Ignored {ignoredPlatforms.Count} platforms ===");
    }
    
    private bool IsPlayerAbovePlatform(Collider2D platformCollider)
    {
        float playerBottom = playerCollider.bounds.min.y;
        float platformTop = platformCollider.bounds.max.y;
        
        // Allow drop through if player is above or slightly overlapping the top
        return playerBottom >= platformTop - 0.2f;
    }
    
    private bool IsPlayerBelowPlatform(Collider2D platformCollider)
    {
        float playerTop = playerCollider.bounds.max.y;
        float platformBottom = platformCollider.bounds.min.y;
        
        // Player is considered below if their top is below the platform's bottom
        return playerTop <= platformBottom + 0.1f;
    }
    
    private void RestoreAllCollisions()
    {
        if (showDebugInfo)
            Debug.Log($"=== RestoreAllCollisions START - {ignoredPlatforms.Count} platforms to restore ===");
            
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
            bool isOverlapping = playerCollider.bounds.Intersects(platform.bounds);
            bool isPlayerBelowPlatform = IsPlayerBelowPlatform(platform);
            
            if (showDebugInfo)
                Debug.Log($"Platform {platform.name}: overlapping={isOverlapping}, playerBelow={isPlayerBelowPlatform}");
            
            if (isOverlapping && !isPlayerBelowPlatform)
            {
                // Player is still inside from above - delay restoration to prevent clipping
                overlappingPlatforms.Add(platform);
                ignoredPlatforms.RemoveAt(i);
                
                if (showDebugInfo)
                    Debug.Log($"Still overlapping {platform.name} from above, delaying restoration");
            }
            else
            {
                // Player is clear OR below the platform - immediately restore collision
                Physics2D.IgnoreCollision(playerCollider, platform, false);
                ignoredPlatforms.RemoveAt(i);
                
                if (showDebugInfo)
                    Debug.Log($"*** IMMEDIATELY RESTORED COLLISION WITH: {platform.name} ***");
            }
        }
        
        // Handle overlapping platforms with short delay
        if (overlappingPlatforms.Count > 0)
        {
            if (showDebugInfo)
                Debug.Log($"Starting delayed restoration for {overlappingPlatforms.Count} overlapping platforms");
            StartCoroutine(RestoreOverlappingPlatforms(overlappingPlatforms));
        }
        
        if (showDebugInfo)
            Debug.Log($"=== RestoreAllCollisions END ===");
    }
    
    private IEnumerator RestoreOverlappingPlatforms(List<Collider2D> platforms)
    {
        if (showDebugInfo)
            Debug.Log($"RestoreOverlappingPlatforms: Waiting {safetyDelay} seconds before restoring {platforms.Count} platforms");
            
        yield return new WaitForSeconds(safetyDelay);
        
        foreach (Collider2D platform in platforms)
        {
            if (platform != null)
            {
                // Check one more time if player is still overlapping from above
                bool isOverlapping = playerCollider.bounds.Intersects(platform.bounds);
                bool isPlayerBelowPlatform = IsPlayerBelowPlatform(platform);
                
                if (showDebugInfo)
                    Debug.Log($"Delayed restore check for {platform.name}: overlapping={isOverlapping}, playerBelow={isPlayerBelowPlatform}");
                
                // Restore collision regardless - player has had enough time to clear
                Physics2D.IgnoreCollision(playerCollider, platform, false);
                
                if (showDebugInfo)
                    Debug.Log($"*** DELAYED RESTORED COLLISION WITH: {platform.name} ***");
            }
        }
        
        if (showDebugInfo)
            Debug.Log("RestoreOverlappingPlatforms: All delayed platforms restored");
    }
    
    private bool IsPlatform(GameObject obj)
    {
        // Check if the object is on a platform layer
        int objLayer = obj.layer;
        if (IsLayerInLayerMask(objLayer, platformLayerMask))
        {
            if (showDebugInfo)
                Debug.Log($"Platform detected by layer: {obj.name} on layer {LayerMask.LayerToName(objLayer)}");
            return true;
        }
        
        // Check parent object (for cases where collider is a child of platform on correct layer)
        Transform parent = obj.transform.parent;
        if (parent != null)
        {
            GameObject parentObj = parent.gameObject;
            int parentLayer = parentObj.layer;
            if (IsLayerInLayerMask(parentLayer, platformLayerMask))
            {
                if (showDebugInfo)
                    Debug.Log($"Platform detected by parent layer: {parentObj.name} on layer {LayerMask.LayerToName(parentLayer)}");
                return true;
            }
        }
        
        // Check for PlatformEffector2D component (Unity's built-in one-way platforms)
        bool hasPlatformEffector = obj.GetComponent<PlatformEffector2D>() != null;
        if (hasPlatformEffector && showDebugInfo)
            Debug.Log($"Platform detected by PlatformEffector2D: {obj.name}");
        
        return hasPlatformEffector;
    }
    
    private bool IsLayerInLayerMask(int layer, LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) != 0;
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
            if (velocity.magnitude > 0.1f && playerStates.isDroppingDown)
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
    public int IgnoredPlatformCount => ignoredPlatforms.Count;
    
    // Force stop (useful for other systems)
    public void ForceStop()
    {
        if (playerStates != null)
            playerStates.SetDropDownReleased();
        StopDropThrough();
    }
}
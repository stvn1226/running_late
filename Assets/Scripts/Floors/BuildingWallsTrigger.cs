using UnityEngine;
using Utils;

public class BuildingWallsTrigger : MonoBehaviour
{
    [SerializeField] private BoxCollider2D boxCollider;
    private static bool loggedInputConnection = false;
    [SerializeField] private PlatformEffector2D platformEffector;
    private float surfaceArc = 90f;
    
    private void Start()
    {
        // boxCollider = GetComponentInChildren<BoxCollider2D>();
        
        if (boxCollider == null)
        {
            boxCollider = GetComponent<BoxCollider2D>();
        }

        if (platformEffector == null)
        {
            platformEffector = GetComponent<PlatformEffector2D>();
            platformEffector.surfaceArc = surfaceArc;
        }
        
        ConfigurePlatformEffectors();
        
        if (!loggedInputConnection)
        {
            GameLogger.Log("BUILDING", $"BuildingWallsTrigger initialized - Layer: {LayerMask.LayerToName(gameObject.layer)}, IsTrigger: {boxCollider?.isTrigger}");
            loggedInputConnection = true;
        }
    }

    private void ConfigurePlatformEffectors()
    {
        PlatformEffector2D effector = GetComponent<PlatformEffector2D>();
        effector.colliderMask = 1 << 6; // Solo Player layer
        GameLogger.LogVerbose("BUILDING", $"Configured PlatformEffector2D on {effector.gameObject.name} - ColliderMask set to Player only (layer 5). Vehicles excluded.");        
    }

    // Dead legacy input subscriptions removed: Wall run states are handled directly by player sensors

    public void UpdateColliderDimensions(Vector2 size, Vector2 offset)
    {
        if (boxCollider == null)
        {
            boxCollider = GetComponent<BoxCollider2D>();
        }

        if (boxCollider != null)
        {
            boxCollider.size = size;
            boxCollider.offset = offset;
            GameLogger.LogVerbose("BUILDING", $"Updated {gameObject.name} collider to Size: {size}, Offset: {offset}");
        }
    }

    public void SetSolid(bool solid)
    {
        if (boxCollider == null) return;
        
        boxCollider.isTrigger = !solid;
        
        // Assuming HardWall is layer 14 and SoftWall is 15 based on previous audit
        int hardWallLayer = LayerMask.NameToLayer("HardWall");
        int softWallLayer = LayerMask.NameToLayer("SoftWall");
        
        if (hardWallLayer != -1 && softWallLayer != -1)
        {
            gameObject.layer = solid ? hardWallLayer : softWallLayer;
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(TagUtils.PLAYER_TAG))
        {
            PlayerMovement playerMovement = other.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.SetNearBuildingWall(true);
            }
            GameLogger.LogVerbose("BUILDING", $"PLAYER ENTERED TRIGGER: {gameObject.name}");
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(TagUtils.PLAYER_TAG))
        {
            PlayerMovement playerMovement = other.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.SetNearBuildingWall(false);
            }
            GameLogger.LogVerbose("BUILDING", $"PLAYER EXITED TRIGGER: {gameObject.name}");
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(TagUtils.PLAYER_TAG))
        {
            Vector2 contactPoint = collision.contacts[0].point;
            Vector2 contactNormal = collision.contacts[0].normal;
            GameLogger.LogVerbose("BUILDING", $"PLAYER COLLISION ENTER: {gameObject.name}, Point: {contactPoint}, Normal: {contactNormal}");
        }
    }
    
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(TagUtils.PLAYER_TAG))
        {
            GameLogger.LogVerbose("BUILDING", $"PLAYER COLLISION EXIT: {gameObject.name}");
        }
    }
}

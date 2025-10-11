using UnityEngine;
using UnityEngine.InputSystem;
using Utils;

public class BuildingWallsTrigger : MonoBehaviour
{
    private BoxCollider2D boxCollider;
    private static bool loggedInputConnection = false;

    private void Start()
    {
        boxCollider = GetComponentInChildren<BoxCollider2D>();
        
        if (boxCollider == null)
        {
            boxCollider = GetComponent<BoxCollider2D>();
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

    private void OnEnable()
    {
        if (UIManager.Instance != null && UIManager.Instance.inputActions != null)
        {
            UIManager.Instance.inputActions.Gameplay.WallRun.performed += OnPlayerWallRun;
            UIManager.Instance.inputActions.Gameplay.WallRun.canceled += OnPlayerStopWallRun;
        }
        else
        {
            GameLogger.LogWarning("BUILDING", $"Could not subscribe to input events - UIManager or inputActions null");
        }
    }

    private void OnDisable()
    {
        if (UIManager.Instance != null && UIManager.Instance.inputActions != null)
        {
            UIManager.Instance.inputActions.Gameplay.WallRun.performed -= OnPlayerWallRun;
            UIManager.Instance.inputActions.Gameplay.WallRun.canceled -= OnPlayerStopWallRun;
        }
    }

    private void OnPlayerWallRun(InputAction.CallbackContext context)
    {
        if (boxCollider != null)
        {
            GameLogger.LogVerbose("BUILDING", $"WALL RUN STARTED: {gameObject.name} collider set to SOLID");
            boxCollider.isTrigger = false;
        }
    }

    private void OnPlayerStopWallRun(InputAction.CallbackContext context)
    {
        if (boxCollider != null)
        {
            GameLogger.LogVerbose("BUILDING", $"WALL RUN STOPPED: {gameObject.name} collider set to TRIGGER");
            boxCollider.isTrigger = true;
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

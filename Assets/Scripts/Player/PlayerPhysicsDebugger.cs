using UnityEngine;

public class PlayerPhysicsDebugger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerStates states;
    
    [Header("Logging Settings")]
    [SerializeField] private bool logEveryFrame = false;
    [SerializeField] private bool logPositionChanges = false;
    [SerializeField] private bool logVelocityChanges = false;
    [SerializeField] private bool logCollisions = false;
    [SerializeField] private float significantPositionChange = 1f;
    [SerializeField] private float significantVelocityChange = 2f;
    
    private Vector3 lastPosition;
    private Vector2 lastVelocity;
    private Vector3 spawnPosition;
    private float timeSinceSpawn;
    private int frameCount;
    private bool hasSpawned = false;
    
    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (states == null) states = GetComponent<PlayerStates>();
        
        spawnPosition = transform.position;
        lastPosition = transform.position;
        lastVelocity = rb != null ? rb.linearVelocity : Vector2.zero;
        
        /*
             GameLogger.Log("PLAYER_PHYSICS", "PLAYER AWAKE");
             GameLogger.Log("PLAYER_PHYSICS", $"  Spawn Position: {spawnPosition}");
             GameLogger.Log("PLAYER_PHYSICS", $"  Rigidbody: {(rb != null ? "OK" : "MISSING")}");
             GameLogger.Log("PLAYER_PHYSICS", $"  States: {(states != null ? "OK" : "MISSING")}");
        */

        
        if (rb != null)
        {
            /* 
                   GameLogger.Log("PLAYER_PHYSICS", "  Rigidbody settings:");
                   GameLogger.Log("PLAYER_PHYSICS", $"    Mass: {rb.mass}");
                   GameLogger.Log("PLAYER_PHYSICS", $"    Gravity Scale: {rb.gravityScale}");
                   GameLogger.Log("PLAYER_PHYSICS", $"    Linear Drag: {rb.linearDamping}");
                   GameLogger.Log("PLAYER_PHYSICS", $"    Body Type: {rb.bodyType}");
                   GameLogger.Log("PLAYER_PHYSICS", $"    Constraints: {rb.constraints}");
            */
        }
    }
    
    private void Start()
    {
        hasSpawned = true;
        timeSinceSpawn = 0f;
        frameCount = 0;
        
        /* 
        GameLogger.Log("PLAYER_PHYSICS", "PLAYER START");
        GameLogger.Log("PLAYER_PHYSICS", $"  Position: {transform.position}");
        GameLogger.Log("PLAYER_PHYSICS", $"  Velocity: {(rb != null ? rb.linearVelocity : Vector2.zero)}");
        */
    }
    
    private void FixedUpdate()
    {
        if (!hasSpawned) return;
        
        timeSinceSpawn += Time.fixedDeltaTime;
        frameCount++;
        
        Vector3 currentPosition = transform.position;
        Vector2 currentVelocity = rb != null ? rb.linearVelocity : Vector2.zero;
        
        Vector3 positionDelta = currentPosition - lastPosition;
        Vector2 velocityDelta = currentVelocity - lastVelocity;
        
        float distanceMoved = positionDelta.magnitude;
        float velocityChange = velocityDelta.magnitude;
        
        bool shouldLog = logEveryFrame;
        
        if (!shouldLog && logPositionChanges && distanceMoved > significantPositionChange)
        {
            shouldLog = true;
            // GameLogger.LogWarning("PLAYER_PHYSICS", $"LARGE POSITION CHANGE: {distanceMoved:F2} units in one frame");
        }
        
        if (!shouldLog && logVelocityChanges && velocityChange > significantVelocityChange)
        {
            shouldLog = true;
            // GameLogger.LogWarning("PLAYER_PHYSICS", $"LARGE VELOCITY CHANGE: {velocityChange:F2} units/s in one frame");
        }
        
        if (shouldLog || frameCount <= 10 || (frameCount % 50 == 0))
        {
            LogPhysicsState(currentPosition, currentVelocity, positionDelta, velocityDelta);
        }
        
        if (currentPosition.y < -50f)
        {
            /*
            GameLogger.LogError("PLAYER_PHYSICS", $"PLAYER FALLING INFINITELY - Y={currentPosition.y:F2}");
            GameLogger.LogError("PLAYER_PHYSICS", $"  Time since spawn: {timeSinceSpawn:F2}s");
            GameLogger.LogError("PLAYER_PHYSICS", $"  Spawn position: {spawnPosition}");
            GameLogger.LogError("PLAYER_PHYSICS", $"  Current velocity: {currentVelocity}");
            GameLogger.LogError("PLAYER_PHYSICS", $"  Is grounded: {(states != null ? states.isGrounded : false)}");
            */
        }
        
        lastPosition = currentPosition;
        lastVelocity = currentVelocity;
    }
    
    private void LogPhysicsState(Vector3 position, Vector2 velocity, Vector3 posDelta, Vector2 velDelta)
    {
        string stateInfo = "";
        if (states != null)
        {
            stateInfo = $"Grounded={states.isGrounded}, WallRun={states.isWallRunning}, Slide={states.isSliding}, Kick={states.isKicking}";
        }
        /*
                GameLogger.Log("PLAYER_PHYSICS", $"[Frame {frameCount}, T={timeSinceSpawn:F2}s]");
                GameLogger.Log("PLAYER_PHYSICS", $"  Pos: {position} (Delta={posDelta})");
                GameLogger.Log("PLAYER_PHYSICS", $"  Vel: {velocity} (Delta={velDelta})");
                GameLogger.Log("PLAYER_PHYSICS", $"  State: {stateInfo}");
                GameLogger.Log("PLAYER_PHYSICS", $"  Dist from spawn: {Vector3.Distance(position, spawnPosition):F2}");
        */
       
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!logCollisions) return;
        
        Vector2 normal = Vector2.zero;
        if (collision.contactCount > 0)
        {
            normal = collision.GetContact(0).normal;
        }
        /*
        GameLogger.Log("PLAYER_PHYSICS", $"COLLISION ENTER: {collision.gameObject.name}");
        GameLogger.Log("PLAYER_PHYSICS", $"  Layer: {LayerMask.LayerToName(collision.gameObject.layer)}");
        GameLogger.Log("PLAYER_PHYSICS", $"  Normal: {normal}");
        GameLogger.Log("PLAYER_PHYSICS", $"  Contact count: {collision.contactCount}");
        GameLogger.Log("PLAYER_PHYSICS", $"  Velocity before: {rb.linearVelocity}");
        */
       
    }
    
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (!logCollisions) return;
        
        GameLogger.Log("PLAYER_PHYSICS", $"COLLISION EXIT: {collision.gameObject.name}");
        GameLogger.Log("PLAYER_PHYSICS", $"  Velocity after: {rb.linearVelocity}");
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!logCollisions) return;
        
        /*
            GameLogger.Log("PLAYER_PHYSICS", $"TRIGGER ENTER: {other.gameObject.name}");
            GameLogger.Log("PLAYER_PHYSICS", $"  Layer: {LayerMask.LayerToName(other.gameObject.layer)}");
        */
        
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (!logCollisions) return;
        
        /*
            GameLogger.Log("PLAYER_PHYSICS", $"TRIGGER EXIT: {other.gameObject.name}");
        */
        
    }
    
    public void LogManualEvent(string eventName, string details = "")
    {
        GameLogger.Log("PLAYER_PHYSICS", $"EVENT: {eventName}");
        if (!string.IsNullOrEmpty(details))
        {
            GameLogger.Log("PLAYER_PHYSICS", $"  {details}");
        }
        GameLogger.Log("PLAYER_PHYSICS", $"  Position: {transform.position}");
        GameLogger.Log("PLAYER_PHYSICS", $"  Velocity: {(rb != null ? rb.linearVelocity : Vector2.zero)}");
    }
}

using System.Collections;
using UnityEngine;
using Utils;

public abstract class Vehicle : MonoBehaviour
{
    private Rigidbody2D rb;
    private Camera mainCamera;
    private PlayerMovement cachedPlayerMovement;
    [SerializeField] private float speed = 5f;
    private bool isRunning;
    private bool hasRan;
    public Collider2D[] hitbox = new Collider2D[2];
    [SerializeField] private float activationDistance = 30f;
    private bool wasStomped;

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = GameReferences.Instance.MainCamera;
        isRunning = false;
        hasRan = false;
        wasStomped = false;
    }

    protected virtual void Update()
    {
        if (mainCamera == null) return;
        
        if (Mathf.Abs(transform.position.x - mainCamera.transform.position.x) > activationDistance)
        {
            isRunning = false;
        }
        else
        {
            isRunning = true;
            hasRan = true;
        }

        if (hasRan && !isRunning && Mathf.Abs(transform.position.x - mainCamera.transform.position.x) > 40f)
        {
            Destroy(gameObject);
        }

        if (!isRunning) return;

        rb.linearVelocityX = -speed;
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isRunning) return;
        
        if (collision.gameObject.CompareTag(TagUtils.PLAYER_TAG))
        {
            GameLogger.Log("VEHICLE", $"[{gameObject.name}] Player collision detected");
            
            Rigidbody2D playerRb = collision.GetComponentInParent<Rigidbody2D>();
            
            if (playerRb != null)
            {
                float velocityY = playerRb.linearVelocity.y;
                
                bool isStomp = velocityY < 0f;

                GameLogger.Log("VEHICLE", $"[{gameObject.name}] VelY={velocityY:F2}, Result={(isStomp ? "STOMP" : "DAMAGE")}");

                if (isStomp)
                {
                    HandleStompOnVehicle(collision, playerRb);
                }
                else
                {
                    HandlePlayerDamage(collision, playerRb);
                }
            }
            else
            {
                GameLogger.LogError("VEHICLE", $"[{gameObject.name}] No Rigidbody2D found on player!");
                HandlePlayerDamage(collision, null);
            }
        }
    }

    private void HandleStompOnVehicle(Collider2D collision, Rigidbody2D playerRb)
    {
        if (wasStomped) return;
        
        GameLogger.Log("VEHICLE", $"[{gameObject.name}] ✓ STOMPED (VelY: {playerRb.linearVelocity.y:F2}) - bouncing!");
        
        wasStomped = true;

        PlayerStomp playerStomp = collision.GetComponentInParent<PlayerStomp>();
        
        if (playerStomp != null)
        {
            playerStomp.ExecuteStompBounce();
        }
        else
        {
            GameLogger.LogWarning("VEHICLE", $"[{gameObject.name}] PlayerStomp component not found!");
        }

        if (AnalyticsManager.Instance != null)
        {
            Vector3 playerPos = playerRb.transform.position;
            float velocityX = Mathf.Abs(playerRb.linearVelocity.x);

            var context = AnalyticsEvents.CreatePlayerActionContext(
                AnalyticsEvents.PLAYER_STOMP,
                playerPos.x,
                velocityX
            );
            context["obstacle_type"] = this.GetType().Name;
            context["velocity_y"] = playerRb.linearVelocity.y;

            AnalyticsManager.Instance.TrackPlayerAction(AnalyticsEvents.PLAYER_STOMP, context);
        }

        StartCoroutine(DisableHitboxTemp(2f));
    }

    private void HandlePlayerDamage(Collider2D collision, Rigidbody2D playerRb)
    {
        float velocityY = playerRb != null ? playerRb.linearVelocity.y : 0f;
        GameLogger.Log("VEHICLE", $"[{gameObject.name}] ✗ DAMAGE (VelY: {velocityY:F2})");

        if (AnalyticsManager.Instance != null)
        {
            if (cachedPlayerMovement == null)
                cachedPlayerMovement = GameReferences.Instance.PlayerMovement;
        
            float playerSpeed = 0f;
            if (cachedPlayerMovement != null)
            {
                Rigidbody2D rb = cachedPlayerMovement.GetComponent<Rigidbody2D>();
                playerSpeed = rb != null ? Mathf.Abs(rb.linearVelocity.x) : 0f;
            }
        
            AnalyticsManager.Instance.TrackEvent(AnalyticsEvents.OBSTACLE_HIT, new System.Collections.Generic.Dictionary<string, object>
            {
                { "obstacle_type", this.GetType().Name },
                { "position_x", transform.position.x },
                { "player_speed", playerSpeed },
                { "player_velocity_y", velocityY }
            });
        }
        
        if (cachedPlayerMovement == null)
            cachedPlayerMovement = GameReferences.Instance.PlayerMovement;
        
        if (cachedPlayerMovement != null)
            cachedPlayerMovement.StaggerPlayer(new Vector2(-1, 1));
        
        StartCoroutine(DisableHitboxTemp(2f));
    }

    protected IEnumerator DisableHitboxTemp(float duration)
    {
        foreach (Collider2D c in hitbox)
        {
            c.enabled = false;
        }
        yield return new WaitForSeconds(duration);
        foreach (Collider2D c in hitbox)
        {
            c.enabled = true;
        }
    }
}

using System.Collections;
using UnityEngine;
using Utils;

public abstract class Vehicle : MonoBehaviour, IWarningIndicator
{
    protected Rigidbody2D rb;
    private Camera mainCamera;
    private PlayerMovement cachedPlayerMovement;
    [SerializeField] private float speed = 5f;
    private bool isRunning;
    private bool hasRan;
    public Collider2D[] hitbox = new Collider2D[2];
    [SerializeField] private float activationDistance = 30f;
    private bool wasStomped;
    private bool wasHit;
    private Coroutine hitboxDisableCoroutine;
    protected bool movementOverridden;
    private float baseGroundedDistance = -1f;
    
    [Header("Warning Indicator")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = GameReferences.Instance.MainCamera;
        isRunning = false;
        hasRan = false;
        wasStomped = false;
        wasHit = false;
        
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        // Measure resting distance to ground
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 5f);
        if (hit.collider != null && !hit.collider.isTrigger)
        {
            baseGroundedDistance = hit.distance;
        }
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
        if (movementOverridden) return;

        // Detect slopes and adjust velocity/rotation accordingly
        RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, Vector2.down, 5f);
        RaycastHit2D validHit = new RaycastHit2D();
        bool foundGround = false;

        foreach (var h in hits)
        {
            if (h.collider != null && !h.collider.isTrigger && h.collider.gameObject != gameObject && !h.collider.transform.IsChildOf(transform))
            {
                validHit = h;
                foundGround = true;
                break;
            }
        }

        if (baseGroundedDistance < 0 && foundGround && validHit.normal.y > 0.9f)
        {
            baseGroundedDistance = validHit.distance;
        }

        float expectedDist = (baseGroundedDistance > 0 ? baseGroundedDistance : 1.0f) / (validHit.normal.y > 0.1f ? validHit.normal.y : 1f);
        float maxAllowedDistance = expectedDist + 0.8f;
        
        bool isCloseToGround = foundGround && validHit.distance <= maxAllowedDistance;

        if (isCloseToGround && validHit.normal.y > 0.3f)
        {
            Vector2 leftTangent = new Vector2(-validHit.normal.y, validHit.normal.x);
            Vector2 targetVel = leftTangent * speed;

            float distanceError = validHit.distance - expectedDist;

            // Dynamically pull the vehicle down to the ground if it floats
            if (distanceError > 0.05f)
            {
                targetVel.y -= (distanceError * 15f);
            }

            // Apply a downward "glue" force only when moving flat or downhill.
            // This prevents the vehicle from flying off the crest of a hill.
            if (leftTangent.y <= 0.1f)
            {
                targetVel.y -= 4f;
            }

            rb.linearVelocity = targetVel;

            float targetAngle = Mathf.Atan2(validHit.normal.y, validHit.normal.x) * Mathf.Rad2Deg - 90f;
            Quaternion targetRot = Quaternion.Euler(0, 0, targetAngle);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * 15f);
        }
        else
        {
            // In the air or flat ground - let gravity act naturally on Y
            rb.linearVelocityX = -speed;
            
            // Smoothly rotate back to flat if we're falling
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.identity, Time.deltaTime * 10f);
        }
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isRunning) return;

        //if (wasHit)
        //{
        //    GameLogger.LogVerbose("VEHICLE", $"[{gameObject.name}] Ignoring collision - recently hit (cooldown active)");
        //    return;
        //}
        
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
                    PlayerStates playerStates = collision.GetComponentInParent<PlayerStates>();
                    
                    if (playerStates != null && playerStates.isInvincible && !playerStates.isShielded)
                    {
                        GameLogger.Log("VEHICLE", $"[{gameObject.name}] Player is INVULNERABLE - ignoring damage (no stomp)");
                        return;
                    }
                    
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
        wasHit = true;

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

        //hitboxDisableCoroutine = StartCoroutine(DisableHitboxTemp(2f));
    }

    private void HandlePlayerDamage(Collider2D collision, Rigidbody2D playerRb)
    {
        float velocityY = playerRb != null ? playerRb.linearVelocity.y : 0f;

        if (cachedPlayerMovement == null)
            cachedPlayerMovement = GameReferences.Instance.PlayerMovement;

        PlayerStates playerStates = collision.GetComponentInParent<PlayerStates>();
        bool isInvincible = playerStates != null && playerStates.isInvincible;
        bool isShielded = playerStates != null && playerStates.isShielded;

        if (isInvincible || isShielded)
        {
            GameLogger.Log("VEHICLE", $"[{gameObject.name}] Player is INVINCIBLE - no damage dealt!");

            playerStates.RemoveShield();
            wasHit = true;

            if (AnalyticsManager.Instance != null)
            {
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
                { "player_velocity_y", velocityY },
                { "player_invincible", true }
            });
            }

            //hitboxDisableCoroutine = StartCoroutine(DisableHitboxTemp(2f));
            return;
        }

        GameLogger.Log("VEHICLE", $"[{gameObject.name}] >> DAMAGE (VelY: {velocityY:F2})");

        wasHit = true;

        if (AnalyticsManager.Instance != null)
        {
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

        if (cachedPlayerMovement != null)
        {
            float knockbackX = playerRb != null ? Mathf.Sign(playerRb.transform.position.x - transform.position.x) : -1f;
            cachedPlayerMovement.StaggerPlayer(new Vector2(knockbackX, 1f));
        }

        OnPlayerDamageDealt();

        //hitboxDisableCoroutine = StartCoroutine(DisableHitboxTemp(2f));
    }

    /// <summary>
    /// Called after the vehicle successfully deals damage to the player. Override to add vehicle-specific reactions.
    /// </summary>
    protected virtual void OnPlayerDamageDealt() { }

    private void ResetHitState()
    {
        wasHit = false;
        GameLogger.LogVerbose("VEHICLE", $"[{gameObject.name}] Hit state RESET");
    }

    protected IEnumerator DisableHitboxTemp(float duration)
    {
        GameLogger.LogVerbose("VEHICLE", $"[{gameObject.name}] Hitboxes DISABLED for {duration}s");
        
        foreach (Collider2D c in hitbox)
        {
            if (c != null) c.enabled = false;
        }
        
        yield return new WaitForSeconds(duration);
        
        if (this != null && !wasStomped)
        {
            foreach (Collider2D c in hitbox)
            {
                if (c != null)
                {
                    c.enabled = true;
                    GameLogger.LogVerbose("VEHICLE", $"[{gameObject.name}] Hitbox {c.name} RE-ENABLED");
                }
            }
            ResetHitState();
            GameLogger.LogVerbose("VEHICLE", $"[{gameObject.name}] Hit state reset after cooldown");
        }

        hitboxDisableCoroutine = null;
    }

    protected virtual void OnDisable()
    {
        foreach (Collider2D c in hitbox)
        {
            if (c != null) c.enabled = true;
        }
        ResetHitState();
    }

    protected virtual void OnDestroy()
    {
        if (hitboxDisableCoroutine != null)
        {
            StopCoroutine(hitboxDisableCoroutine);
        }
        StopAllCoroutines();
    }
    
    public virtual Sprite GetWarningSprite()
    {
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            return spriteRenderer.sprite;
        }
        return null;
    }
    
    public virtual Vector3 GetPosition()
    {
        return transform.position;
    }
    
    public virtual bool ShouldShowWarning()
    {
        return !wasStomped;
    }
}

using UnityEngine;

public class PlayerKickColliderManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PlayerStates states;
    [SerializeField] private BoxCollider2D kickBox;

    [Header("Timing")]
    [SerializeField] private float kickActivationDelay = 0.1f;

    private float kickActivationTimer = 0f;
    private bool wasKicking = false;

    private void Awake()
    {
        ValidateDependencies();
    }

    private void Start()
    {
        if (kickBox != null)
        {
            kickBox.enabled = false;
        }
    }

    private void Update()
    {
        if (states == null || kickBox == null) return;

        bool isKicking = states.isKicking;

        if (isKicking && !wasKicking)
        {
            kickActivationTimer = kickActivationDelay;
            GameLogger.Log("KICK_COLL", $"Kick started - delay={kickActivationDelay:F2}s, isGrounded={states.isGrounded}");
        }
        else if (!isKicking && wasKicking)
        {
            kickBox.enabled = false;
            kickActivationTimer = 0f;
            GameLogger.Log("KICK_COLL", "Kick ended - collider DISABLED");
        }

        if (isKicking && kickActivationTimer > 0f)
        {
            kickActivationTimer -= Time.deltaTime;
            
            if (kickActivationTimer <= 0f)
            {
                kickBox.enabled = true;
                GameLogger.Log("KICK_COLL", "Collider ENABLED after delay");
                
                TrackKickAnalytics();
            }
        }

        wasKicking = isKicking;
    }

    private void TrackKickAnalytics()
    {
        if (AnalyticsManager.Instance != null)
        {
            Rigidbody2D rb = GetComponentInParent<Rigidbody2D>();
            Vector3 playerPos = transform.position;
            float velocityX = rb != null ? rb.linearVelocity.x : 0f;
            
            var context = AnalyticsEvents.CreatePlayerActionContext(
                AnalyticsEvents.PLAYER_KICK, 
                playerPos.x, 
                velocityX
            );
            
            AnalyticsManager.Instance.TrackPlayerAction(AnalyticsEvents.PLAYER_KICK, context);
        }
    }

    private void ValidateDependencies()
    {
        if (states == null)
        {
            states = GetComponentInParent<PlayerStates>();
            if (states == null)
            {
                GameLogger.LogError("KICK_COLL", "PlayerStates not found in parent!");
            }
        }

        if (kickBox == null)
        {
            kickBox = GetComponent<BoxCollider2D>();
            if (kickBox == null)
            {
                GameLogger.LogError("KICK_COLL", "BoxCollider2D not found on this GameObject!");
            }
        }
    }
}

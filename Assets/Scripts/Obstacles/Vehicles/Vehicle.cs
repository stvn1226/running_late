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

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = GameReferences.Instance.MainCamera;
        isRunning = false;
        hasRan = false;
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
            GameLogger.Log("VEHICLE", "Vehicle collided with Player");
            
            if (AnalyticsManager.Instance != null)
            {
                if (cachedPlayerMovement == null)
                    cachedPlayerMovement = GameReferences.Instance.PlayerMovement;
            
                float playerSpeed = 0f;
                if (cachedPlayerMovement != null)
                {
                    Rigidbody2D playerRb = cachedPlayerMovement.GetComponent<Rigidbody2D>();
                    playerSpeed = playerRb != null ? Mathf.Abs(playerRb.linearVelocity.x) : 0f;
                }
            
                AnalyticsManager.Instance.TrackEvent(AnalyticsEvents.OBSTACLE_HIT, new System.Collections.Generic.Dictionary<string, object>
                {
                    { "obstacle_type", this.GetType().Name },
                    { "position_x", transform.position.x },
                    { "player_speed", playerSpeed }
                });
            }
            
            if (cachedPlayerMovement == null)
                cachedPlayerMovement = GameReferences.Instance.PlayerMovement;
            
            if (cachedPlayerMovement != null)
                cachedPlayerMovement.StaggerPlayer(new Vector2(-1, 1));
            
            StartCoroutine(DisableHitboxTemp(2f));
        }
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

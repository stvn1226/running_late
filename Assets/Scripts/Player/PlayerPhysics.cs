using UnityEngine;

public class PlayerPhysics : MonoBehaviour
{
    [Header("References")]
    public PlayerConfig config;
    
    private CapsuleCollider2D playerCollider;
    private PlayerStates states;
    private Vector2 originalColliderSize;
    private Vector2 originalColliderOffset;
    private Vector2 crouchColliderSize;
    private Vector2 crouchColliderOffset;

    private void Awake()
    {
        playerCollider = GetComponentInChildren<CapsuleCollider2D>();
        if (playerCollider == null)
        {
            Debug.LogError("PlayerPhysics: No CapsuleCollider2D found in children!");
        }
        states = GetComponent<PlayerStates>();
        
        originalColliderSize = playerCollider.size;
        originalColliderOffset = playerCollider.offset;
    }

    private void Start()
    {
        if (config != null)
        {
            crouchColliderSize = new Vector2(
                originalColliderSize.x * config.crouchSizeMultiplier.x, 
                originalColliderSize.y * config.crouchSizeMultiplier.y
            );
            
            crouchColliderOffset = new Vector2(
                originalColliderOffset.x, 
                originalColliderOffset.y - (originalColliderSize.y - crouchColliderSize.y) * 0.5f
            );
        }
    }

    private void Update()
    {
        CheckGrounded();
        CheckWallRun();
    }

    private void CheckGrounded()
    {
        if (config == null) return;

        Vector2 boxCenter = (Vector2)transform.position + playerCollider.offset;
        Vector2 boxSize = playerCollider.size;

        RaycastHit2D hit = Physics2D.BoxCast(boxCenter, boxSize, 0f, Vector2.down, config.groundCheckDistance, config.groundLayer);
        
        states.wasGrounded = states.isGrounded;
        states.isGrounded = hit.collider != null && GetComponent<Rigidbody2D>().linearVelocity.y <= 0.1f;
    }

    private void CheckWallRun()
    {
        if (config == null) return;

        if (states.isGrounded)
        {
            states.canWallRun = false;
            return;
        }

        Vector2 boxCenter = (Vector2)transform.position + playerCollider.offset;
        Vector2 boxSize = new Vector2(config.wallCheckDistance, playerCollider.size.y);

        // Check right wall
        RaycastHit2D rightHit = Physics2D.BoxCast(boxCenter, boxSize, 0f, Vector2.right, config.wallCheckDistance, config.wallLayer);
        // Check left wall
        RaycastHit2D leftHit = Physics2D.BoxCast(boxCenter, boxSize, 0f, Vector2.left, config.wallCheckDistance, config.wallLayer);

        if (rightHit.collider != null && states.moveInput.x > 0.1f)
        {
            states.canWallRun = true;
            states.wallDirection = 1; // Right wall
        }
        else if (leftHit.collider != null && states.moveInput.x < -0.1f)
        {
            states.canWallRun = true;
            states.wallDirection = -1; // Left wall
        }
        else
        {
            states.canWallRun = false;
        }
    }

    public void UpdateColliderSize()
    {
        if (states.isCrouching || states.isSliding)
        {
            playerCollider.direction = CapsuleDirection2D.Horizontal;
            playerCollider.size = crouchColliderSize;
            playerCollider.offset = crouchColliderOffset;
        }
        else
        {
            playerCollider.direction = CapsuleDirection2D.Vertical;
            playerCollider.size = originalColliderSize;
            playerCollider.offset = originalColliderOffset;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (playerCollider == null || config == null) return;

        Vector2 boxCenter = (Vector2)transform.position + playerCollider.offset;
        Vector2 boxSize = playerCollider.size;

        // Draw ground check
        Gizmos.color = states != null && states.isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireCube(boxCenter + Vector2.down * config.groundCheckDistance, boxSize);

        // Draw wall check
        Gizmos.color = states != null && states.canWallRun ? Color.blue : Color.gray;
        Vector2 wallCheckSize = new Vector2(config.wallCheckDistance, boxSize.y);
        Gizmos.DrawWireCube(boxCenter + Vector2.right * config.wallCheckDistance, wallCheckSize);
        Gizmos.DrawWireCube(boxCenter + Vector2.left * config.wallCheckDistance, wallCheckSize);
    }
}
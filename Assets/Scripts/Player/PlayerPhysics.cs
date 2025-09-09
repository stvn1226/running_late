using UnityEngine;

public class PlayerPhysics : MonoBehaviour
{
    [Header("References")]
    public PlayerConfig config;
    
    private BoxCollider2D boxCollider;
    private PlayerStates states;
    private Vector2 originalColliderSize;
    private Vector2 originalColliderOffset;
    private Vector2 crouchColliderSize;
    private Vector2 crouchColliderOffset;

    private void Awake()
    {
        boxCollider = GetComponentInChildren<BoxCollider2D>();
        states = GetComponent<PlayerStates>();
        
        originalColliderSize = boxCollider.size;
        originalColliderOffset = boxCollider.offset;
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

        Vector2 boxCenter = (Vector2)transform.position + boxCollider.offset;
        Vector2 boxSize = boxCollider.size;

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

        Vector2 boxCenter = (Vector2)transform.position + boxCollider.offset;
        Vector2 boxSize = new Vector2(config.wallCheckDistance, boxCollider.size.y);

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
            boxCollider.size = crouchColliderSize;
            boxCollider.offset = crouchColliderOffset;
        }
        else
        {
            boxCollider.size = originalColliderSize;
            boxCollider.offset = originalColliderOffset;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (boxCollider == null || config == null) return;

        Vector2 boxCenter = (Vector2)transform.position + boxCollider.offset;
        Vector2 boxSize = boxCollider.size;

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
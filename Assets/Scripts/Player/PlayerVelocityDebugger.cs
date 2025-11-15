using UnityEngine;

public class PlayerVelocityDebugger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;

    [Header("Gizmo Settings")]
    [SerializeField] private bool showVelocityGizmo = true;
    [SerializeField] private float velocityScale = 0.5f;
    [SerializeField] private Color velocityColor = Color.green;
    [SerializeField] private Color negativeYColor = Color.red;
    [SerializeField] private Color positiveYColor = Color.cyan;

    [Header("Debug Info")]
    [SerializeField] private bool showDebugText = true;
    [SerializeField] private bool logVelocityChanges = false;
    private Vector2 lastVelocity;

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }
    }

    private void Update()
    {
        if (rb == null) return;

        if (logVelocityChanges)
        {
            Vector2 currentVel = rb.linearVelocity;
            if (Mathf.Abs(currentVel.y - lastVelocity.y) > 0.1f)
            {
                GameLogger.LogVerbose("VELOCITY", $"Y Velocity: {currentVel.y:F2} | X: {currentVel.x:F2}");
                lastVelocity = currentVel;
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!showVelocityGizmo || rb == null) return;

        Vector2 velocity = rb.linearVelocity;
        Vector3 position = transform.position;

        Color arrowColor = velocity.y < 0 ? negativeYColor : positiveYColor;
        Gizmos.color = arrowColor;

        Vector3 velocityVector = velocity * velocityScale;
        Gizmos.DrawLine(position, position + velocityVector);
        
        DrawArrowHead(position, position + velocityVector, 0.3f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(position, 0.2f);

        if (showDebugText)
        {
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(position + Vector3.up * 2f, 
                $"Vel Y: {velocity.y:F2}\nVel X: {velocity.x:F2}\nFalling: {velocity.y < 0}");
            #endif
        }
    }

    private void DrawArrowHead(Vector3 from, Vector3 to, float size)
    {
        Vector3 direction = (to - from).normalized;
        Vector3 right = Vector3.Cross(direction, Vector3.forward) * size;
        Vector3 arrowTip = to;
        
        Gizmos.DrawLine(arrowTip, arrowTip - direction * size + right);
        Gizmos.DrawLine(arrowTip, arrowTip - direction * size - right);
    }
}

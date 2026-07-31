using UnityEngine;
using UnityEngine.InputSystem;

public class PanCamObject : MonoBehaviour
{
    [Header("Configuration")]
    public PlayerConfig config;

    [Header("Input Settings")]
    [Range(0f, 0.3f)]
    [Tooltip("Dead zone for stick input to prevent drift")]
    public float inputDeadzone = 0.15f;

    [Header("Debug")]
    public bool showDebug = false;

    private Vector2 panInput;
    private Vector2 currentVelocity;
    private Vector2 targetLocalPosition = Vector2.zero;

    private PlayerInputActions inputActions;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        inputActions.Gameplay.PanCamera.Enable();
        inputActions.Gameplay.PanCamera.performed += OnPanInput;
        inputActions.Gameplay.PanCamera.canceled += OnPanInput;
        
        if (showDebug)
            Debug.Log("[PanCam] Successfully subscribed to PanCamera input");
    }

    private void OnDisable()
    {
        inputActions.Gameplay.PanCamera.performed -= OnPanInput;
        inputActions.Gameplay.PanCamera.canceled -= OnPanInput;
        inputActions.Gameplay.PanCamera.Disable();
    }

    private void OnPanInput(InputAction.CallbackContext context)
    {
        Vector2 rawInput = context.ReadValue<Vector2>();
        
        if (rawInput.magnitude < inputDeadzone)
        {
            panInput = Vector2.zero;
        }
        else
        {
            panInput = rawInput;
        }
        
        if (showDebug)
            Debug.Log($"[PanCam] Raw Input: {rawInput} | Pan Input: {panInput}");
    }

    void Update()
    {
        UpdateTargetPosition();
        ApplyMovement();
    }

    private void UpdateTargetPosition()
    {
        if (panInput.magnitude < inputDeadzone)
        {
            targetLocalPosition = Vector2.zero;
            return;
        }

        Vector2 normalizedInput = panInput.normalized;

        if (config.invertXAxis) normalizedInput.x = -normalizedInput.x;
        if (config.invertYAxis) normalizedInput.y = -normalizedInput.y;

        float inputMagnitude = Mathf.Clamp01((panInput.magnitude - inputDeadzone) / (1f - inputDeadzone));
        
        targetLocalPosition = normalizedInput * (config.panAmount * inputMagnitude);
        
        if (showDebug && panInput.magnitude > inputDeadzone)
        {
            Debug.Log($"[PanCam] Target: {targetLocalPosition} | X: {targetLocalPosition.x:F2} | Y: {targetLocalPosition.y:F2}");
        }
    }

    private void ApplyMovement()
    {
        bool isPanning = panInput.magnitude > inputDeadzone;
        float smoothTime = isPanning ? (1f / config.panSpeed) : (1f / config.returnSpeed);

        Vector2 previousPos = transform.localPosition;

        transform.localPosition = Vector2.SmoothDamp(
            transform.localPosition,
            targetLocalPosition,
            ref currentVelocity,
            smoothTime
        );

        if (showDebug && isPanning && Vector2.Distance(previousPos, transform.localPosition) > 0.01f)
        {
            Debug.Log($"[PanCam] LocalPos: {transform.localPosition} | X: {transform.localPosition.x:F2} | Y: {transform.localPosition.y:F2}");
        }

        if (!isPanning && Vector2.Distance(transform.localPosition, Vector2.zero) < 0.01f)
        {
            transform.localPosition = Vector2.zero;
            currentVelocity = Vector2.zero;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        
        Gizmos.color = Color.green;
        Vector3 targetWorldPos = transform.parent.TransformPoint(targetLocalPosition);
        Gizmos.DrawLine(transform.position, targetWorldPos);
        Gizmos.DrawWireSphere(targetWorldPos, 0.2f);
    }
}

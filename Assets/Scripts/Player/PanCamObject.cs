using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PanCamObject : MonoBehaviour
{
    [Header("Configuration")]
    public PlayerConfig config;

    [Header("Component References")]
    private PlayerInputActions inputActions;

    [Header("Cache")]
    private Vector2 startPosition;

    [Header("Misc.")]
    private Vector2 panInput;
    private Vector2 targetLocalPosition = Vector2.zero;
    private bool isPanning = false;

    private void Awake()
    {
        // Create input actions
        inputActions = new PlayerInputActions();
    }

    void Start()
    {
        startPosition = transform.position;
    }

    private void OnEnable()
    {
        inputActions.Gameplay.Enable();

        inputActions.Gameplay.PanCamera.performed += OnPanInput;
        inputActions.Gameplay.PanCamera.canceled += OnPanEnd;

    }

    private void OnDisable()
    {
        inputActions.Gameplay.Disable();

        inputActions.Gameplay.PanCamera.performed -= OnPanInput;
        inputActions.Gameplay.PanCamera.canceled -= OnPanEnd;
    }

    private void OnPanInput(InputAction.CallbackContext context)
    {
        panInput = context.ReadValue<Vector2>();
        isPanning = panInput.magnitude > 0.1f;

        if (isPanning)
        {
            UpdateOrbitPosition();
        }
    }

    private void OnPanEnd(InputAction.CallbackContext context)
    {
        panInput = Vector2.zero;
        isPanning = false;
    }

    void Update()
    {
        ApplyMovement();

    }

    private void UpdateOrbitPosition()
    {
        Vector2 normalizedInput = panInput.normalized;

        if (config.invertXAxis) normalizedInput.x = -normalizedInput.x;
        if (config.invertYAxis) normalizedInput.y = -normalizedInput.y;

        float inputMagnitude = Mathf.Clamp01(panInput.magnitude);
        Vector3 orbitDirection = new Vector3(normalizedInput.x, normalizedInput.y, 0);

        targetLocalPosition = orbitDirection * (config.panAmount * inputMagnitude);
    }

    private void ApplyMovement()
    {
        float speed = isPanning ? config.panSpeed : config.returnSpeed;

        transform.localPosition = Vector2.Lerp(
            transform.localPosition,
            targetLocalPosition,
            speed * Time.deltaTime
        );

        if (!isPanning && Vector2.Distance(transform.localPosition, Vector3.zero) < 0.01f)
        {
            transform.localPosition = Vector2.zero;
        }
    }
}

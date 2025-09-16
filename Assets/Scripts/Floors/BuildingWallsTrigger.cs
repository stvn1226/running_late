using UnityEngine;
using UnityEngine.InputSystem;

public class BuildingWallsTrigger : MonoBehaviour
{
    private PlayerInputActions inputActions;
    private BoxCollider2D boxCollider;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    private void Start()
    {
        boxCollider = GetComponentInChildren<BoxCollider2D>();
    }

    private void OnEnable()
    {
        inputActions.Gameplay.Enable();

        inputActions.Gameplay.WallRun.performed += OnPlayerWallRun;
        inputActions.Gameplay.WallRun.canceled += OnPlayerStopWallRun;

    }

    private void OnDisable()
    {
        inputActions.Gameplay.Disable();

        inputActions.Gameplay.WallRun.performed -= OnPlayerWallRun;
        inputActions.Gameplay.WallRun.canceled -= OnPlayerStopWallRun;

    }

    private void Update()
    {
        
    }

    private void OnPlayerWallRun(InputAction.CallbackContext context)
    {
        boxCollider.isTrigger = false;
    }

    private void OnPlayerStopWallRun(InputAction.CallbackContext context)
    {
        boxCollider.isTrigger = true;
    }
}

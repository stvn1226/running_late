using UnityEngine;
using UnityEngine.InputSystem;

public class BuildingWallsTrigger : MonoBehaviour
{
    private BoxCollider2D boxCollider;

    private void Start()
    {
        boxCollider = GetComponentInChildren<BoxCollider2D>();
    }

    private void OnEnable()
    {
        if (UIManager.Instance != null && UIManager.Instance.inputActions != null)
        {
            UIManager.Instance.inputActions.Gameplay.WallRun.performed += OnPlayerWallRun;
            UIManager.Instance.inputActions.Gameplay.WallRun.canceled += OnPlayerStopWallRun;
        }
    }

    private void OnDisable()
    {
        if (UIManager.Instance != null && UIManager.Instance.inputActions != null)
        {
            UIManager.Instance.inputActions.Gameplay.WallRun.performed -= OnPlayerWallRun;
            UIManager.Instance.inputActions.Gameplay.WallRun.canceled -= OnPlayerStopWallRun;
        }
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

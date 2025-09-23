using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControllerModular : MonoBehaviour
{
    [Header("Configuration")]
    public PlayerConfig config;
    
    [Header("Component References")]
    private PlayerInputActions inputActions;
    private PlayerStates states;
    private PlayerMovement movement;
    private PlayerJump jump;
    private PlayerPhysics physics;
    private PlayerKick kicker;
    private PlayerStateManager stateManager;

    private void Awake()
    {
        // Get components
        states = GetComponent<PlayerStates>();
        movement = GetComponent<PlayerMovement>();
        jump = GetComponent<PlayerJump>();
        physics = GetComponent<PlayerPhysics>();
        kicker = GetComponentInChildren<PlayerKick>();
        stateManager = GetComponent<PlayerStateManager>();
        
        // Create input actions
        inputActions = new PlayerInputActions();
        
        // Ensure all components have the config
        if (config != null)
        {
            AssignConfigToComponents();
        }
    }

    private void Start()
    {
        // Initialize jump buffer timer
        if (config != null)
        {
            states.jumpBufferTimer = config.jumpBufferTime;
        }
    }

    private void OnEnable()
    {
        inputActions.Gameplay.Enable();

        // Subscribe to input events
        inputActions.Gameplay.Run.performed += OnMove;
        inputActions.Gameplay.Run.canceled += OnMove;
        inputActions.Gameplay.Jump.performed += OnJumpStart;
        inputActions.Gameplay.Jump.canceled += OnJumpEnd;
        inputActions.Gameplay.Crouch.performed += OnCrouchStart;
        inputActions.Gameplay.Crouch.canceled += OnCrouchEnd;
        inputActions.Gameplay.WallRun.performed += OnWallRunStart;
        inputActions.Gameplay.WallRun.canceled += OnWallRunEnd;
        inputActions.Gameplay.Kick.performed += OnKick;
        inputActions.Gameplay.DropDown.performed += OnDropDownStart;
        inputActions.Gameplay.DropDown.canceled += OnDropDownEnd;
    }

    private void OnDisable()
    {
        inputActions.Gameplay.Disable();

        // Unsubscribe from input events
        inputActions.Gameplay.Run.performed -= OnMove;
        inputActions.Gameplay.Run.canceled -= OnMove;
        inputActions.Gameplay.Jump.performed -= OnJumpStart;
        inputActions.Gameplay.Jump.canceled -= OnJumpEnd;
        inputActions.Gameplay.Crouch.performed -= OnCrouchStart;
        inputActions.Gameplay.Crouch.canceled -= OnCrouchEnd;
        inputActions.Gameplay.WallRun.performed -= OnWallRunStart;
        inputActions.Gameplay.WallRun.canceled -= OnWallRunEnd;
        inputActions.Gameplay.Kick.performed -= OnKick;
        inputActions.Gameplay.DropDown.performed -= OnDropDownStart;
        inputActions.Gameplay.DropDown.canceled -= OnDropDownEnd;
    }

    private void Update()
    {
    }

    private void AssignConfigToComponents()
    {
        if (movement != null) movement.config = config;
        if (jump != null) jump.config = config;
        if (physics != null) physics.config = config;
        if (stateManager != null) stateManager.config = config;
    }

    #region Input Callbacks
    private void OnMove(InputAction.CallbackContext context)
    {
        if (states != null)
        {
            states.moveInput = context.ReadValue<Vector2>();
        }
    }

    private void OnJumpStart(InputAction.CallbackContext context)
    {
        if (states != null)
        {
            states.SetJumpPressed();
            if (config != null)
            {
                states.jumpBufferTimer = config.jumpBufferTime;
            }
        }
    }

    private void OnJumpEnd(InputAction.CallbackContext context)
    {
        if (states != null)
        {
            states.SetJumpReleased();
        }
    }

    private void OnCrouchStart(InputAction.CallbackContext context)
    {
        if (states != null)
        {
            states.crouchHeld = true;
        }
    }

    private void OnCrouchEnd(InputAction.CallbackContext context)
    {
        if (states != null)
        {
            states.crouchHeld = false;
        }
    }

    private void OnWallRunStart(InputAction.CallbackContext context)
    {
        if (states != null)
        {
            states.wallRunHeld = true;
        }
    }

    private void OnWallRunEnd(InputAction.CallbackContext context)
    {
        if (states != null)
        {
            states.wallRunHeld = false;
        }
    }

    private void OnKick(InputAction.CallbackContext context)
    {
        if (states != null)
        {
            if (states.isKicking) return;
            states.SetKickPressed();
            kicker.PerformKick(config.kickDuration);
        }
    }

    private void OnDropDownStart(InputAction.CallbackContext context)
    {
        Debug.Log("PlayerControllerModular: OnDropDownStart called!");
        if (states != null)
        {
            states.SetDropDownPressed();
        }
    }

    private void OnDropDownEnd(InputAction.CallbackContext context)
    {
        Debug.Log("PlayerControllerModular: OnDropDownEnd called!");
        if (states != null)
        {
            states.SetDropDownReleased();
        }
    }
    #endregion

    #region Component Management
    public void SetConfig(PlayerConfig newConfig)
    {
        config = newConfig;
        AssignConfigToComponents();
    }

    public PlayerStates GetStates() => states;
    public PlayerConfig GetConfig() => config;
    #endregion

    #region Validation
    private void OnValidate()
    {
        // Auto-assign config to components in editor
        if (config != null)
        {
            AssignConfigToComponents();
        }
    }
    #endregion
}
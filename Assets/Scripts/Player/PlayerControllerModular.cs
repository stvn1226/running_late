using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControllerModular : MonoBehaviour
{
    [Header("Configuration")]
    public PlayerConfig config;
    
    [Header("Component References")]
    private PlayerInputActions inputActions;
    [SerializeField] private PlayerStates states;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerJump jump;
    [SerializeField] private PlayerPhysics physics;
    [SerializeField] private PlayerKick kicker;
    [SerializeField] private PlayerStateManager stateManager;
    [SerializeField] private PlayerAirDodge airDodge;


    private void Awake()
    {
        inputActions = new PlayerInputActions();
        
        if (config != null)
        {
            AssignConfigToComponents();
        }
    }

    private void Start()
    {
        if (config != null)
        {
            states.jumpBufferTimer = config.jumpBufferTime;
        }
    }

    private void OnEnable()
    {
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
        inputActions.Gameplay.AirDodge.performed += OnAirDodge;
        inputActions.Gameplay.Dive.performed += OnDive;

        inputActions.Gameplay.Reset.performed += OnReset;
        
        inputActions.Gameplay.Enable();
    }

    private void OnDisable()
    {
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
        inputActions.Gameplay.AirDodge.performed -= OnAirDodge;
        inputActions.Gameplay.Dive.performed -= OnDive;

        inputActions.Gameplay.Reset.performed -= OnReset;
        
        inputActions.Gameplay.Disable();
    }

    private void Update()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsPaused)
        {
            if (inputActions.Gameplay.enabled)
            {
                inputActions.Gameplay.Disable();
            }
        }
        else
        {
            if (!inputActions.Gameplay.enabled)
            {
                inputActions.Gameplay.Enable();
            }
        }
    }

    private void AssignConfigToComponents()
    {
        if (movement != null) movement.config = config;
        if (jump != null) jump.config = config;
        if (physics != null) physics.config = config;
        if (stateManager != null) stateManager.config = config;
        if (airDodge != null) airDodge.config = config;
    }

    #region Input Callbacks
    private void OnMove(InputAction.CallbackContext context)
    {
        Vector2 input = context.ReadValue<Vector2>();
    
        const float deadzone = 0.3f;
    
        float x = Mathf.Abs(input.x) > deadzone ? Mathf.Sign(input.x) : 0f;
        float y = Mathf.Abs(input.y) > deadzone ? Mathf.Sign(input.y) : 0f;
    
        states.moveInput = new Vector2(x, y);
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
            
            if (config != null && !states.isGrounded)
            {
                states.BufferCrouchInput(config.longJumpBufferTime);
            }
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
        if (states != null && config != null)
        {
            if (states.isKicking)
            {
                GameLogger.LogWarning("KICK", "Kick already active, ignoring input");
                return;
            }
            
            states.SetKickPressed(config.kickDuration);
            kicker.PerformKick(config.kickDuration);
            
            GameLogger.LogInput($"Kick input processed - Duration: {config.kickDuration}s");
        }
    }

    private void OnDropDownStart(InputAction.CallbackContext context)
    {
        GameLogger.LogInput("OnDropDownStart called!");
        if (states != null)
        {
            states.SetDropDownPressed();
        }
    }

    private void OnDropDownEnd(InputAction.CallbackContext context)
    {
        GameLogger.LogInput("OnDropDownEnd called!");
        if (states != null)
        {
            states.SetDropDownReleased();
        }
    }

    private void OnAirDodge(InputAction.CallbackContext context)
    {
        GameLogger.LogInput("OnAirDodge called!");
        if (states != null)
        {
            states.SetAirDodgePressed();
        }
    }

    private void OnDive(InputAction.CallbackContext context)
    {
        GameLogger.LogInput("OnDive called!");
        if (states != null)
        {
            //states.SetDivePressed();
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
        if (config != null)
        {
            AssignConfigToComponents();
        }
    }
    #endregion

    #region Fast Retry
    private void OnReset(InputAction.CallbackContext context)
    {
        FastRetry();
    }

    public void FastRetry()
    {
        if (LevelStarter.Instance == null || LevelManager.Instance == null)
        {
            GameLogger.LogWarning("PLAYER", "Cannot retry: LevelStarter or LevelManager not found");
            return;
        }

        int currentSeed = LevelStarter.Instance.GetCurrentSeed();
        
        PlayerPrefs.SetInt("RetrySeed", currentSeed);
        PlayerPrefs.SetInt("UseRetrySeed", 1);
        
        Time.timeScale = 1f;
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideAllPanels();
        }
        
        LevelManager.Instance.ReplayLevel();
        
        GameLogger.Log("PLAYER", $"Fast retry triggered for seed: {currentSeed}");
    }
    #endregion
}

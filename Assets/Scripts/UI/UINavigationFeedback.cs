using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class UINavigationFeedback : MonoBehaviour
{
    [Header("Vibration Settings")]
    [SerializeField] private bool enableNavigationVibration = true;
    [SerializeField] private float navigationVibrationDuration = 0.05f;

    [Header("Audio Settings")]
    [SerializeField] private bool enableNavigationSound = true;

    private PlayerInputActions _inputActions;
    private GameObject _lastSelectedObject;

    private void Awake()
    {
        _inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        _inputActions.UI.Enable();
        _inputActions.UI.Submit.performed += OnSubmit;
        _inputActions.UI.Click.performed += OnClick;
    }

    private void OnDisable()
    {
        _inputActions.UI.Submit.performed -= OnSubmit;
        _inputActions.UI.Click.performed -= OnClick;
        _inputActions.UI.Disable();
    }

    private void OnDestroy()
    {
        _inputActions.Dispose();
    }

    private void Update()
    {
        if (EventSystem.current == null)
            return;

        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;

        if (currentSelected != _lastSelectedObject && currentSelected != null && _lastSelectedObject != null)
        {
            if (enableNavigationVibration)
                GamepadVibrationManager.Instance?.TriggerLightVibration(navigationVibrationDuration);

            if (enableNavigationSound)
                AudioManager.Instance?.Play(AudioManager.SoundType.OptionChange);
        }

        _lastSelectedObject = currentSelected;
    }

    /// <summary>
    /// Plays OptionSelect when the player confirms a selection via gamepad or keyboard.
    /// </summary>
    private void OnSubmit(InputAction.CallbackContext context)
    {
        if (!enableNavigationSound) return;
        if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null) return;

        AudioManager.Instance?.Play(AudioManager.SoundType.OptionSelect);
    }

    /// <summary>
    /// Plays OptionSelect when the player clicks a UI element with the mouse.
    /// </summary>
    private void OnClick(InputAction.CallbackContext context)
    {
        if (!enableNavigationSound) return;
        if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null) return;

        AudioManager.Instance?.Play(AudioManager.SoundType.OptionSelect);
    }
}
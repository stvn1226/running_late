using UnityEngine;
using Steamworks;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Opens Steam's controller keyboard when its assigned input field receives a UI submit event.
/// While the Steam overlay owns controller input, Unity's menu navigation is suspended.
/// </summary>
public class SteamKeyboardController : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private TMP_InputField targetInputField;

    private bool isKeyboardOpen;
    private bool navigationEventsWereEnabled;
    private bool uiInputModuleWasEnabled;
    private bool menuNavigationSuspended;
    private InputSystemUIInputModule uiInputModule;
    private PlayerInputActions inputActions;

    public static bool IsAnyKeyboardOpen { get; private set; }

    private void Awake()
    {
        if (targetInputField == null)
        {
            targetInputField = GetComponent<TMP_InputField>();
        }

        SteamUtils.OnGamepadTextInputDismissed += OnSteamKeyboardDismissed;
        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        inputActions.UI.Enable();
        inputActions.UI.Submit.performed += OnControllerSubmit;
    }

    private void OnDisable()
    {
        inputActions.UI.Submit.performed -= OnControllerSubmit;
        inputActions.UI.Disable();
    }

    private void OnDestroy()
    {
        SteamUtils.OnGamepadTextInputDismissed -= OnSteamKeyboardDismissed;
        inputActions.Dispose();
        IsAnyKeyboardOpen = false;

        // SteamManager owns the Steam client lifecycle. Only restore the UI state this
        // component changed if it is removed while the overlay is still open.
        RestoreMenuNavigation();
    }

    /// <summary>
    /// Configures the input field controlled by this component.
    /// </summary>
    public void Configure(TMP_InputField inputField)
    {
        targetInputField = inputField;
    }

    private void OnControllerSubmit(InputAction.CallbackContext context)
    {
        if (targetInputField == null ||
            context.control.device is not Gamepad ||
            EventSystem.current == null ||
            EventSystem.current.currentSelectedGameObject != targetInputField.gameObject)
        {
            return;
        }

        OpenSteamOverlayKeyboard();
    }

    /// <summary>
    /// Opens Steam's on-screen keyboard. This can also be assigned to a UnityEvent.
    /// </summary>
    public void OpenSteamOverlayKeyboard()
    {
        if (isKeyboardOpen || targetInputField == null || !SteamClient.IsValid)
        {
            Debug.Log("Steam client failed!");
            return;
        }

        isKeyboardOpen = true;
        IsAnyKeyboardOpen = true;
        SuspendMenuNavigation();
        targetInputField.DeactivateInputField();

        bool keyboardOpened = SteamUtils.ShowGamepadTextInput(
            GamepadTextInputMode.Normal,
            GamepadTextInputLineMode.SingleLine,
            targetInputField.placeholder != null ? targetInputField.placeholder.GetComponent<TMP_Text>()?.text : "Enter seed...",
            targetInputField.characterLimit > 0 ? targetInputField.characterLimit : 100,
            targetInputField.text
        );

        Debug.Log($"Steam keyboard requested. Opened: {keyboardOpened}, Overlay enabled: {SteamUtils.IsOverlayEnabled}, Big Picture: {SteamUtils.IsSteamInBigPictureMode}");

        if (!keyboardOpened)
        {
            isKeyboardOpen = false;
            IsAnyKeyboardOpen = false;
            RestoreMenuNavigation();
        }
    }

    /// <summary>
    /// Event fired automatically by Steam when the user submits or cancels the overlay keyboard.
    /// </summary>
    private void OnSteamKeyboardDismissed(bool submitted)
    {
        if (!isKeyboardOpen)
            return;

        isKeyboardOpen = false;
        IsAnyKeyboardOpen = false;

        if (submitted)
        {
            string resultText = SteamUtils.GetEnteredGamepadText();

            if (targetInputField != null)
                targetInputField.text = resultText;
        }

        RestoreMenuNavigation();
    }

    private void SuspendMenuNavigation()
    {
        if (EventSystem.current == null)
            return;

        navigationEventsWereEnabled = EventSystem.current.sendNavigationEvents;
        EventSystem.current.sendNavigationEvents = false;
        menuNavigationSuspended = true;

        uiInputModule = EventSystem.current.GetComponent<InputSystemUIInputModule>();
        if (uiInputModule != null)
        {
            uiInputModuleWasEnabled = uiInputModule.enabled;
            uiInputModule.enabled = false;
        }
    }

    private void RestoreMenuNavigation()
    {
        if (!menuNavigationSuspended)
            return;

        if (EventSystem.current != null)
        {
            EventSystem.current.sendNavigationEvents = navigationEventsWereEnabled;
        }

        if (uiInputModule != null)
        {
            uiInputModule.enabled = uiInputModuleWasEnabled;
            uiInputModule = null;
        }

        menuNavigationSuspended = false;
    }
}

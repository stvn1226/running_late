using UnityEngine;
#if UNITY_STANDALONE && !UNITY_SERVER
using Steamworks;
#endif
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Opens Steam's controller keyboard when its assigned input field receives a UI submit event.
/// While the Steam overlay owns controller input, Unity's menu navigation is suspended.
/// </summary>
public class SteamKeyboardController : MonoBehaviour, ISubmitHandler
{
    [Header("UI Reference")]
    [SerializeField] private TMP_InputField targetInputField;

    private bool isKeyboardOpen;
    private bool navigationEventsWereEnabled;
    private bool uiInputModuleWasEnabled;
    private bool menuNavigationSuspended;
    private InputSystemUIInputModule uiInputModule;

    public static bool IsAnyKeyboardOpen { get; private set; }

    private void Awake()
    {
        if (targetInputField == null)
        {
            targetInputField = GetComponent<TMP_InputField>();
        }

#if UNITY_STANDALONE && !UNITY_SERVER
        SteamUtils.OnGamepadTextInputDismissed += OnSteamKeyboardDismissed;
#endif
    }

    private void OnDestroy()
    {
#if UNITY_STANDALONE && !UNITY_SERVER
        SteamUtils.OnGamepadTextInputDismissed -= OnSteamKeyboardDismissed;
#endif
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

    /// <summary>
    /// Called by the EventSystem when the focused input field is confirmed.
    /// </summary>
    public void OnSubmit(BaseEventData eventData)
    {
        OpenSteamOverlayKeyboard();
    }

    /// <summary>
    /// Opens Steam's on-screen keyboard. This can also be assigned to a UnityEvent.
    /// </summary>
    public void OpenSteamOverlayKeyboard()
    {
#if UNITY_STANDALONE && !UNITY_SERVER
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

        if (!keyboardOpened)
        {
            isKeyboardOpen = false;
            IsAnyKeyboardOpen = false;
            RestoreMenuNavigation();
        }
#else
        if (targetInputField != null)
            targetInputField.ActivateInputField();
#endif
    }

    /// <summary>
    /// Event fired automatically by Steam when the user submits or cancels the overlay keyboard.
    /// </summary>
    private void OnSteamKeyboardDismissed(bool submitted)
    {
#if UNITY_STANDALONE && !UNITY_SERVER
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
#endif
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

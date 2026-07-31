using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using UnityEngine.EventSystems;

[Serializable]
public class KeyboardLayoutConfig
{
    public KeyboardLayoutType layoutType;
    public GameObject layoutContainer;
    public GameObject defaultSelectedKey;

    [Tooltip("Characters assigned in order to the Key buttons in this layout.\nSpecial values: BACKSPACE, ENTER, CANCEL, SPACE, COPY, PASTE, SHIFT")]
    public string[] keySequence;

    [Tooltip("Alternative characters when Shift is active (same length as keySequence).\nLeave empty to disable Shift for this layout.")]
    public string[] shiftKeySequence;
}

[RequireComponent(typeof(CanvasGroup))]
public class VirtualKeyboardPanel : MonoBehaviour
{
    public static VirtualKeyboardPanel Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindObjectOfType<VirtualKeyboardPanel>(true);
            return _instance;
        }
    }
    private static VirtualKeyboardPanel _instance;

    [Header("UI References")]
    [SerializeField] private TMP_Text displayText;

    [Header("Layout Controls")]
    [SerializeField] private KeyboardLayoutConfig[] layouts;

    // --- Internal state ---
    private Action<string> onConfirmCallback;
    private string currentDraft = "";
    private PlayerInputActions inputActions;
    private CanvasGroup canvasGroup;
    public bool IsOpen { get; private set; }
    private Coroutine navigationGuardCoroutine;
    private GameObject lastValidKey;
    private GameObject previousSelection; // The object that was selected before opening
    private bool isShiftActive = false;
    private KeyboardLayoutType activeLayoutType;

    // Gamepad edge-detection
    private bool startWasHeld;
    private bool xWasHeld;
    private bool cancelWasHeld;

    // ─── Lifecycle ────────────────────────────────────────────────

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        inputActions = new PlayerInputActions();
        canvasGroup = GetComponent<CanvasGroup>();
        SetPanelEnabled(false);
    }

    private void OnDestroy()
    {
        // Clear static reference so the Editor doesn't hold a stale ref after Play Mode stops
        if (_instance == this) _instance = null;
    }

    private void Start()
    {
        // Assign key characters and text labels from the keySequence defined in the Inspector.
        // Safe here because the GameObject is always active (CanvasGroup approach).
        if (layouts != null)
        {
            foreach (var layout in layouts)
            {
                if (layout.layoutContainer != null)
                    InitializeButtonsInContainer(layout);
            }
        }
    }

    private void OnEnable()
    {
        inputActions.UI.Enable();
    }

    private void OnDisable()
    {
        inputActions.UI.Disable();
    }

    private void Update()
    {
        if (!IsOpen) return;

        var gamepad = UnityEngine.InputSystem.Gamepad.current;
        if (gamepad == null) return;

        // Start → Confirm
        bool startHeld = gamepad.startButton.isPressed;
        if (startHeld && !startWasHeld) Confirm();
        startWasHeld = startHeld;

        // X (West) → Copy to clipboard
        bool xHeld = gamepad.buttonWest.isPressed;
        if (xHeld && !xWasHeld) CopyToClipboard();
        xWasHeld = xHeld;

        // B (East) → Cancel (fallback in case InputSystem event doesn't fire)
        bool cancelHeld = gamepad.buttonEast.isPressed;
        if (cancelHeld && !cancelWasHeld) Cancel();
        cancelWasHeld = cancelHeld;
    }

    // ─── Public API ───────────────────────────────────────────────

    public void Show(string initialText, KeyboardLayoutType layoutType, Action<string> onConfirm)
    {
        currentDraft = initialText;
        onConfirmCallback = onConfirm;
        UpdateDisplay();

        if (EventSystem.current != null)
            previousSelection = EventSystem.current.currentSelectedGameObject;

        // Switch layouts
        GameObject defaultKeyToSelect = null;
        if (layouts != null)
        {
            foreach (var layout in layouts)
            {
                if (layout.layoutContainer == null) continue;
                bool isActive = (layout.layoutType == layoutType);
                layout.layoutContainer.SetActive(isActive);
                if (isActive)
                {
                    defaultKeyToSelect = layout.defaultSelectedKey;
                    BakeNavigation(layout.layoutContainer);
                }
            }
        }

        SetPanelEnabled(true);
        IsOpen = true;
        isShiftActive = false;
        activeLayoutType = layoutType;

        inputActions.UI.Cancel.performed += OnCancelPerformed;

        if (navigationGuardCoroutine != null) StopCoroutine(navigationGuardCoroutine);
        navigationGuardCoroutine = StartCoroutine(NavigationGuard());

        lastValidKey = defaultKeyToSelect;

        // Defer selection one frame so EventSystem finishes processing
        // the InputField's OnSelect that triggered this Show() call.
        StartCoroutine(SelectNextFrame(defaultKeyToSelect));
    }

    private IEnumerator SelectNextFrame(GameObject target)
    {
        yield return null;
        if (EventSystem.current != null && target != null && target.activeInHierarchy)
            EventSystem.current.SetSelectedGameObject(target);
    }

    public void AddCharacter(string c)
    {
        currentDraft += c;
        UpdateDisplay();
    }

    public void Backspace()
    {
        if (currentDraft.Length > 0)
        {
            currentDraft = currentDraft.Substring(0, currentDraft.Length - 1);
            UpdateDisplay();
        }
    }

    public void Confirm()
    {
        onConfirmCallback?.Invoke(currentDraft);
        Hide();
    }

    public void Cancel()
    {
        Hide();
    }

    public void ToggleShift()
    {
        isShiftActive = !isShiftActive;

        if (layouts != null)
        {
            foreach (var layout in layouts)
            {
                if (layout.layoutContainer != null && layout.layoutContainer.activeSelf)
                    ApplySequenceToButtons(layout, isShiftActive);
            }
        }
    }

    public void CopyToClipboard()
    {
        if (!string.IsNullOrEmpty(currentDraft))
        {
            GUIUtility.systemCopyBuffer = currentDraft;
        }
    }

    public void PasteFromClipboard()
    {
        string pasted = GUIUtility.systemCopyBuffer;
        if (!string.IsNullOrEmpty(pasted))
        {
            currentDraft += pasted;
            UpdateDisplay();
        }
    }

    private void Hide()
    {
        if (!IsOpen) return;

        inputActions.UI.Cancel.performed -= OnCancelPerformed;

        if (navigationGuardCoroutine != null)
        {
            StopCoroutine(navigationGuardCoroutine);
            navigationGuardCoroutine = null;
        }

        currentDraft = "";
        onConfirmCallback = null;
        IsOpen = false;
        SetPanelEnabled(false);

        if (EventSystem.current != null && previousSelection != null && previousSelection.activeInHierarchy)
        {
            EventSystem.current.SetSelectedGameObject(previousSelection);
        }
        previousSelection = null;
    }

    private void SetPanelEnabled(bool enabled)
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha = enabled ? 1f : 0f;
        canvasGroup.interactable = enabled;
        canvasGroup.blocksRaycasts = enabled;
    }

    private void UpdateDisplay()
    {
        if (displayText != null)
            displayText.text = currentDraft;
    }

    private void OnCancelPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx) => Cancel();

    private IEnumerator NavigationGuard()
    {
        while (true)
        {
            yield return null;
            if (!IsOpen || EventSystem.current == null) continue;

            GameObject selected = EventSystem.current.currentSelectedGameObject;

            if (selected == null)
            {
                if (lastValidKey != null)
                    EventSystem.current.SetSelectedGameObject(lastValidKey);
                continue;
            }

            if (selected.GetComponentInParent<VirtualKeyboardPanel>() == this)
                lastValidKey = selected;
            else
            {
                if (lastValidKey != null)
                    EventSystem.current.SetSelectedGameObject(lastValidKey);
            }
        }
    }

    private void InitializeButtonsInContainer(KeyboardLayoutConfig layout)
    {
        var buttons = layout.layoutContainer.GetComponentsInChildren<VirtualKeyboardButton>(true);
        foreach (var btn in buttons)
            btn.gameObject.SetActive(true);

        ApplySequenceToButtons(layout, useShift: false);
    }

    private void ApplySequenceToButtons(KeyboardLayoutConfig layout, bool useShift)
    {
        var buttons = layout.layoutContainer.GetComponentsInChildren<VirtualKeyboardButton>(false);
        string[] sequence = (useShift && layout.shiftKeySequence != null && layout.shiftKeySequence.Length > 0)
            ? layout.shiftKeySequence
            : layout.keySequence;

        if (sequence == null || sequence.Length == 0) return;

        int seqIndex = 0;
        foreach (var btn in buttons)
        {
            if (!btn.gameObject.activeSelf) continue;

            if (seqIndex < sequence.Length)
            {
                btn.SetKey(sequence[seqIndex]);
                seqIndex++;
            }
            else
            {
                btn.gameObject.SetActive(false);
            }
        }
    }

    private void BakeNavigation(GameObject container)
    {
        var buttons = container.GetComponentsInChildren<Button>(false);

        foreach (var btn in buttons)
        {
            var pos = ((RectTransform)btn.transform).position;

            Button up = null, down = null, left = null, right = null;
            float minUp = float.MaxValue, minDown = float.MaxValue;
            float minLeft = float.MaxValue, minRight = float.MaxValue;

            foreach (var other in buttons)
            {
                if (other == btn) continue;
                var otherPos = ((RectTransform)other.transform).position;
                Vector2 diff = (Vector2)otherPos - (Vector2)pos;
                float absX = Mathf.Abs(diff.x);
                float absY = Mathf.Abs(diff.y);

                if (absY >= absX)
                {
                    if (diff.y > 0 && absY < minUp)  { minUp   = absY; up   = other; }
                    if (diff.y < 0 && absY < minDown) { minDown = absY; down = other; }
                }
                else
                {
                    if (diff.x > 0 && absX < minRight) { minRight = absX; right = other; }
                    if (diff.x < 0 && absX < minLeft)  { minLeft  = absX; left  = other; }
                }
            }

            btn.navigation = new Navigation
            {
                mode          = Navigation.Mode.Explicit,
                selectOnUp    = up,
                selectOnDown  = down,
                selectOnLeft  = left,
                selectOnRight = right
            };
        }
    }
}

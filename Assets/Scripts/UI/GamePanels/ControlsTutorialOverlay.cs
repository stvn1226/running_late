using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

namespace UI.GamePanels
{
    /// <summary>
    /// Interactive Controls Tutorial Overlay displaying layered artwork for Gamepad and Keyboard/Mouse.
    /// Uses Outline Material on the active target button/key and alternates between Base (Front) and Base2 (Top).
    /// Advances step-by-step through the mechanics while updating the interactive Player Preview.
    /// </summary>
    public class ControlsTutorialOverlay : MonoBehaviour
    {
        public static ControlsTutorialOverlay Instance { get; private set; }

        private const string TUTORIAL_DONE_PREF = "ControlsTutorial_Done";

        public enum TutorialStep
        {
            Move = 0,
            Jump = 1,
            Slide = 2,
            Kick = 3,
            WallRun = 4,
            DropDown = 5,
            AirDodge = 6,
            Dive = 7,
            Completed = 8
        }

        [Header("Tutorial Progression Rules")]
        [Tooltip("Master switch to enable/disable the tutorial popup entirely.")]
        [SerializeField] private bool enableTutorial = false;
        [Tooltip("If FALSE (default): pressing at least ONE valid input advances to the next step. If TRUE: ALL displayed inputs for the step must be pressed before advancing.")]
        [SerializeField] private bool requireAllKeysPerStep = false;

        [Header("Testing & Debug")]
        [Tooltip("Force show tutorial on start even if already completed.")]
        [SerializeField] private bool forceShowForTesting = false;

        [Header("Root Canvas & Fade")]
        [SerializeField] private CanvasGroup mainCanvasGroup;
        [SerializeField] private float fadeDuration = 0.35f;

        [Header("Device Containers")]
        [SerializeField] private GameObject controllerContainer;
        [SerializeField] private GameObject keyboardContainer;
        [SerializeField] private TextMeshProUGUI deviceStatusText;

        [Header("Outline Highlighting Material")]
        [Tooltip("Material with sprite/UI outline effect to inject directly into active Images.")]
        [SerializeField] private Material outlineMaterial;

        [Header("Controller Bases (Front vs Top Views)")]
        [Tooltip("Front controller view containing Palanca1, DPad, Face Buttons A/B/X/Y, Select, Start")]
        [SerializeField] private GameObject baseFrontObject; // Base (Front)
        [Tooltip("Top controller view containing L1, R1, L2, R2")]
        [SerializeField] private GameObject baseTopObject;   // Base2 (Top)

        [Header("CanvasGroups for Instant Alpha Toggle (assign in Inspector)")]
        [Tooltip("CanvasGroup on the Base (Front) object — add the component in the Inspector.")]
        [SerializeField] private CanvasGroup baseFrontCanvasGroup;
        [Tooltip("CanvasGroup on the Base2 (Top) object — add the component in the Inspector.")]
        [SerializeField] private CanvasGroup baseTopCanvasGroup;
        [Tooltip("CanvasGroup on the KeyboardContainer — add the component in the Inspector.")]
        [SerializeField] private CanvasGroup keyboardCanvasGroup;

        [Header("Visual QWERTY Keyboard Display (optional)")]
        [Tooltip("KeyboardDisplay component placed on any empty GameObject. Right-click it → Build QWERTY Layout, then position freely. When assigned, replaces loose key images for keyboard highlighting.")]
        [SerializeField] private KeyboardDisplay keyboardDisplay;

        [Header("Controller Front Artwork Layers (Base)")]
        [SerializeField] private Image controllerBaseImage;
        [SerializeField] private Image controllerStick1Image;    // Palanca 1 (Move)
        [SerializeField] private Image controllerStick2Image;    // Palanca 2 (Camera)
        [SerializeField] private Image controllerDpadImage;      // Flechas (Move)
        [SerializeField] private Image controllerButtonAImage;   // A (Jump)
        [SerializeField] private Image controllerButtonBImage;   // B (Slide)
        [SerializeField] private Image controllerButtonXImage;   // X (Kick)
        [SerializeField] private Image controllerButtonYImage;   // Button_Dive / Y
        [SerializeField] private Image controllerSelectImage;    // Select
        [SerializeField] private Image controllerStartImage;     // Start

        [Header("Controller Top Artwork Layers (Base2)")]
        [SerializeField] private Image controllerL1Image;        // Shoulder_L1 (Drop Down)
        [SerializeField] private Image controllerR1Image;        // Shoulder_R1 (Air Dodge)
        [SerializeField] private Image controllerL2Image;        // Trigger_L2 (Crouch/Slide)
        [SerializeField] private Image controllerR2Image;        // Trigger_R2 (Wall Run)

        [Header("Keyboard & Mouse Artwork Layers")]
        [SerializeField] private Image keyMoveLeftImage;         // A
        [SerializeField] private Image keyMoveRightImage;        // D
        [SerializeField] private Image keyMoveUpImage;           // W (Climb / Jump)
        [SerializeField] private Image keySlideImage;            // S
        [SerializeField] private Image keySpaceImage;            // Barra espaciadora (Jump)
        [SerializeField] private Image keyKickImage;             // F
        [SerializeField] private Image keyKickAltImage;          // Q
        [SerializeField] private Image keyShiftImage;            // Shift (Wall Run)
        [SerializeField] private Image keyCtrlImage;             // Ctrl (Slide)
        [SerializeField] private Image keyDropDownImage;         // C (Drop Down)
        [SerializeField] private Image keyAirDodgeImage;         // V (Air Dodge)
        [SerializeField] private Image keyDiveImage;             // B (Dive)
        [SerializeField] private Image mouseLeftClickImage;      // Click Izquierdo (Kick)
        [SerializeField] private Image mouseRightClickImage;     // Click Derecho (Camera)

        [Header("Interactive Animated Player Preview")]
        [SerializeField] private UIPlayerPreview playerPreview;

        [Header("Action Badges & Checkmarks (Optional)")]
        [SerializeField] private GameObject badgeMoveCheck;
        [SerializeField] private GameObject badgeJumpCheck;
        [SerializeField] private GameObject badgeSlideCheck;
        [SerializeField] private GameObject badgeKickCheck;

        [Header("Buttons")]
        [SerializeField] private Button startLevelButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private TextMeshProUGUI startButtonText;

        // Sequence State
        private TutorialStep currentStep = TutorialStep.Move;
        private HashSet<TutorialStep> completedSteps = new HashSet<TutorialStep>();
        private Coroutine stepTransitionCoroutine;

        private Action onCompleteCallback;
        private bool isShowing = false;
        private ControllerType currentDevice = ControllerType.Keyboard;
        private List<Image> currentlyOutlinedImages = new List<Image>();

        public static bool IsShowingTutorial => Instance != null && Instance.isShowing;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (mainCanvasGroup != null)
            {
                mainCanvasGroup.alpha = 0f;
                mainCanvasGroup.blocksRaycasts = false;
                mainCanvasGroup.interactable = false;
            }

            if (!enableTutorial)
            {
                gameObject.SetActive(false);
                return;
            }

            if (startLevelButton != null)
            {
                startLevelButton.onClick.AddListener(OnStartClicked);
                startLevelButton.gameObject.SetActive(false);
            }

            if (skipButton != null)
            {
                skipButton.onClick.AddListener(OnSkipClicked);
            }

            // Auto-fill any keyboard Image fields that were not assigned in the Inspector
            AutoLinkKeyboardImages();
        }

        /// <summary>
        /// Fills any unassigned keyboard Image fields by searching two sources:
        ///   1. KeyboardContainer children — names like "Key_A", "Key_D", "Key_Space" (original loose sprites)
        ///   2. KeyboardDisplay children  — names like "A", "D", "SPACE"  (generated by KeyboardDisplay.BuildLayout)
        /// Only fills fields that are currently null — never overwrites manual assignments.
        /// Call from the Inspector ContextMenu at edit time, or it runs automatically in Awake.
        /// </summary>
#if UNITY_EDITOR
        [UnityEditor.MenuItem("CONTEXT/ControlsTutorialOverlay/Auto-Link Keyboard Keys")]
        private static void MenuAutoLink(UnityEditor.MenuCommand cmd)
        {
            ((ControlsTutorialOverlay)cmd.context).AutoLinkKeyboardImages();
            UnityEditor.EditorUtility.SetDirty(cmd.context);
        }
#endif
        [ContextMenu("Auto-Link Keyboard Keys")]
        private void AutoLinkKeyboardImages()
        {
            // Source 1: KeyboardContainer  (names: "Key_A", "Key_D", "Mouse_Left" …)
            // Source 2: KeyboardDisplay    (names: "A", "D", "SPACE", "SHIFT" …)

            // key Image fields — (KeyboardContainer child name, KeyboardDisplay child name)
            LinkKeyImage("Key_A",       "A",       ref keyMoveLeftImage);
            LinkKeyImage("Key_D",       "D",       ref keyMoveRightImage);
            LinkKeyImage("Key_W",       "W",       ref keyMoveUpImage);
            LinkKeyImage("Key_S",       "S",       ref keySlideImage);
            LinkKeyImage("Key_Space",   "SPACE",   ref keySpaceImage);
            LinkKeyImage("Key_F",       "F",       ref keyKickImage);
            LinkKeyImage("Key_Q",       "Q",       ref keyKickAltImage);
            LinkKeyImage("Key_Shift",   "SHIFT",   ref keyShiftImage);
            LinkKeyImage("Key_Ctrl",    "CTRL",    ref keyCtrlImage);
            LinkKeyImage("Key_C",       "C",       ref keyDropDownImage);
            LinkKeyImage("Key_V",       "V",       ref keyAirDodgeImage);
            LinkKeyImage("Key_E",       "E",       ref keyDiveImage);
            LinkKeyImage("Mouse_Left",  null,      ref mouseLeftClickImage);
            LinkKeyImage("Mouse_Right", null,      ref mouseRightClickImage);

#if UNITY_EDITOR
            Debug.Log("[ControlsTutorialOverlay] Auto-Link complete.");
#endif
        }

        /// <summary>
        /// Tries to find an Image for the given field.
        /// Search order: (1) keyboardContainer by containerName, (2) keyboardDisplay by displayName.
        /// </summary>
        private void LinkKeyImage(string containerName, string displayName, ref Image field)
        {
            if (field != null) return; // already assigned — skip

            // --- Source 1: KeyboardContainer ---
            if (keyboardContainer != null)
            {
                Image img = FindImageByName(keyboardContainer.transform, containerName);
                if (img != null) { field = img; return; }
            }

            // --- Source 2: KeyboardDisplay ---
            if (keyboardDisplay != null && displayName != null)
            {
                Image img = FindImageByName(keyboardDisplay.transform, displayName);
                if (img != null) { field = img; return; }
            }
        }

        /// <summary>Shallow-first, then deep search for a child named <paramref name="childName"/> that has an Image.</summary>
        private static Image FindImageByName(Transform root, string childName)
        {
            // Direct child first (fastest)
            Transform direct = root.Find(childName);
            if (direct != null)
            {
                Image img = direct.GetComponent<Image>();
                if (img != null) return img;
            }

            // Full deep search
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != childName) continue;
                Image img = t.GetComponent<Image>();
                if (img != null) return img;
            }
            return null;
        }

        public static bool ShouldShowTutorial()
        {
            if (Instance != null) return Instance.ShouldShowTutorialInstance();
            return false;
        }

        public bool ShouldShowTutorialInstance()
        {
            if (!enableTutorial) return false;
            if (forceShowForTesting) return true;
            return PlayerPrefs.GetInt(TUTORIAL_DONE_PREF, 0) == 0;
        }

        /// <summary>
        /// Explicitly opens the tutorial at any time (e.g. from Settings or Pause menu), regardless of PlayerPrefs.
        /// </summary>
        public static void ShowTutorialExplicit(Action onComplete = null)
        {
            if (Instance != null)
            {
                Instance.ShowTutorial(onComplete);
            }
            else
            {
                ControlsTutorialOverlay overlay = FindFirstObjectByType<ControlsTutorialOverlay>(FindObjectsInactive.Include);
                if (overlay != null)
                {
                    overlay.ShowTutorial(onComplete);
                }
                else
                {
                    onComplete?.Invoke();
                }
            }
        }

        public void ShowTutorial(Action onComplete)
        {
            if (!enableTutorial)
            {
                onComplete?.Invoke();
                return;
            }
            onCompleteCallback = onComplete;
            isShowing = true;

            completedSteps.Clear();
            currentStep = TutorialStep.Move;

            if (startLevelButton != null) startLevelButton.gameObject.SetActive(false);
            if (skipButton != null) skipButton.gameObject.SetActive(true);

            DetectInitialDevice();
            ApplyStepVisuals(currentStep);

            StartCoroutine(FadeRoutine(1f, true));
        }

        public void HideTutorial()
        {
            if (!isShowing) return;
            isShowing = false;

            ClearAllOutlines();

            PlayerPrefs.SetInt(TUTORIAL_DONE_PREF, 1);
            PlayerPrefs.Save();

            StartCoroutine(FadeRoutine(0f, false, () =>
            {
                onCompleteCallback?.Invoke();
                onCompleteCallback = null;
            }));
        }

        private void Update()
        {
            if (!isShowing) return;

            UpdateDeviceDetection();
            CheckLiveInputs();

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                OnSkipClicked();
            }
            else if (Gamepad.current != null && (
                Gamepad.current.startButton.wasPressedThisFrame  ||
                Gamepad.current.selectButton.wasPressedThisFrame ||
                Gamepad.current.buttonEast.wasPressedThisFrame))  // B button = skip
            {
                if (currentStep == TutorialStep.Completed) OnStartClicked();
                else OnSkipClicked();
            }
        }

        private void DetectInitialDevice()
        {
            if (Gamepad.current != null && Gamepad.current.added)
            {
                SetDevice(ControllerType.Xbox);
            }
            else
            {
                SetDevice(ControllerType.Keyboard);
            }
        }

        private void UpdateDeviceDetection()
        {
            if (Gamepad.current != null)
            {
                bool gamepadActivity =
                    Gamepad.current.wasUpdatedThisFrame &&
                    (Gamepad.current.allControls.Count > 0);

                if (gamepadActivity && currentDevice == ControllerType.Keyboard)
                {
                    SetDevice(ControllerType.Xbox);
                    ApplyStepVisuals(currentStep);
                }
            }

            if (Keyboard.current != null && Keyboard.current.wasUpdatedThisFrame)
            {
                if (currentDevice != ControllerType.Keyboard)
                {
                    SetDevice(ControllerType.Keyboard);
                    ApplyStepVisuals(currentStep);
                }
            }
        }

        private void SetDevice(ControllerType type)
        {
            currentDevice = type;
            bool isGamepad = type != ControllerType.Keyboard;

            if (controllerContainer != null) controllerContainer.SetActive(isGamepad);
            if (keyboardContainer != null) keyboardContainer.SetActive(!isGamepad);

            // CanvasGroup-based instant alpha toggle (if assigned)
            SetCanvasGroupVisible(keyboardCanvasGroup, !isGamepad);
        }

        #region Step-By-Step Outline & Visual Management

        private void ApplyStepVisuals(TutorialStep step)
        {
            ClearAllOutlines();

            bool isGamepad = currentDevice != ControllerType.Keyboard;
            bool useTopBase = false;

            string actionText = "";

            switch (step)
            {
                case TutorialStep.Move:
                    actionText = isGamepad ? "1. MOVE: Use LEFT STICK or D-PAD" : "1. MOVE: Press [A] and [D]";
                    useTopBase = false;
                    if (isGamepad)
                    {
                        AddOutlineToImage(controllerStick1Image);
                        AddOutlineToImage(controllerDpadImage);
                    }
                    else
                    {
                        if (keyboardDisplay != null) keyboardDisplay.HighlightKeys(outlineMaterial, "a", "d");
                        else { AddOutlineToImage(keyMoveLeftImage); AddOutlineToImage(keyMoveRightImage); }
                    }
                    break;

                case TutorialStep.Jump:
                    actionText = isGamepad ? "2. JUMP: Press button [A]" : "2. JUMP: Press [SPACE]";
                    useTopBase = false;
                    if (isGamepad)
                    {
                        AddOutlineToImage(controllerButtonAImage);
                    }
                    else
                    {
                        if (keyboardDisplay != null) keyboardDisplay.HighlightKeys(outlineMaterial, "space");
                        else AddOutlineToImage(keySpaceImage);
                    }
                    break;

                case TutorialStep.Slide:
                    actionText = isGamepad ? "3. SLIDE: Press trigger [LT]" : "3. SLIDE: Press [S] or [CTRL]";
                    useTopBase = false;
                    if (isGamepad)
                    {
                        AddOutlineToImage(controllerL2Image);
                    }
                    else
                    {
                        if (keyboardDisplay != null) keyboardDisplay.HighlightKeys(outlineMaterial, "s", "ctrl");
                        else { AddOutlineToImage(keySlideImage); AddOutlineToImage(keyCtrlImage); }
                    }
                    break;

                case TutorialStep.Kick:
                    actionText = isGamepad ? "4. KICK: Press button [X]" : "4. KICK: Press [Q]";
                    useTopBase = false;
                    if (isGamepad)
                    {
                        AddOutlineToImage(controllerButtonXImage);
                    }
                    else
                    {
                        if (keyboardDisplay != null) keyboardDisplay.HighlightKeys(outlineMaterial, "q");
                        else AddOutlineToImage(keyKickAltImage != null ? keyKickAltImage : keyKickImage);
                    }
                    break;

                case TutorialStep.WallRun:
                    actionText = isGamepad ? "5. WALL RUN: Hold [RT]" : "5. WALL RUN: Hold [SHIFT]";
                    useTopBase = true; // Top View (Base2)
                    if (isGamepad)
                    {
                        AddOutlineToImage(controllerR2Image);
                    }
                    else
                    {
                        if (keyboardDisplay != null) keyboardDisplay.HighlightKeys(outlineMaterial, "shift");
                        else AddOutlineToImage(keyShiftImage);
                    }
                    break;

                case TutorialStep.DropDown:
                    actionText = isGamepad ? "6. DROP DOWN: Press button [LB]" : "6. DROP DOWN: Press [C]";
                    useTopBase = true; // Top View (Base2)
                    if (isGamepad)
                    {
                        AddOutlineToImage(controllerL1Image);
                    }
                    else
                    {
                        if (keyboardDisplay != null) keyboardDisplay.HighlightKeys(outlineMaterial, "c");
                        else AddOutlineToImage(keyDropDownImage);
                    }
                    break;

                case TutorialStep.AirDodge:
                    actionText = isGamepad ? "7. AIR DODGE: Press button [RB]" : "7. AIR DODGE: Press [V]";
                    useTopBase = true; // Top View (Base2)
                    if (isGamepad)
                    {
                        AddOutlineToImage(controllerR1Image);
                    }
                    else
                    {
                        if (keyboardDisplay != null) keyboardDisplay.HighlightKeys(outlineMaterial, "v");
                        else AddOutlineToImage(keyAirDodgeImage);
                    }
                    break;

                case TutorialStep.Dive:
                    actionText = isGamepad ? "8. DIVE: Press button [Y]" : "8. DIVE: Press [E]";
                    useTopBase = false; // Front View (Base)
                    if (isGamepad)
                    {
                        AddOutlineToImage(controllerButtonYImage);
                    }
                    else
                    {
                        if (keyboardDisplay != null) keyboardDisplay.HighlightKeys(outlineMaterial, "e");
                        else AddOutlineToImage(keyDiveImage);
                    }
                    break;

                case TutorialStep.Completed:
                    actionText = "DONE! ALL CONTROLS MASTERED";
                    useTopBase = false;
                    if (startLevelButton != null) startLevelButton.gameObject.SetActive(true);
                    break;
            }

            // Update Bases (Front vs Top) — SetActive + optional CanvasGroup alpha
            if (baseFrontObject != null) baseFrontObject.SetActive(!useTopBase);
            if (baseTopObject != null)   baseTopObject.SetActive(useTopBase);
            SetCanvasGroupVisible(baseFrontCanvasGroup, !useTopBase);
            SetCanvasGroupVisible(baseTopCanvasGroup,   useTopBase);

            if (deviceStatusText != null)
            {
                deviceStatusText.text = actionText;
            }

            // Trigger preview demonstration for this step
            if (playerPreview != null)
            {
                switch (step)
                {
                    case TutorialStep.Move:     playerPreview.PlayMove(true); break;
                    case TutorialStep.Jump:     playerPreview.TriggerJump(); break;
                    case TutorialStep.Slide:    playerPreview.TriggerSlide(); break;
                    case TutorialStep.Kick:     playerPreview.TriggerKick(); break;
                    case TutorialStep.WallRun:  playerPreview.TriggerWallRun(); break;
                    case TutorialStep.DropDown: playerPreview.TriggerDropDown(); break;
                    case TutorialStep.AirDodge: playerPreview.TriggerAirDodge(); break;
                    case TutorialStep.Dive:     playerPreview.TriggerDive(); break;
                }
            }
        }

        /// <summary>Helper: instantly sets a CanvasGroup's alpha and interaction flags.</summary>
        private static void SetCanvasGroupVisible(CanvasGroup cg, bool visible)
        {
            if (cg == null) return;
            cg.alpha = visible ? 1f : 0f;
            cg.blocksRaycasts = visible;
            cg.interactable = visible;
        }

        private void AddOutlineToImage(Image img)
        {
            if (img == null) return;

            if (outlineMaterial != null)
            {
                img.material = outlineMaterial;
            }

            if (!currentlyOutlinedImages.Contains(img))
            {
                currentlyOutlinedImages.Add(img);
            }
        }

        /// <summary>Removes outline material from an Image when pressed.</summary>
        private void RemoveOutlineToImage(Image img)
        {
            if (img == null) return;
            img.material = null;
            currentlyOutlinedImages.Remove(img);
        }

        /// <summary>Removes highlight from a key on the visual keyboard AND/OR its fallback loose Image.</summary>
        private void RemoveKeyHighlight(string keyName, Image fallbackImage)
        {
            if (keyboardDisplay != null)
            {
                keyboardDisplay.RemoveKeyHighlight(keyName);
            }
            if (fallbackImage != null)
            {
                RemoveOutlineToImage(fallbackImage);
            }
        }

        /// <summary>Returns true if there are still active outlines in the current step.</summary>
        private bool HasRemainingOutlines()
        {
            if (currentlyOutlinedImages.Count > 0) return true;
            if (keyboardDisplay != null && keyboardDisplay.RemainingHighlightCount > 0) return true;
            return false;
        }

        /// <summary>Handles step progression when a valid key/button is pressed.</summary>
        private void OnStepInputPressed(TutorialStep step)
        {
            if (currentStep != step) return;

            if (requireAllKeysPerStep)
            {
                // Only advance once all highlighted keys/buttons for this step are pressed
                if (!HasRemainingOutlines())
                {
                    AdvanceStep(step);
                }
            }
            else
            {
                // Advance immediately upon pressing at least one valid input
                AdvanceStep(step);
            }
        }

        private void ClearAllOutlines()
        {
            foreach (var img in currentlyOutlinedImages)
            {
                if (img != null)
                {
                    img.material = null;
                }
            }
            currentlyOutlinedImages.Clear();

            // Also clear the QWERTY visual keyboard if assigned
            keyboardDisplay?.ClearHighlights();
        }

        private void AdvanceStep(TutorialStep completedStep)
        {
            completedSteps.Add(completedStep);

            if (stepTransitionCoroutine != null) StopCoroutine(stepTransitionCoroutine);
            stepTransitionCoroutine = StartCoroutine(TransitionToNextStepRoutine());
        }

        private IEnumerator TransitionToNextStepRoutine()
        {
            yield return new WaitForSecondsRealtime(0.45f);

            int next = (int)currentStep + 1;
            if (next <= (int)TutorialStep.Dive)
            {
                currentStep = (TutorialStep)next;
                ApplyStepVisuals(currentStep);
            }
            else
            {
                currentStep = TutorialStep.Completed;
                ApplyStepVisuals(currentStep);
            }
            stepTransitionCoroutine = null;
        }

        #endregion

        #region Live Input Detection

        private void CheckLiveInputs()
        {
            // 1. MOVE (A/D or Stick / DPad)
            bool movePressed = false;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                {
                    movePressed = true;
                    if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame)
                    {
                        RemoveKeyHighlight("a", keyMoveLeftImage);
                        OnStepInputPressed(TutorialStep.Move);
                    }
                }
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                {
                    movePressed = true;
                    if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame)
                    {
                        RemoveKeyHighlight("d", keyMoveRightImage);
                        OnStepInputPressed(TutorialStep.Move);
                    }
                }
            }
            if (Gamepad.current != null)
            {
                if (Mathf.Abs(Gamepad.current.leftStick.x.ReadValue()) > 0.3f)
                {
                    movePressed = true;
                    RemoveOutlineToImage(controllerStick1Image);
                    OnStepInputPressed(TutorialStep.Move);
                }
                if (Gamepad.current.dpad.left.isPressed || Gamepad.current.dpad.right.isPressed)
                {
                    movePressed = true;
                    RemoveOutlineToImage(controllerDpadImage);
                    OnStepInputPressed(TutorialStep.Move);
                }
            }

            if (playerPreview != null)
            {
                playerPreview.SetMoving(movePressed);
            }

            // 2. JUMP (Space, W, or Button A)
            if (Keyboard.current != null)
            {
                if (Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    RemoveKeyHighlight("space", keySpaceImage);
                    if (playerPreview != null) playerPreview.TriggerJump();
                    OnStepInputPressed(TutorialStep.Jump);
                }
                if (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame)
                {
                    RemoveKeyHighlight("w", keyMoveUpImage);
                    if (playerPreview != null) playerPreview.TriggerJump();
                    OnStepInputPressed(TutorialStep.Jump);
                }
            }
            if (Gamepad.current != null)
            {
                if (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.leftStick.y.ReadValue() > 0.6f)
                {
                    RemoveOutlineToImage(controllerButtonAImage);
                    if (playerPreview != null) playerPreview.TriggerJump();
                    OnStepInputPressed(TutorialStep.Jump);
                }
            }

            // 3. SLIDE (S, Left Ctrl, or Button B / LT)
            if (Keyboard.current != null)
            {
                if (Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame)
                {
                    RemoveKeyHighlight("s", keySlideImage);
                    if (playerPreview != null) playerPreview.TriggerSlide();
                    OnStepInputPressed(TutorialStep.Slide);
                }
                if (Keyboard.current.leftCtrlKey.wasPressedThisFrame || Keyboard.current.rightCtrlKey.wasPressedThisFrame)
                {
                    RemoveKeyHighlight("ctrl", keyCtrlImage);
                    if (playerPreview != null) playerPreview.TriggerSlide();
                    OnStepInputPressed(TutorialStep.Slide);
                }
            }
            if (Gamepad.current != null)
            {
                if (Gamepad.current.buttonEast.wasPressedThisFrame)
                {
                    RemoveOutlineToImage(controllerButtonBImage);
                    if (playerPreview != null) playerPreview.TriggerSlide();
                    OnStepInputPressed(TutorialStep.Slide);
                }
                if (Gamepad.current.leftTrigger.wasPressedThisFrame || Gamepad.current.leftTrigger.ReadValue() > 0.4f)
                {
                    RemoveOutlineToImage(controllerL2Image);
                    if (playerPreview != null) playerPreview.TriggerSlide();
                    OnStepInputPressed(TutorialStep.Slide);
                }
            }

            // 4. KICK (Q, Button X)
            if (Keyboard.current != null)
            {
                if (Keyboard.current.qKey.wasPressedThisFrame)
                {
                    RemoveKeyHighlight("q", keyKickAltImage != null ? keyKickAltImage : keyKickImage);
                    if (playerPreview != null) playerPreview.TriggerKick();
                    OnStepInputPressed(TutorialStep.Kick);
                }
                else if (Keyboard.current.fKey.wasPressedThisFrame)
                {
                    RemoveKeyHighlight("f", keyKickImage);
                    if (playerPreview != null) playerPreview.TriggerKick();
                    OnStepInputPressed(TutorialStep.Kick);
                }
            }
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                RemoveOutlineToImage(mouseLeftClickImage);
                if (playerPreview != null) playerPreview.TriggerKick();
                OnStepInputPressed(TutorialStep.Kick);
            }
            if (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame)
            {
                RemoveOutlineToImage(controllerButtonXImage);
                if (playerPreview != null) playerPreview.TriggerKick();
                OnStepInputPressed(TutorialStep.Kick);
            }

            // 5. WALL RUN (Left Shift, RT / R2)
            if (Keyboard.current != null && (Keyboard.current.leftShiftKey.wasPressedThisFrame || Keyboard.current.rightShiftKey.wasPressedThisFrame))
            {
                RemoveKeyHighlight("shift", keyShiftImage);
                if (playerPreview != null) playerPreview.TriggerWallRun();
                OnStepInputPressed(TutorialStep.WallRun);
            }
            if (Gamepad.current != null && (Gamepad.current.rightTrigger.wasPressedThisFrame || Gamepad.current.rightTrigger.ReadValue() > 0.3f))
            {
                RemoveOutlineToImage(controllerR2Image);
                if (playerPreview != null) playerPreview.TriggerWallRun();
                OnStepInputPressed(TutorialStep.WallRun);
            }

            // 6. DROP DOWN (C, LB / L1)
            if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
            {
                RemoveKeyHighlight("c", keyDropDownImage);
                if (playerPreview != null) playerPreview.TriggerDropDown();
                OnStepInputPressed(TutorialStep.DropDown);
            }
            if (Gamepad.current != null && Gamepad.current.leftShoulder.wasPressedThisFrame)
            {
                RemoveOutlineToImage(controllerL1Image);
                if (playerPreview != null) playerPreview.TriggerDropDown();
                OnStepInputPressed(TutorialStep.DropDown);
            }

            // 7. AIR DODGE (V, RB / R1)
            if (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
            {
                RemoveKeyHighlight("v", keyAirDodgeImage);
                if (playerPreview != null) playerPreview.TriggerAirDodge();
                OnStepInputPressed(TutorialStep.AirDodge);
            }
            if (Gamepad.current != null && Gamepad.current.rightShoulder.wasPressedThisFrame)
            {
                RemoveOutlineToImage(controllerR1Image);
                if (playerPreview != null) playerPreview.TriggerAirDodge();
                OnStepInputPressed(TutorialStep.AirDodge);
            }

            // 8. DIVE (E, Y / Button North)
            if (Keyboard.current != null)
            {
                if (Keyboard.current.eKey.wasPressedThisFrame)
                {
                    RemoveKeyHighlight("e", keyDiveImage);
                    if (playerPreview != null) playerPreview.TriggerDive();
                    OnStepInputPressed(TutorialStep.Dive);
                }
                else if (Keyboard.current.bKey.wasPressedThisFrame)
                {
                    RemoveKeyHighlight("b", keyDiveImage);
                    if (playerPreview != null) playerPreview.TriggerDive();
                    OnStepInputPressed(TutorialStep.Dive);
                }
            }
            if (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame)
            {
                RemoveOutlineToImage(controllerButtonYImage);
                if (playerPreview != null) playerPreview.TriggerDive();
                OnStepInputPressed(TutorialStep.Dive);
            }
        }

        #endregion

        private void OnStartClicked()
        {
            HideTutorial();
        }

        private void OnSkipClicked()
        {
            HideTutorial();
        }

        private IEnumerator FadeRoutine(float targetAlpha, bool enableInteraction, Action onEnd = null)
        {
            if (mainCanvasGroup == null)
            {
                onEnd?.Invoke();
                yield break;
            }

            float startAlpha = mainCanvasGroup.alpha;
            float elapsed = 0f;

            if (enableInteraction)
            {
                mainCanvasGroup.blocksRaycasts = true;
                mainCanvasGroup.interactable = true;
            }

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                mainCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
                yield return null;
            }

            mainCanvasGroup.alpha = targetAlpha;

            if (!enableInteraction)
            {
                mainCanvasGroup.blocksRaycasts = false;
                mainCanvasGroup.interactable = false;
            }

            onEnd?.Invoke();
        }
    }
}

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

        [Header("Virtual QWERTY Keyboard (optional)")]
        [Tooltip("VirtualKeyboardOverlay placed on the VirtualCanvasGroup. When assigned, replaces loose key images for keyboard highlighting.")]
        [SerializeField] private VirtualKeyboardOverlay virtualKeyboard;

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

        // Colors
        private readonly Color normalLayerColor = new Color(1f, 1f, 1f, 0.45f);
        private readonly Color activeLayerColor = new Color(1f, 1f, 1f, 1f);
        private readonly Color completedColor   = new Color(0.35f, 0.95f, 0.45f, 1f);

        // Sequence State
        private TutorialStep currentStep = TutorialStep.Move;
        private HashSet<TutorialStep> completedSteps = new HashSet<TutorialStep>();
        private Coroutine stepTransitionCoroutine;

        private Action onCompleteCallback;
        private bool isShowing = false;
        private ControllerType currentDevice = ControllerType.Keyboard;
        private List<Image> currentlyOutlinedImages = new List<Image>();

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

            if (startLevelButton != null)
            {
                startLevelButton.onClick.AddListener(OnStartClicked);
                startLevelButton.gameObject.SetActive(false);
            }

            if (skipButton != null)
            {
                skipButton.onClick.AddListener(OnSkipClicked);
            }
        }

        public static bool ShouldShowTutorial()
        {
            if (Instance != null && Instance.forceShowForTesting) return true;
            return PlayerPrefs.GetInt(TUTORIAL_DONE_PREF, 0) == 0;
        }

        public bool ShouldShowTutorialInstance()
        {
            if (forceShowForTesting) return true;
            return PlayerPrefs.GetInt(TUTORIAL_DONE_PREF, 0) == 0;
        }

        public void ShowTutorial(Action onComplete)
        {
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
            else if (Gamepad.current != null && (Gamepad.current.startButton.wasPressedThisFrame || Gamepad.current.selectButton.wasPressedThisFrame))
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
                    actionText = isGamepad ? "1. MOVERSE: Usa PALANCA IZQUIERDA o FLECHAS" : "1. MOVERSE: Presiona teclas [A] y [D]";
                    useTopBase = false;
                    if (isGamepad)
                    {
                        AddOutlineToImage(controllerStick1Image);
                        AddOutlineToImage(controllerDpadImage);
                    }
                    else
                    {
                        if (virtualKeyboard != null) virtualKeyboard.HighlightKeys(outlineMaterial, "a", "d");
                        else { AddOutlineToImage(keyMoveLeftImage); AddOutlineToImage(keyMoveRightImage); }
                    }
                    break;

                case TutorialStep.Jump:
                    actionText = isGamepad ? "2. SALTAR: Presiona el botón [A]" : "2. SALTAR: Presiona [BARRA ESPACIADORA] o [W]";
                    useTopBase = false;
                    if (isGamepad)
                    {
                        AddOutlineToImage(controllerButtonAImage);
                    }
                    else
                    {
                        if (virtualKeyboard != null) virtualKeyboard.HighlightKeys(outlineMaterial, "space", "w");
                        else { AddOutlineToImage(keySpaceImage); AddOutlineToImage(keyMoveUpImage); }
                    }
                    break;

                case TutorialStep.Slide:
                    actionText = isGamepad ? "3. DESLIZARSE: Presiona [B] o gatillo [LT]" : "3. DESLIZARSE: Presiona tecla [S] o [CTRL]";
                    useTopBase = false;
                    if (isGamepad)
                    {
                        AddOutlineToImage(controllerButtonBImage);
                    }
                    else
                    {
                        if (virtualKeyboard != null) virtualKeyboard.HighlightKeys(outlineMaterial, "s", "ctrl");
                        else { AddOutlineToImage(keySlideImage); AddOutlineToImage(keyCtrlImage); }
                    }
                    break;

                case TutorialStep.Kick:
                    actionText = isGamepad ? "4. PATEAR: Presiona botón [X]" : "4. PATEAR: Presiona [F], [Q] o [CLIC IZQ]";
                    useTopBase = false;
                    if (isGamepad)
                    {
                        AddOutlineToImage(controllerButtonXImage);
                    }
                    else
                    {
                        if (virtualKeyboard != null) virtualKeyboard.HighlightKeys(outlineMaterial, "f", "q");
                        else { AddOutlineToImage(keyKickImage); AddOutlineToImage(keyKickAltImage); }
                        AddOutlineToImage(mouseLeftClickImage); // mouse image always shown separately
                    }
                    break;

                case TutorialStep.WallRun:
                    actionText = isGamepad ? "5. CORRER POR PARED: Mantén presionado [RT]" : "5. CORRER POR PARED: Mantén presionado [SHIFT]";
                    useTopBase = true; // Top View (Base2)
                    if (isGamepad)
                    {
                        AddOutlineToImage(controllerR2Image);
                    }
                    else
                    {
                        if (virtualKeyboard != null) virtualKeyboard.HighlightKeys(outlineMaterial, "shift");
                        else AddOutlineToImage(keyShiftImage);
                    }
                    break;

                case TutorialStep.DropDown:
                    actionText = isGamepad ? "6. BAJAR PLATAFORMA: Presiona botón [LB]" : "6. BAJAR PLATAFORMA: Presiona tecla [C]";
                    useTopBase = true; // Top View (Base2)
                    if (isGamepad)
                    {
                        AddOutlineToImage(controllerL1Image);
                    }
                    else
                    {
                        if (virtualKeyboard != null) virtualKeyboard.HighlightKeys(outlineMaterial, "c");
                        else AddOutlineToImage(keyDropDownImage);
                    }
                    break;

                case TutorialStep.AirDodge:
                    actionText = isGamepad ? "7. ESQUIVE EN AIRE: Presiona botón [RB]" : "7. ESQUIVE EN AIRE: Presiona tecla [V]";
                    useTopBase = true; // Top View (Base2)
                    if (isGamepad)
                    {
                        AddOutlineToImage(controllerR1Image);
                    }
                    else
                    {
                        if (virtualKeyboard != null) virtualKeyboard.HighlightKeys(outlineMaterial, "v");
                        else AddOutlineToImage(keyAirDodgeImage);
                    }
                    break;

                case TutorialStep.Dive:
                    actionText = isGamepad ? "8. PICADO AL SUELO: Presiona botón [Y]" : "8. PICADO AL SUELO: Presiona tecla [B]";
                    useTopBase = false; // Front View (Base)
                    if (isGamepad)
                    {
                        AddOutlineToImage(controllerButtonYImage);
                    }
                    else
                    {
                        if (virtualKeyboard != null) virtualKeyboard.HighlightKeys(outlineMaterial, "b");
                        else AddOutlineToImage(keyDiveImage);
                    }
                    break;

                case TutorialStep.Completed:
                    actionText = "¡LISTO! TODOS LOS CONTROLES DOMINADOS";
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
            img.color = activeLayerColor;

            if (!currentlyOutlinedImages.Contains(img))
            {
                currentlyOutlinedImages.Add(img);
            }
        }

        private void ClearAllOutlines()
        {
            foreach (var img in currentlyOutlinedImages)
            {
                if (img != null)
                {
                    img.material = null;
                    img.color = normalLayerColor;
                }
            }
            currentlyOutlinedImages.Clear();

            // Also clear the QWERTY virtual keyboard if assigned
            virtualKeyboard?.ClearHighlights();
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
                if (Keyboard.current.aKey.isPressed || Keyboard.current.dKey.isPressed ||
                    Keyboard.current.leftArrowKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                    movePressed = true;
            }
            if (Gamepad.current != null)
            {
                if (Mathf.Abs(Gamepad.current.leftStick.x.ReadValue()) > 0.3f ||
                    Gamepad.current.dpad.left.isPressed || Gamepad.current.dpad.right.isPressed)
                    movePressed = true;
            }

            if (playerPreview != null)
            {
                playerPreview.SetMoving(movePressed);
            }

            if (movePressed && currentStep == TutorialStep.Move)
            {
                AdvanceStep(TutorialStep.Move);
            }

            // 2. JUMP (Space, W, or Button A)
            bool jumpJustPressed = false;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame) jumpJustPressed = true;
            }
            if (Gamepad.current != null)
            {
                if (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.leftStick.y.ReadValue() > 0.6f) jumpJustPressed = true;
            }

            if (jumpJustPressed)
            {
                if (playerPreview != null) playerPreview.TriggerJump();
                if (currentStep == TutorialStep.Jump) AdvanceStep(TutorialStep.Jump);
            }

            // 3. SLIDE (S, Left Ctrl, or Button B / LT)
            bool slideJustPressed = false;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.leftCtrlKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame) slideJustPressed = true;
            }
            if (Gamepad.current != null)
            {
                if (Gamepad.current.buttonEast.wasPressedThisFrame || Gamepad.current.leftTrigger.wasPressedThisFrame || Gamepad.current.leftTrigger.ReadValue() > 0.4f) slideJustPressed = true;
            }

            if (slideJustPressed)
            {
                if (playerPreview != null) playerPreview.TriggerSlide();
                if (currentStep == TutorialStep.Slide) AdvanceStep(TutorialStep.Slide);
            }

            // 4. KICK (F, Q, Left Click, Button X)
            bool kickJustPressed = false;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.fKey.wasPressedThisFrame || Keyboard.current.qKey.wasPressedThisFrame) kickJustPressed = true;
            }
            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame) kickJustPressed = true;
            }
            if (Gamepad.current != null)
            {
                if (Gamepad.current.buttonWest.wasPressedThisFrame) kickJustPressed = true;
            }

            if (kickJustPressed)
            {
                if (playerPreview != null) playerPreview.TriggerKick();
                if (currentStep == TutorialStep.Kick) AdvanceStep(TutorialStep.Kick);
            }

            // 5. WALL RUN (Left Shift, RT / R2)
            bool wallRunJustPressed = false;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.leftShiftKey.wasPressedThisFrame) wallRunJustPressed = true;
            }
            if (Gamepad.current != null)
            {
                if (Gamepad.current.rightTrigger.wasPressedThisFrame || Gamepad.current.rightTrigger.ReadValue() > 0.3f) wallRunJustPressed = true;
            }

            if (wallRunJustPressed)
            {
                if (playerPreview != null) playerPreview.TriggerWallRun();
                if (currentStep == TutorialStep.WallRun) AdvanceStep(TutorialStep.WallRun);
            }

            // 6. DROP DOWN (C, LB / L1)
            bool dropDownJustPressed = false;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.cKey.wasPressedThisFrame) dropDownJustPressed = true;
            }
            if (Gamepad.current != null)
            {
                if (Gamepad.current.leftShoulder.wasPressedThisFrame) dropDownJustPressed = true;
            }

            if (dropDownJustPressed)
            {
                if (playerPreview != null) playerPreview.TriggerDropDown();
                if (currentStep == TutorialStep.DropDown) AdvanceStep(TutorialStep.DropDown);
            }

            // 7. AIR DODGE (V, RB / R1)
            bool airDodgeJustPressed = false;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.vKey.wasPressedThisFrame) airDodgeJustPressed = true;
            }
            if (Gamepad.current != null)
            {
                if (Gamepad.current.rightShoulder.wasPressedThisFrame) airDodgeJustPressed = true;
            }

            if (airDodgeJustPressed)
            {
                if (playerPreview != null) playerPreview.TriggerAirDodge();
                if (currentStep == TutorialStep.AirDodge) AdvanceStep(TutorialStep.AirDodge);
            }

            // 8. DIVE (B, Y / Button North)
            bool diveJustPressed = false;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.bKey.wasPressedThisFrame) diveJustPressed = true;
            }
            if (Gamepad.current != null)
            {
                if (Gamepad.current.buttonNorth.wasPressedThisFrame) diveJustPressed = true;
            }

            if (diveJustPressed)
            {
                if (playerPreview != null) playerPreview.TriggerDive();
                if (currentStep == TutorialStep.Dive) AdvanceStep(TutorialStep.Dive);
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

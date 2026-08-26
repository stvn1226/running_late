using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

namespace UI.GamePanels
{
    /// <summary>
    /// Interactive Controls Tutorial Overlay displaying the dedicated layered artwork from Assets/Sprites/ControlsOverlay.
    /// Dynamically toggles between Controller diagram and Keyboard diagram.
    /// Highlights buttons/sticks in real-time and plays interactive player preview animations as actions are tested.
    /// </summary>
    public class ControlsTutorialOverlay : MonoBehaviour
    {
        public static ControlsTutorialOverlay Instance { get; private set; }

        private const string TUTORIAL_DONE_PREF = "ControlsTutorial_Done";

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

        [Header("Controller Artwork Layers")]
        [SerializeField] private Image controllerBaseImage;
        [SerializeField] private Image controllerStick1Image; // Palanca 1 (Move)
        [SerializeField] private Image controllerStick2Image; // Palanca 2 (Camera)
        [SerializeField] private Image controllerDpadImage;   // Flechas (Move)
        [SerializeField] private Image controllerButtonAImage; // A (Jump)
        [SerializeField] private Image controllerButtonBImage; // B (Slide)
        [SerializeField] private Image controllerButtonXImage; // X (Kick)
        [SerializeField] private Image controllerButtonYImage; // Y (Dive)
        [SerializeField] private Image controllerSelectImage;  // Select (Reset)
        [SerializeField] private Image controllerStartImage;   // Start (Pause)
        [SerializeField] private Image controllerL1Image;      // L1 (Drop Down)
        [SerializeField] private Image controllerR1Image;      // R1 (Air Dodge)
        [SerializeField] private Image controllerL2Image;      // L2 (Crouch / Slide)
        [SerializeField] private Image controllerR2Image;      // R2 (Wall Run)

        [Header("Keyboard & Mouse Artwork Layers")]
        [SerializeField] private Image keyMoveLeftImage;   // A
        [SerializeField] private Image keyMoveRightImage;  // D
        [SerializeField] private Image keyMoveUpImage;     // W (Climb)
        [SerializeField] private Image keySlideImage;      // S
        [SerializeField] private Image keySpaceImage;      // Barra espaciadora (Jump)
        [SerializeField] private Image keyKickImage;       // F
        [SerializeField] private Image keyKickAltImage;    // Q
        [SerializeField] private Image keyShiftImage;      // Shift (Wall Run)
        [SerializeField] private Image keyCtrlImage;       // Ctrl (Slide)
        [SerializeField] private Image keyDropDownImage;   // C (Drop Down)
        [SerializeField] private Image keyAirDodgeImage;   // V (Air Dodge)
        [SerializeField] private Image keyDiveImage;       // B (Dive)
        [SerializeField] private Image mouseLeftClickImage;  // Click Izquierdo (Kick)
        [SerializeField] private Image mouseRightClickImage; // Click Derecho (Camera / Pan)

        [Header("Interactive Animated Player Preview")]
        [SerializeField] private UIPlayerPreview playerPreview;

        [Header("Action Badges & Checkmarks")]
        [SerializeField] private GameObject badgeMoveCheck;
        [SerializeField] private GameObject badgeJumpCheck;
        [SerializeField] private GameObject badgeSlideCheck;
        [SerializeField] private GameObject badgeKickCheck;

        [Header("Buttons")]
        [SerializeField] private Button startLevelButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private TextMeshProUGUI startButtonText;

        // Colors
        private readonly Color normalLayerColor = new Color(1f, 1f, 1f, 0.4f);
        private readonly Color activeLayerColor = new Color(0.2f, 0.95f, 0.4f, 1f);
        private readonly Color highlightColor   = new Color(1f, 0.85f, 0.2f, 1f);

        // Action Completion State
        private bool testedMove = false;
        private bool testedJump = false;
        private bool testedSlide = false;
        private bool testedKick = false;

        private Action onCompleteCallback;
        private bool isShowing = false;
        private ControllerType currentDevice = ControllerType.Keyboard;

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

        public bool ShouldShowTutorial()
        {
            if (forceShowForTesting) return true;
            return PlayerPrefs.GetInt(TUTORIAL_DONE_PREF, 0) == 0;
        }

        public void ShowTutorial(Action onComplete)
        {
            onCompleteCallback = onComplete;
            isShowing = true;

            ResetTestedState();
            DetectInitialDevice();
            StartCoroutine(FadeRoutine(1f, true));
        }

        public void HideTutorial()
        {
            if (!isShowing) return;
            isShowing = false;

            PlayerPrefs.SetInt(TUTORIAL_DONE_PREF, 1);
            PlayerPrefs.Save();

            StartCoroutine(FadeRoutine(0f, false, () =>
            {
                onCompleteCallback?.Invoke();
                onCompleteCallback = null;
            }));
        }

        private void ResetTestedState()
        {
            testedMove = false;
            testedJump = false;
            testedSlide = false;
            testedKick = false;

            if (badgeMoveCheck != null) badgeMoveCheck.SetActive(false);
            if (badgeJumpCheck != null) badgeJumpCheck.SetActive(false);
            if (badgeSlideCheck != null) badgeSlideCheck.SetActive(false);
            if (badgeKickCheck != null) badgeKickCheck.SetActive(false);

            if (startLevelButton != null) startLevelButton.gameObject.SetActive(false);
            if (skipButton != null) skipButton.gameObject.SetActive(true);
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
                if (AreAllTested()) OnStartClicked();
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
                }
            }

            if (Keyboard.current != null && Keyboard.current.wasUpdatedThisFrame)
            {
                if (currentDevice != ControllerType.Keyboard)
                {
                    SetDevice(ControllerType.Keyboard);
                }
            }
        }

        private void SetDevice(ControllerType type)
        {
            currentDevice = type;
            bool isGamepad = type != ControllerType.Keyboard;

            if (controllerContainer != null) controllerContainer.SetActive(isGamepad);
            if (keyboardContainer != null) keyboardContainer.SetActive(!isGamepad);

            if (deviceStatusText != null)
            {
                deviceStatusText.text = isGamepad ? $"MANDO DETECTADO: {type.ToString().ToUpper()}" : "TECLADO Y RATÓN DETECTADO";
            }
        }

        private void CheckLiveInputs()
        {
            // 1. MOVE (A/D or Stick Left/Right / DPad)
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

            if (movePressed)
            {
                if (!testedMove) { testedMove = true; TriggerActionFeedback("MOVE"); }
                HighlightLayer(controllerStick1Image, true);
                HighlightLayer(controllerDpadImage, true);
                HighlightLayer(keyMoveLeftImage, true);
                HighlightLayer(keyMoveRightImage, true);
            }
            else
            {
                HighlightLayer(controllerStick1Image, false, testedMove);
                HighlightLayer(controllerDpadImage, false, testedMove);
                HighlightLayer(keyMoveLeftImage, false, testedMove);
                HighlightLayer(keyMoveRightImage, false, testedMove);
            }

            // 2. JUMP (Space, W, or Button A)
            bool jumpPressed = false;
            bool jumpJustPressed = false;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame) jumpJustPressed = true;
                if (Keyboard.current.spaceKey.isPressed || Keyboard.current.wKey.isPressed) jumpPressed = true;
            }
            if (Gamepad.current != null)
            {
                if (Gamepad.current.buttonSouth.wasPressedThisFrame) jumpJustPressed = true;
                if (Gamepad.current.buttonSouth.isPressed || Gamepad.current.leftStick.y.ReadValue() > 0.5f) jumpPressed = true;
            }

            if (jumpJustPressed && playerPreview != null)
            {
                playerPreview.TriggerJump();
            }

            if (jumpPressed)
            {
                if (!testedJump) { testedJump = true; TriggerActionFeedback("JUMP"); }
                HighlightLayer(controllerButtonAImage, true);
                HighlightLayer(keySpaceImage, true);
                HighlightLayer(keyMoveUpImage, true);
            }
            else
            {
                HighlightLayer(controllerButtonAImage, false, testedJump);
                HighlightLayer(keySpaceImage, false, testedJump);
                HighlightLayer(keyMoveUpImage, false, testedJump);
            }

            // 3. SLIDE (S or Stick Down / DPad Down / Button B / L2 / Ctrl)
            bool slidePressed = false;
            bool slideJustPressed = false;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.leftCtrlKey.wasPressedThisFrame) slideJustPressed = true;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.downArrowKey.isPressed) slidePressed = true;
            }
            if (Gamepad.current != null)
            {
                if (Gamepad.current.buttonEast.wasPressedThisFrame || Gamepad.current.leftTrigger.wasPressedThisFrame) slideJustPressed = true;
                if (Gamepad.current.leftStick.y.ReadValue() < -0.4f || Gamepad.current.dpad.down.isPressed || Gamepad.current.buttonEast.isPressed || Gamepad.current.leftTrigger.ReadValue() > 0.3f) slidePressed = true;
            }

            if (slideJustPressed && playerPreview != null)
            {
                playerPreview.TriggerSlide();
            }

            if (slidePressed)
            {
                if (!testedSlide) { testedSlide = true; TriggerActionFeedback("SLIDE"); }
                HighlightLayer(controllerButtonBImage, true);
                HighlightLayer(controllerL2Image, true);
                HighlightLayer(keySlideImage, true);
                HighlightLayer(keyCtrlImage, true);
            }
            else
            {
                HighlightLayer(controllerButtonBImage, false, testedSlide);
                HighlightLayer(controllerL2Image, false, testedSlide);
                HighlightLayer(keySlideImage, false, testedSlide);
                HighlightLayer(keyCtrlImage, false, testedSlide);
            }

            // 4. KICK (F, Q, Left Click, Button X)
            bool kickPressed = false;
            bool kickJustPressed = false;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.fKey.wasPressedThisFrame || Keyboard.current.qKey.wasPressedThisFrame) kickJustPressed = true;
                if (Keyboard.current.fKey.isPressed || Keyboard.current.qKey.isPressed) kickPressed = true;
            }
            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame) kickJustPressed = true;
                if (Mouse.current.leftButton.isPressed) kickPressed = true;
            }
            if (Gamepad.current != null)
            {
                if (Gamepad.current.buttonWest.wasPressedThisFrame) kickJustPressed = true;
                if (Gamepad.current.buttonWest.isPressed) kickPressed = true;
            }

            if (kickJustPressed && playerPreview != null)
            {
                playerPreview.TriggerKick();
            }

            if (kickPressed)
            {
                if (!testedKick) { testedKick = true; TriggerActionFeedback("KICK"); }
                HighlightLayer(controllerButtonXImage, true);
                HighlightLayer(keyKickImage, true);
                HighlightLayer(keyKickAltImage, true);
                HighlightLayer(mouseLeftClickImage, true);
            }
            else
            {
                HighlightLayer(controllerButtonXImage, false, testedKick);
                HighlightLayer(keyKickImage, false, testedKick);
                HighlightLayer(keyKickAltImage, false, testedKick);
                HighlightLayer(mouseLeftClickImage, false, testedKick);
            }

            // 5. WALL RUN (Left Shift, RT / R2)
            bool wallRunPressed = false;
            bool wallRunJustPressed = false;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.leftShiftKey.wasPressedThisFrame) wallRunJustPressed = true;
                if (Keyboard.current.leftShiftKey.isPressed) wallRunPressed = true;
            }
            if (Gamepad.current != null)
            {
                if (Gamepad.current.rightTrigger.wasPressedThisFrame) wallRunJustPressed = true;
                if (Gamepad.current.rightTrigger.ReadValue() > 0.3f) wallRunPressed = true;
            }

            if (wallRunJustPressed && playerPreview != null)
            {
                playerPreview.TriggerWallRun();
            }

            if (wallRunPressed)
            {
                HighlightLayer(controllerR2Image, true);
                HighlightLayer(keyShiftImage, true);
            }
            else
            {
                HighlightLayer(controllerR2Image, false);
                HighlightLayer(keyShiftImage, false);
            }

            // 6. AIR DODGE (V, RB / R1)
            bool airDodgePressed = false;
            bool airDodgeJustPressed = false;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.vKey.wasPressedThisFrame) airDodgeJustPressed = true;
                if (Keyboard.current.vKey.isPressed) airDodgePressed = true;
            }
            if (Gamepad.current != null)
            {
                if (Gamepad.current.rightShoulder.wasPressedThisFrame) airDodgeJustPressed = true;
                if (Gamepad.current.rightShoulder.isPressed) airDodgePressed = true;
            }

            if (airDodgeJustPressed && playerPreview != null)
            {
                playerPreview.TriggerAirDodge();
            }

            if (airDodgePressed)
            {
                HighlightLayer(controllerR1Image, true);
                HighlightLayer(keyAirDodgeImage, true);
            }
            else
            {
                HighlightLayer(controllerR1Image, false);
                HighlightLayer(keyAirDodgeImage, false);
            }

            // 7. DIVE (B, Y / Button North)
            bool divePressed = false;
            bool diveJustPressed = false;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.bKey.wasPressedThisFrame) diveJustPressed = true;
                if (Keyboard.current.bKey.isPressed) divePressed = true;
            }
            if (Gamepad.current != null)
            {
                if (Gamepad.current.buttonNorth.wasPressedThisFrame) diveJustPressed = true;
                if (Gamepad.current.buttonNorth.isPressed) divePressed = true;
            }

            if (diveJustPressed && playerPreview != null)
            {
                playerPreview.TriggerDive();
            }

            if (divePressed)
            {
                HighlightLayer(controllerButtonYImage, true);
                HighlightLayer(keyDiveImage, true);
            }
            else
            {
                HighlightLayer(controllerButtonYImage, false);
                HighlightLayer(keyDiveImage, false);
            }

            // 8. DROP DOWN (C, LB / L1)
            bool dropDownPressed = false;
            bool dropDownJustPressed = false;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.cKey.wasPressedThisFrame) dropDownJustPressed = true;
                if (Keyboard.current.cKey.isPressed) dropDownPressed = true;
            }
            if (Gamepad.current != null)
            {
                if (Gamepad.current.leftShoulder.wasPressedThisFrame) dropDownJustPressed = true;
                if (Gamepad.current.leftShoulder.isPressed) dropDownPressed = true;
            }

            if (dropDownJustPressed && playerPreview != null)
            {
                playerPreview.TriggerDropDown();
            }

            if (dropDownPressed)
            {
                HighlightLayer(controllerL1Image, true);
                HighlightLayer(keyDropDownImage, true);
            }
            else
            {
                HighlightLayer(controllerL1Image, false);
                HighlightLayer(keyDropDownImage, false);
            }

            // 9. PAN CAMERA (Right Stick, Mouse Right)
            if (Gamepad.current != null && Gamepad.current.rightStick.ReadValue().sqrMagnitude > 0.1f)
            {
                HighlightLayer(controllerStick2Image, true);
            }
            else
            {
                HighlightLayer(controllerStick2Image, false);
            }

            if (Mouse.current != null && Mouse.current.rightButton.isPressed)
            {
                HighlightLayer(mouseRightClickImage, true);
            }
            else
            {
                HighlightLayer(mouseRightClickImage, false);
            }

            // 10. SELECT / RESET & START / PAUSE
            if (Gamepad.current != null && Gamepad.current.selectButton.isPressed)
                HighlightLayer(controllerSelectImage, true);
            else
                HighlightLayer(controllerSelectImage, false);

            if (Gamepad.current != null && Gamepad.current.startButton.isPressed)
                HighlightLayer(controllerStartImage, true);
            else
                HighlightLayer(controllerStartImage, false);
        }

        private void HighlightLayer(Image img, bool active, bool permanentTested = false)
        {
            if (img == null) return;

            if (active)
            {
                img.color = activeLayerColor;
            }
            else if (permanentTested)
            {
                img.color = highlightColor;
            }
            else
            {
                img.color = normalLayerColor;
            }
        }

        private void TriggerActionFeedback(string actionName)
        {
            if (actionName == "MOVE" && badgeMoveCheck != null) badgeMoveCheck.SetActive(true);
            if (actionName == "JUMP" && badgeJumpCheck != null) badgeJumpCheck.SetActive(true);
            if (actionName == "SLIDE" && badgeSlideCheck != null) badgeSlideCheck.SetActive(true);
            if (actionName == "KICK" && badgeKickCheck != null) badgeKickCheck.SetActive(true);

            if (AreAllTested())
            {
                if (startLevelButton != null) startLevelButton.gameObject.SetActive(true);
            }
        }

        private bool AreAllTested()
        {
            return testedMove && testedJump && testedSlide && testedKick;
        }

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

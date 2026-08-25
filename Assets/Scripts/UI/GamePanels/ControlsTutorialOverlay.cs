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
    /// Highlights buttons/sticks in real-time as the player tests actions.
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

        [Header("Controller Artwork Layers (Assets/Sprites/ControlsOverlay/Controller Front)")]
        [SerializeField] private Image controllerBaseImage;
        [SerializeField] private Image controllerStick1Image; // Palanca 1 (Move)
        [SerializeField] private Image controllerDpadImage;   // Flechas (Move)
        [SerializeField] private Image controllerButtonAImage; // A (Jump)
        [SerializeField] private Image controllerButtonBImage; // B (Slide)
        [SerializeField] private Image controllerButtonXImage; // X (Kick)

        [Header("Keyboard Artwork Layers (Assets/Sprites/ControlsOverlay/Keyboard and Mouse)")]
        [SerializeField] private Image keyMoveLeftImage;   // A
        [SerializeField] private Image keyMoveRightImage;  // D
        [SerializeField] private Image keySlideImage;      // S
        [SerializeField] private Image keyKickImage;       // F
        [SerializeField] private Image keySpaceImage;      // Barra espaciadora (Jump)

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

        private void Start()
        {
            if (ControllerTypeDetector.Instance != null)
            {
                ControllerTypeDetector.Instance.OnControllerChanged += HandleControllerChanged;
                SetDevice(ControllerTypeDetector.Instance.CurrentControllerType);
            }
            else
            {
                SetDevice(ControllerType.Keyboard);
            }
        }

        private void OnDestroy()
        {
            if (ControllerTypeDetector.Instance != null)
            {
                ControllerTypeDetector.Instance.OnControllerChanged -= HandleControllerChanged;
            }
        }

        public static bool ShouldShowTutorial()
        {
            if (Instance != null && Instance.forceShowForTesting) return true;
            return PlayerPrefs.GetInt(TUTORIAL_DONE_PREF, 0) == 0;
        }

        public void ShowTutorial(Action onComplete)
        {
            onCompleteCallback = onComplete;
            isShowing = true;
            gameObject.SetActive(true);

            // Reset action states
            testedMove = false;
            testedJump = false;
            testedSlide = false;
            testedKick = false;

            UpdateBadgeChecks();
            ResetLayerColors();

            if (startLevelButton != null) startLevelButton.gameObject.SetActive(false);

            StartCoroutine(FadeRoutine(1f));
            GameLogger.Log("TUTORIAL", "Interactive Controls Overlay displayed.");
        }

        private void Update()
        {
            if (!isShowing) return;

            CheckLiveInputs();

            // ESC or B to skip
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                OnSkipClicked();
            }
            else if (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame)
            {
                if (AllActionsTested()) OnStartClicked();
                else OnSkipClicked();
            }
        }

        private void HandleControllerChanged(ControllerType newType)
        {
            SetDevice(newType);
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
            bool isGamepad = currentDevice != ControllerType.Keyboard;

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

            // 2. JUMP (Space or Button A / Cross)
            bool jumpPressed = false;
            if (Keyboard.current != null && (Keyboard.current.spaceKey.isPressed || Keyboard.current.wKey.isPressed))
                jumpPressed = true;
            if (Gamepad.current != null && (Gamepad.current.buttonSouth.isPressed || Gamepad.current.leftStick.y.ReadValue() > 0.5f))
                jumpPressed = true;

            if (jumpPressed)
            {
                if (!testedJump) { testedJump = true; TriggerActionFeedback("JUMP"); }
                HighlightLayer(controllerButtonAImage, true);
                HighlightLayer(keySpaceImage, true);
            }
            else
            {
                HighlightLayer(controllerButtonAImage, false, testedJump);
                HighlightLayer(keySpaceImage, false, testedJump);
            }

            // 3. SLIDE (S or Stick Down / DPad Down / Button B)
            bool slidePressed = false;
            if (Keyboard.current != null && (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed))
                slidePressed = true;
            if (Gamepad.current != null && (Gamepad.current.leftStick.y.ReadValue() < -0.4f || Gamepad.current.dpad.down.isPressed || Gamepad.current.buttonEast.isPressed))
                slidePressed = true;

            if (slidePressed)
            {
                if (!testedSlide) { testedSlide = true; TriggerActionFeedback("SLIDE"); }
                HighlightLayer(controllerButtonBImage, true);
                HighlightLayer(keySlideImage, true);
            }
            else
            {
                HighlightLayer(controllerButtonBImage, false, testedSlide);
                HighlightLayer(keySlideImage, false, testedSlide);
            }

            // 4. KICK (F or Button X / Square / Left Click)
            bool kickPressed = false;
            if (Keyboard.current != null && Keyboard.current.fKey.isPressed)
                kickPressed = true;
            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
                kickPressed = true;
            if (Gamepad.current != null && (Gamepad.current.buttonWest.isPressed || Gamepad.current.rightTrigger.isPressed))
                kickPressed = true;

            if (kickPressed)
            {
                if (!testedKick) { testedKick = true; TriggerActionFeedback("KICK"); }
                HighlightLayer(controllerButtonXImage, true);
                HighlightLayer(keyKickImage, true);
            }
            else
            {
                HighlightLayer(controllerButtonXImage, false, testedKick);
                HighlightLayer(keyKickImage, false, testedKick);
            }

            UpdateBadgeChecks();

            if (AllActionsTested() && startLevelButton != null && !startLevelButton.gameObject.activeSelf)
            {
                startLevelButton.gameObject.SetActive(true);
                StartCoroutine(PunchScale(startLevelButton.transform));
            }
        }

        private bool AllActionsTested()
        {
            return testedMove && testedJump && testedSlide && testedKick;
        }

        private void HighlightLayer(Image img, bool active, bool alreadyTested = false)
        {
            if (img == null) return;
            if (active) img.color = highlightColor;
            else if (alreadyTested) img.color = activeLayerColor;
            else img.color = normalLayerColor;
        }

        private void TriggerActionFeedback(string actionName)
        {
            GamepadVibrationManager.Instance?.TriggerLightVibration(0.08f);
            GameLogger.Log("TUTORIAL", $"Action validated: {actionName}");
        }

        private void UpdateBadgeChecks()
        {
            if (badgeMoveCheck != null) badgeMoveCheck.SetActive(testedMove);
            if (badgeJumpCheck != null) badgeJumpCheck.SetActive(testedJump);
            if (badgeSlideCheck != null) badgeSlideCheck.SetActive(testedSlide);
            if (badgeKickCheck != null) badgeKickCheck.SetActive(testedKick);
        }

        private void ResetLayerColors()
        {
            HighlightLayer(controllerStick1Image, false);
            HighlightLayer(controllerDpadImage, false);
            HighlightLayer(controllerButtonAImage, false);
            HighlightLayer(controllerButtonBImage, false);
            HighlightLayer(controllerButtonXImage, false);

            HighlightLayer(keyMoveLeftImage, false);
            HighlightLayer(keyMoveRightImage, false);
            HighlightLayer(keySlideImage, false);
            HighlightLayer(keyKickImage, false);
            HighlightLayer(keySpaceImage, false);
        }

        private IEnumerator PunchScale(Transform tr)
        {
            Vector3 original = tr.localScale;
            tr.localScale = original * 1.25f;
            float elapsed = 0f;
            while (elapsed < 0.2f)
            {
                elapsed += Time.unscaledDeltaTime;
                tr.localScale = Vector3.Lerp(original * 1.25f, original, elapsed / 0.2f);
                yield return null;
            }
            tr.localScale = original;
        }

        private void OnStartClicked()
        {
            CompleteTutorial();
        }

        private void OnSkipClicked()
        {
            CompleteTutorial();
        }

        private void CompleteTutorial()
        {
            if (!isShowing) return;
            isShowing = false;

            PlayerPrefs.SetInt(TUTORIAL_DONE_PREF, 1);
            PlayerPrefs.Save();

            if (AnalyticsManager.Instance != null)
            {
                AnalyticsManager.Instance.TrackEvent("tutorial_completed");
            }

            StartCoroutine(FadeRoutine(0f, () =>
            {
                gameObject.SetActive(false);
                onCompleteCallback?.Invoke();
            }));
        }

        private IEnumerator FadeRoutine(float targetAlpha, Action onDone = null)
        {
            if (mainCanvasGroup == null)
            {
                onDone?.Invoke();
                yield break;
            }

            mainCanvasGroup.blocksRaycasts = targetAlpha > 0.5f;
            mainCanvasGroup.interactable = targetAlpha > 0.5f;

            float startAlpha = mainCanvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                mainCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
                yield return null;
            }

            mainCanvasGroup.alpha = targetAlpha;
            onDone?.Invoke();
        }
    }
}

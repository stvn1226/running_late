using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI.GamePanels
{
    public class ControlsTutorialOverlay : MonoBehaviour
    {
        public static ControlsTutorialOverlay Instance { get; private set; }

        private const string PREF_KEY = "ControlsTutorial_Done";

        [Header("Testing & Debug")]
        [Tooltip("Force show tutorial even if already completed")]
        [SerializeField] private bool forceShowForTesting = false;

        [Header("UI Panels")]
        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button skipButton;
        [SerializeField] private Button startButton;
        [SerializeField] private TextMeshProUGUI titleText;

        [Header("Action Cards")]
        [SerializeField] private TutorialActionCard moveCard;
        [SerializeField] private TutorialActionCard jumpCard;
        [SerializeField] private TutorialActionCard slideCard;
        [SerializeField] private TutorialActionCard kickCard;

        private Action onCompleteCallback;
        private bool isTutorialActive = false;
        private bool moveTested = false;
        private bool jumpTested = false;
        private bool slideTested = false;
        private bool kickTested = false;

        [Serializable]
        public class TutorialActionCard
        {
            public GameObject cardRoot;
            public TextMeshProUGUI actionNameText;
            public TextMeshProUGUI keyPromptText;
            public Image checkmarkImage;
            public Image backgroundCard;
            public Color normalColor = new Color(1f, 1f, 1f, 0.85f);
            public Color testedColor = new Color(0.3f, 0.85f, 0.4f, 0.95f);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (overlayRoot != null)
            {
                overlayRoot.SetActive(false);
            }

            if (skipButton != null) skipButton.onClick.AddListener(OnSkipClicked);
            if (startButton != null) startButton.onClick.AddListener(OnStartClicked);
        }

        private void OnEnable()
        {
            if (ControllerTypeDetector.Instance != null)
            {
                ControllerTypeDetector.Instance.OnControllerChanged += OnDeviceChanged;
            }
        }

        private void OnDisable()
        {
            if (ControllerTypeDetector.Instance != null)
            {
                ControllerTypeDetector.Instance.OnControllerChanged -= OnDeviceChanged;
            }
        }

        public static bool ShouldShowTutorial()
        {
            if (Instance != null && Instance.forceShowForTesting) return true;
            return PlayerPrefs.GetInt(PREF_KEY, 0) == 0;
        }

        public void ShowTutorial(Action onComplete)
        {
            onCompleteCallback = onComplete;
            isTutorialActive = true;

            moveTested = false;
            jumpTested = false;
            slideTested = false;
            kickTested = false;

            if (overlayRoot != null) overlayRoot.SetActive(true);
            UpdateDevicePrompts(ControllerTypeDetector.Instance != null 
                ? ControllerTypeDetector.Instance.CurrentControllerType 
                : ControllerType.Keyboard);

            ResetCardState(moveCard);
            ResetCardState(jumpCard);
            ResetCardState(slideCard);
            ResetCardState(kickCard);

            if (startButton != null) startButton.gameObject.SetActive(false);

            StartCoroutine(FadeCanvasGroup(0f, 1f, 0.3f));
            GameLogger.Log("TUTORIAL", "Controls tutorial overlay opened");
        }

        private void Update()
        {
            if (!isTutorialActive) return;

            CheckInputs();
        }

        private void CheckInputs()
        {
            // Move: A/D, Left/Right Arrows, or Gamepad Left Stick / DPad
            if (!moveTested)
            {
                float h = Input.GetAxisRaw("Horizontal");
                if (Mathf.Abs(h) > 0.3f || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))
                {
                    moveTested = true;
                    MarkCardTested(moveCard);
                    CheckAllTested();
                }
            }

            // Jump: Space, W, UpArrow, or Gamepad Button 0 / South
            if (!jumpTested)
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetButtonDown("Jump"))
                {
                    jumpTested = true;
                    MarkCardTested(jumpCard);
                    CheckAllTested();
                }
            }

            // Slide: S, DownArrow, or Gamepad Down / Button
            if (!slideTested)
            {
                if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
                {
                    slideTested = true;
                    MarkCardTested(slideCard);
                    CheckAllTested();
                }
            }

            // Kick: F, LeftShift, or Gamepad Button West / X
            if (!kickTested)
            {
                if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.LeftShift) || Input.GetMouseButtonDown(0))
                {
                    kickTested = true;
                    MarkCardTested(kickCard);
                    CheckAllTested();
                }
            }
        }

        private void MarkCardTested(TutorialActionCard card)
        {
            if (card == null) return;
            if (card.checkmarkImage != null) card.checkmarkImage.gameObject.SetActive(true);
            if (card.backgroundCard != null) card.backgroundCard.color = card.testedColor;

            if (card.cardRoot != null)
            {
                StartCoroutine(PunchScaleCard(card.cardRoot.transform));
            }
        }

        private void ResetCardState(TutorialActionCard card)
        {
            if (card == null) return;
            if (card.checkmarkImage != null) card.checkmarkImage.gameObject.SetActive(false);
            if (card.backgroundCard != null) card.backgroundCard.color = card.normalColor;
        }

        private void CheckAllTested()
        {
            if (moveTested && jumpTested && slideTested && kickTested)
            {
                if (startButton != null)
                {
                    startButton.gameObject.SetActive(true);
                    StartCoroutine(PunchScaleCard(startButton.transform));
                }
            }
        }

        private void OnDeviceChanged(ControllerType type)
        {
            UpdateDevicePrompts(type);
        }

        private void UpdateDevicePrompts(ControllerType type)
        {
            bool isGamepad = type != ControllerType.Keyboard;

            if (moveCard != null && moveCard.keyPromptText != null)
                moveCard.keyPromptText.text = isGamepad ? "Left Stick / D-Pad" : "[A]  [D]";

            if (jumpCard != null && jumpCard.keyPromptText != null)
                jumpCard.keyPromptText.text = isGamepad ? "Button (A / Cross)" : "[SPACE]";

            if (slideCard != null && slideCard.keyPromptText != null)
                slideCard.keyPromptText.text = isGamepad ? "Stick Down / (B)" : "[S] / [DOWN]";

            if (kickCard != null && kickCard.keyPromptText != null)
                kickCard.keyPromptText.text = isGamepad ? "Button (X / Square)" : "[F] / [L-CLICK]";
        }

        private void OnSkipClicked()
        {
            CompleteTutorial(skipped: true);
        }

        private void OnStartClicked()
        {
            CompleteTutorial(skipped: false);
        }

        private void CompleteTutorial(bool skipped)
        {
            if (!isTutorialActive) return;
            isTutorialActive = false;

            PlayerPrefs.SetInt(PREF_KEY, 1);
            PlayerPrefs.Save();

            GameLogger.Log("TUTORIAL", $"Controls tutorial completed (Skipped: {skipped})");

            StartCoroutine(CloseAndProceed());
        }

        private IEnumerator CloseAndProceed()
        {
            yield return StartCoroutine(FadeCanvasGroup(1f, 0f, 0.25f));
            if (overlayRoot != null) overlayRoot.SetActive(false);

            Action callback = onCompleteCallback;
            onCompleteCallback = null;
            callback?.Invoke();
        }

        private IEnumerator FadeCanvasGroup(float from, float to, float duration)
        {
            if (canvasGroup == null) yield break;
            float elapsed = 0f;
            canvasGroup.alpha = from;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            canvasGroup.alpha = to;
        }

        private IEnumerator PunchScaleCard(Transform target)
        {
            if (target == null) yield break;
            Vector3 orig = Vector3.one;
            float elapsed = 0f;
            float dur = 0.2f;
            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                float s = 1f + 0.15f * Mathf.Sin((elapsed / dur) * Mathf.PI);
                target.localScale = orig * s;
                yield return null;
            }
            target.localScale = orig;
        }

        public static void ResetTutorial()
        {
            PlayerPrefs.DeleteKey(PREF_KEY);
            PlayerPrefs.Save();
            GameLogger.Log("TUTORIAL", "Controls tutorial reset in PlayerPrefs");
        }
    }
}

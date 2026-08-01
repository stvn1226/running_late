using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the ChallengeCardOverlay prefab — the layout rendered into the shareable PNG.
///
/// ── Prefab Hierarchy Expected ─────────────────────────────────────────────────
///
///  Card  (this component lives here)
///  ├── TopImg                    ← background panel sprite (assign if needed)
///  ├── LeftImage                 ← PHEW! burst zone
///  │   └── ExpressionText        ← "PHEW!" or equivalent exclamation
///  ├── RightImage                ← Best Time speech bubble zone
///  │   └── BestTimeContainer     ← hide this when no best time is available
///  │       ├── Title             ← static label "BEST TIME"
///  │       ├── BestTimeText      ← best time value
///  │       └── Image             ← bubble sprite (assign via Inspector)
///  └── BottomBG                  ← bottom info strip
///      └── Container
///          ├── FinishGameText    ← game title e.g. "RUNNING LATE"
///          ├── SubText           ← "CHALLENGE COMPLETE"
///          └── CenterContainer
///              ├── ChallengeCode
///              │   ├── FinishGameText   ← static label "CHALLENGE CODE"
///              │   └── GroupData
///              │       ├── Image            ← code icon sprite
///              │       └── CurrentTimeText  ← seed value (the Challenge Code)
///              ├── Route
///              │   ├── FinishGameText   ← static label "ROUTE"
///              │   └── GroupData
///              │       ├── Image            ← Single/Double Decker icon sprite
///              │       └── CurrentTimeText  ← "DOUBLE DECKER" / "SINGLE DECKER"
///              ├── Time
///              │   ├── FinishGameText   ← static label "TIME"
///              │   └── GroupData
///              │       ├── Image            ← clock icon sprite
///              │       └── CurrentTimeText  ← formatted completion time
///              └── ChallengeText        ← "CAN YOU BEAT MY TIME?"
///
/// ── Rules ─────────────────────────────────────────────────────────────────────
///  - No GameObject.Find, no transform.Find with strings, no Resources.Load.
///  - All references assigned via Inspector.
///  - This component populates data only. No buttons, toggles, or interactives.
/// </summary>
public class ChallengeCardOverlay : MonoBehaviour
{
    // ─── Left Zone (PHEW! burst) ──────────────────────────────────────────────

    [Header("Left Zone — Expression")]
    [Tooltip("The 'PHEW!' or exclamation text shown on the left burst.")]
    [SerializeField] private TMP_Text expressionText;

    // ─── Right Zone (Best Time bubble) ───────────────────────────────────────

    [Header("Right Zone — Best Time Bubble")]
    [Tooltip("Parent container of the Best Time bubble. Hidden when no best time is available.")]
    [SerializeField] private GameObject bestTimeContainer;

    [Tooltip("The best time value text (e.g. '00:58.12').")]
    [SerializeField] private TMP_Text bestTimeText;

    // ─── Bottom Strip — Header ────────────────────────────────────────────────

    [Header("Bottom Strip — Header")]
    [Tooltip("Game title text (e.g. 'RUNNING LATE'). Leave assigned and control via ChallengeConfig.")]
    [SerializeField] private TMP_Text finishGameText;

    [Tooltip("Subtitle (e.g. 'CHALLENGE COMPLETE').")]
    [SerializeField] private TMP_Text subText;

    // ─── Challenge Code Column ────────────────────────────────────────────────

    [Header("Challenge Code Column")]
    [Tooltip("The Challenge Code value — always equals seed.ToString(). Never transformed.")]
    [SerializeField] private TMP_Text challengeCodeValue;

    [Tooltip("Optional icon shown alongside the Challenge Code value.")]
    [SerializeField] private Image challengeCodeIcon;

    // ─── Route Column ─────────────────────────────────────────────────────────

    [Header("Route Column")]
    [Tooltip("Route value text: 'DOUBLE DECKER' or 'SINGLE DECKER'.")]
    [SerializeField] private TMP_Text routeValue;

    [Tooltip("Route icon — swap sprite at runtime based on pathMode.")]
    [SerializeField] private Image routeIcon;

    // ─── Time Column ──────────────────────────────────────────────────────────

    [Header("Time Column")]
    [Tooltip("Formatted completion time (e.g. '01:45.33').")]
    [SerializeField] private TMP_Text timeValue;

    [Tooltip("Optional icon shown alongside the time value.")]
    [SerializeField] private Image timeIcon;

    // ─── Challenge Message ────────────────────────────────────────────────────

    [Header("Challenge Message")]
    [Tooltip("Bottom message text (e.g. 'CAN YOU BEAT MY TIME?').")]
    [SerializeField] private TMP_Text challengeText;

    // ─── Config-driven sprites ────────────────────────────────────────────────

    [Header("Route Sprites (assigned from ChallengeConfig at runtime)")]
    [Tooltip("Sprite to display in routeIcon when path mode is DualPath (Double Decker).")]
    [SerializeField] private Sprite doubleDeckerSprite;

    [Tooltip("Sprite to display in routeIcon when path mode is SingleSidewalk (Single Decker).")]
    [SerializeField] private Sprite singleDeckerSprite;

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Populates all overlay elements from ChallengeData and ChallengeConfig.
    /// Call after instantiating the prefab, before triggering the camera render.
    ///
    /// Route sprites on this component take priority over those in ChallengeConfig
    /// so the prefab can be self-contained. If not assigned here, falls back to config.
    /// </summary>
    public void Populate(ChallengeData data, ChallengeConfig config)
    {
        SetExpression(data, config);
        SetBestTime(data);
        SetGameTitle(config);
        SetSubTitle(config);
        SetChallengeCode(data);
        SetRoute(data, config);
        SetTime(data);
        SetMessage(config);
    }

    // ─── Internal ─────────────────────────────────────────────────────────────

    private void SetExpression(ChallengeData data, ChallengeConfig config)
    {
        if (expressionText == null) return;

        // Use a phrase from config if available, otherwise a default exclamation.
        // The expression stays as a fun reaction — it is NOT the Challenge Code.
        if (config != null && !string.IsNullOrWhiteSpace(config.cardExpressionText))
            expressionText.text = config.cardExpressionText;
        // If not configured, leave whatever the prefab has set by default (e.g. "PHEW!")
    }

    private void SetBestTime(ChallengeData data)
    {
        // Show the bubble only if a best time exists and differs from current time
        bool hasBestTime = data.bestTime > 0f && !data.isFirstClear;

        if (bestTimeContainer != null)
            bestTimeContainer.SetActive(hasBestTime);

        if (hasBestTime && bestTimeText != null)
            bestTimeText.text = data.formattedBestTime;
    }

    private void SetGameTitle(ChallengeConfig config)
    {
        if (finishGameText == null || config == null) return;
        finishGameText.text = config.productName;
    }

    private void SetSubTitle(ChallengeConfig config)
    {
        if (subText == null || config == null) return;
        subText.text = config.challengeCompleteLabel;
    }

    private void SetChallengeCode(ChallengeData data)
    {
        // Challenge Code = seed.ToString() — no transformation, no encoding.
        if (challengeCodeValue != null)
            challengeCodeValue.text = data.challengeCode;
    }

    private void SetRoute(ChallengeData data, ChallengeConfig config)
    {
        if (routeValue != null)
            routeValue.text = data.routeName;

        if (routeIcon != null)
        {
            // Sprites on this component take priority (prefab is self-contained).
            // Fall back to ChallengeConfig sprites if not set locally.
            Sprite sprite = data.pathMode == LevelPathMode.DualPath
                ? (doubleDeckerSprite != null ? doubleDeckerSprite : config?.doubleDeckerSprite)
                : (singleDeckerSprite != null ? singleDeckerSprite : config?.singleDeckerSprite);

            if (sprite != null)
            {
                routeIcon.sprite = sprite;
                routeIcon.gameObject.SetActive(true);
            }
        }
    }

    private void SetTime(ChallengeData data)
    {
        if (timeValue != null)
            timeValue.text = data.formattedTime;
    }

    private void SetMessage(ChallengeConfig config)
    {
        if (challengeText == null || config == null) return;
        challengeText.text = config.challengeMessage;
    }
}

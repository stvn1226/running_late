using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Achievements
{
    /// <summary>
    /// Displays a popup banner notification in UI whenever an achievement is unlocked.
    /// Works seamlessly on Desktop and Mobile.
    /// </summary>
    public class AchievementNotificationUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup bannerCanvasGroup;
        [SerializeField] private RectTransform bannerRect;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descText;

        [Header("Animation Settings")]
        [SerializeField] private float slideDuration = 0.4f;
        [SerializeField] private float displayDuration = 3.5f;

        private Coroutine activeNotificationCoroutine;

        private void Awake()
        {
            if (bannerCanvasGroup != null)
            {
                bannerCanvasGroup.alpha = 0f;
                bannerCanvasGroup.blocksRaycasts = false;
            }
        }

        private void OnEnable()
        {
            AchievementsManager.OnAchievementUnlocked += ShowAchievementBanner;
        }

        private void OnDisable()
        {
            AchievementsManager.OnAchievementUnlocked -= ShowAchievementBanner;
        }

        private void ShowAchievementBanner(string id, string title, string desc)
        {
            if (bannerCanvasGroup == null || bannerRect == null) return;

            if (titleText != null) titleText.text = string.IsNullOrEmpty(title) ? "¡Logro Desbloqueado!" : title;
            if (descText != null) descText.text = desc;

            if (activeNotificationCoroutine != null)
            {
                StopCoroutine(activeNotificationCoroutine);
            }
            activeNotificationCoroutine = StartCoroutine(AnimateBannerRoutine());
        }

        private IEnumerator AnimateBannerRoutine()
        {
            bannerCanvasGroup.blocksRaycasts = false;
            bannerCanvasGroup.alpha = 1f;

            Vector2 hiddenPos = new Vector2(0f, 150f);
            Vector2 visiblePos = new Vector2(0f, -40f);

            bannerRect.anchoredPosition = hiddenPos;

            // Slide in (EaseOutBack)
            float elapsed = 0f;
            while (elapsed < slideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / slideDuration);
                // Ease out cubic
                float ease = 1f - Mathf.Pow(1f - t, 3f);
                bannerRect.anchoredPosition = Vector2.Lerp(hiddenPos, visiblePos, ease);
                yield return null;
            }
            bannerRect.anchoredPosition = visiblePos;

            // Display time
            yield return new WaitForSecondsRealtime(displayDuration);

            // Fade out
            elapsed = 0f;
            float fadeDuration = 0.5f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                bannerCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                yield return null;
            }
            bannerCanvasGroup.alpha = 0f;
            activeNotificationCoroutine = null;
        }
    }
}

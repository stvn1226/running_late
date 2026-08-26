using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UI.GamePanels
{
    public enum UIPlayerState
    {
        Idle,
        Sprint,
        Jump,
        Slide,
        Kick
    }

    /// <summary>
    /// UI-based animated Player character preview for the Controls Tutorial Overlay.
    /// Operates completely independently of Time.timeScale (uses unscaled time).
    /// Reacts to live player inputs to show on-screen visual demonstrations of moves.
    /// </summary>
    public class UIPlayerPreview : MonoBehaviour
    {
        [Header("Target UI Image & Platform")]
        [SerializeField] private Image playerImage;
        [SerializeField] private RectTransform characterTransform;
        [SerializeField] private Image shadowImage;

        [Header("Animation Frame Rates (FPS)")]
        [SerializeField] private float sprintFPS = 12f;
        [SerializeField] private float kickFPS = 14f;
        [SerializeField] private float idleFPS = 6f;

        [Header("Sprite Sequences")]
        [SerializeField] private Sprite[] idleSprites;
        [SerializeField] private Sprite[] sprintSprites;
        [SerializeField] private Sprite[] jumpRiseSprites;
        [SerializeField] private Sprite[] jumpApexSprites;
        [SerializeField] private Sprite[] jumpFallSprites;
        [SerializeField] private Sprite[] slideSprites;
        [SerializeField] private Sprite[] kickSprites;

        private UIPlayerState currentState = UIPlayerState.Idle;
        private Coroutine activeActionRoutine;
        private Vector2 baseAnchoredPosition;
        private Vector3 baseScale;

        private void Awake()
        {
            if (characterTransform == null && playerImage != null)
            {
                characterTransform = playerImage.rectTransform;
            }
            if (characterTransform != null)
            {
                baseAnchoredPosition = characterTransform.anchoredPosition;
                baseScale = characterTransform.localScale;
            }
        }

        private void OnEnable()
        {
            PlayIdleOrSprint(false);
        }

        private void OnDisable()
        {
            if (activeActionRoutine != null)
            {
                StopCoroutine(activeActionRoutine);
                activeActionRoutine = null;
            }
            ResetTransform();
        }

        public void PlayMove(bool isMoving)
        {
            if (currentState == UIPlayerState.Jump || currentState == UIPlayerState.Slide || currentState == UIPlayerState.Kick)
            {
                return; // Let current active action finish
            }

            PlayIdleOrSprint(isMoving);
        }

        public void TriggerJump()
        {
            if (activeActionRoutine != null) StopCoroutine(activeActionRoutine);
            activeActionRoutine = StartCoroutine(JumpRoutine());
        }

        public void TriggerSlide()
        {
            if (activeActionRoutine != null) StopCoroutine(activeActionRoutine);
            activeActionRoutine = StartCoroutine(SlideRoutine());
        }

        public void TriggerKick()
        {
            if (activeActionRoutine != null) StopCoroutine(activeActionRoutine);
            activeActionRoutine = StartCoroutine(KickRoutine());
        }

        private void PlayIdleOrSprint(bool isSprinting)
        {
            if (activeActionRoutine != null)
            {
                StopCoroutine(activeActionRoutine);
                activeActionRoutine = null;
            }

            ResetTransform();

            if (isSprinting && sprintSprites != null && sprintSprites.Length > 0)
            {
                currentState = UIPlayerState.Sprint;
                activeActionRoutine = StartCoroutine(LoopAnimationRoutine(sprintSprites, sprintFPS));
            }
            else
            {
                currentState = UIPlayerState.Idle;
                if (idleSprites != null && idleSprites.Length > 0)
                {
                    activeActionRoutine = StartCoroutine(LoopAnimationRoutine(idleSprites, idleFPS));
                }
                else if (sprintSprites != null && sprintSprites.Length > 0)
                {
                    SetSprite(sprintSprites[0]);
                }
            }
        }

        private IEnumerator LoopAnimationRoutine(Sprite[] frames, float fps)
        {
            if (frames == null || frames.Length == 0) yield break;

            int index = 0;
            float frameDuration = 1f / Mathf.Max(fps, 1f);

            while (true)
            {
                SetSprite(frames[index]);
                index = (index + 1) % frames.Length;
                yield return new WaitForSecondsRealtime(frameDuration);
            }
        }

        private IEnumerator JumpRoutine()
        {
            currentState = UIPlayerState.Jump;

            // 1. Rise phase
            if (jumpRiseSprites != null && jumpRiseSprites.Length > 0) SetSprite(jumpRiseSprites[0]);
            float jumpHeight = 70f;
            float jumpDuration = 0.5f;
            float elapsed = 0f;

            while (elapsed < jumpDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / jumpDuration;

                // Parabolic arc: 4 * t * (1 - t)
                float currentY = baseAnchoredPosition.y + Mathf.Sin(t * Mathf.PI) * jumpHeight;
                characterTransform.anchoredPosition = new Vector2(baseAnchoredPosition.x, currentY);

                if (t > 0.4f && t < 0.7f && jumpApexSprites != null && jumpApexSprites.Length > 0)
                {
                    SetSprite(jumpApexSprites[0]);
                }
                else if (t >= 0.7f && jumpFallSprites != null && jumpFallSprites.Length > 0)
                {
                    SetSprite(jumpFallSprites[0]);
                }

                if (shadowImage != null)
                {
                    float shadowScale = Mathf.Lerp(1f, 0.6f, Mathf.Sin(t * Mathf.PI));
                    shadowImage.transform.localScale = new Vector3(shadowScale, shadowScale, 1f);
                }

                yield return null;
            }

            ResetTransform();
            PlayIdleOrSprint(false);
        }

        private IEnumerator SlideRoutine()
        {
            currentState = UIPlayerState.Slide;

            if (slideSprites != null && slideSprites.Length > 0) SetSprite(slideSprites[0]);

            float slideDuration = 0.45f;
            float elapsed = 0f;
            Vector2 slideOffset = new Vector2(25f, -12f);

            while (elapsed < slideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / slideDuration;

                float offsetX = Mathf.Sin(t * Mathf.PI) * slideOffset.x;
                float offsetY = Mathf.Sin(t * Mathf.PI) * slideOffset.y;
                characterTransform.anchoredPosition = baseAnchoredPosition + new Vector2(offsetX, offsetY);

                yield return null;
            }

            ResetTransform();
            PlayIdleOrSprint(false);
        }

        private IEnumerator KickRoutine()
        {
            currentState = UIPlayerState.Kick;

            if (kickSprites != null && kickSprites.Length > 0)
            {
                float frameDuration = 1f / Mathf.Max(kickFPS, 1f);
                for (int i = 0; i < kickSprites.Length; i++)
                {
                    SetSprite(kickSprites[i]);
                    // Punch forward on impact frame
                    if (i == 1 && characterTransform != null)
                    {
                        characterTransform.anchoredPosition = baseAnchoredPosition + new Vector2(18f, 0f);
                    }
                    yield return new WaitForSecondsRealtime(frameDuration);
                }
            }
            else
            {
                yield return new WaitForSecondsRealtime(0.3f);
            }

            ResetTransform();
            PlayIdleOrSprint(false);
        }

        private void SetSprite(Sprite s)
        {
            if (playerImage != null && s != null)
            {
                playerImage.sprite = s;
                playerImage.preserveAspect = true;
            }
        }

        private void ResetTransform()
        {
            if (characterTransform != null)
            {
                characterTransform.anchoredPosition = baseAnchoredPosition;
                characterTransform.localScale = baseScale;
            }
            if (shadowImage != null)
            {
                shadowImage.transform.localScale = Vector3.one;
            }
        }
    }
}

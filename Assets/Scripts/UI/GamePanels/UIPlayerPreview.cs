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
        Kick,
        WallRun,
        AirDodge,
        Dive,
        DropDown
    }

    /// <summary>
    /// UI-based animated Player character preview for the Controls Tutorial Overlay.
    /// Operates completely independently of Time.timeScale (uses unscaled time).
    /// Reacts to live player inputs to show on-screen visual demonstrations of all game mechanics.
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
        [SerializeField] private float rollFPS = 14f;
        [SerializeField] private float wallRunFPS = 12f;
        [SerializeField] private float idleFPS = 6f;

        [Header("Sprite Sequences")]
        [SerializeField] private Sprite[] idleSprites;
        [SerializeField] private Sprite[] sprintSprites;
        [SerializeField] private Sprite[] jumpRiseSprites;
        [SerializeField] private Sprite[] jumpApexSprites;
        [SerializeField] private Sprite[] jumpFallSprites;
        [SerializeField] private Sprite[] slideSprites;
        [SerializeField] private Sprite[] kickSprites;
        [SerializeField] private Sprite[] wallRunSprites;
        [SerializeField] private Sprite[] rollSprites;
        [SerializeField] private Sprite[] diveSprites;

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

        public void SetMoving(bool isMoving)
        {
            PlayMove(isMoving);
        }

        public void PlayMove(bool isMoving)
        {
            if (currentState == UIPlayerState.Jump || 
                currentState == UIPlayerState.Slide || 
                currentState == UIPlayerState.Kick ||
                currentState == UIPlayerState.WallRun ||
                currentState == UIPlayerState.AirDodge ||
                currentState == UIPlayerState.Dive)
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

        public void TriggerWallRun()
        {
            if (activeActionRoutine != null) StopCoroutine(activeActionRoutine);
            activeActionRoutine = StartCoroutine(WallRunRoutine());
        }

        public void TriggerAirDodge()
        {
            if (activeActionRoutine != null) StopCoroutine(activeActionRoutine);
            activeActionRoutine = StartCoroutine(AirDodgeRoutine());
        }

        public void TriggerDive()
        {
            if (activeActionRoutine != null) StopCoroutine(activeActionRoutine);
            activeActionRoutine = StartCoroutine(DiveRoutine());
        }

        public void TriggerDropDown()
        {
            if (activeActionRoutine != null) StopCoroutine(activeActionRoutine);
            activeActionRoutine = StartCoroutine(DropDownRoutine());
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

            if (jumpRiseSprites != null && jumpRiseSprites.Length > 0) SetSprite(jumpRiseSprites[0]);
            float jumpHeight = 70f;
            float jumpDuration = 0.5f;
            float elapsed = 0f;

            while (elapsed < jumpDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / jumpDuration;

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

        private IEnumerator WallRunRoutine()
        {
            currentState = UIPlayerState.WallRun;

            if (wallRunSprites != null && wallRunSprites.Length > 0)
            {
                float frameDuration = 1f / Mathf.Max(wallRunFPS, 1f);
                for (int i = 0; i < wallRunSprites.Length; i++)
                {
                    SetSprite(wallRunSprites[i]);
                    if (characterTransform != null)
                    {
                        characterTransform.anchoredPosition = baseAnchoredPosition + new Vector2(10f, 15f + i * 4f);
                    }
                    yield return new WaitForSecondsRealtime(frameDuration);
                }
            }
            else
            {
                yield return new WaitForSecondsRealtime(0.4f);
            }

            ResetTransform();
            PlayIdleOrSprint(false);
        }

        private IEnumerator AirDodgeRoutine()
        {
            currentState = UIPlayerState.AirDodge;

            if (rollSprites != null && rollSprites.Length > 0)
            {
                float frameDuration = 1f / Mathf.Max(rollFPS, 1f);
                for (int i = 0; i < rollSprites.Length; i++)
                {
                    SetSprite(rollSprites[i]);
                    if (characterTransform != null)
                    {
                        characterTransform.anchoredPosition = baseAnchoredPosition + new Vector2(30f * ((float)i / rollSprites.Length), 20f);
                    }
                    yield return new WaitForSecondsRealtime(frameDuration);
                }
            }
            else
            {
                yield return new WaitForSecondsRealtime(0.35f);
            }

            ResetTransform();
            PlayIdleOrSprint(false);
        }

        private IEnumerator DiveRoutine()
        {
            currentState = UIPlayerState.Dive;

            if (diveSprites != null && diveSprites.Length > 0) SetSprite(diveSprites[0]);

            float diveDuration = 0.35f;
            float elapsed = 0f;

            // Start up high then slam down
            while (elapsed < diveDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / diveDuration;
                float currentY = baseAnchoredPosition.y + (1f - t) * 50f;
                characterTransform.anchoredPosition = new Vector2(baseAnchoredPosition.x + t * 15f, currentY);
                yield return null;
            }

            ResetTransform();
            PlayIdleOrSprint(false);
        }

        private IEnumerator DropDownRoutine()
        {
            currentState = UIPlayerState.DropDown;

            if (jumpFallSprites != null && jumpFallSprites.Length > 0) SetSprite(jumpFallSprites[0]);

            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float currentY = baseAnchoredPosition.y - t * 25f;
                characterTransform.anchoredPosition = new Vector2(baseAnchoredPosition.x, currentY);
                yield return null;
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

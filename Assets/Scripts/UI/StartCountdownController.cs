using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class StartCountdownController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject countdownPanel;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private Image countdownBackground;
    [SerializeField] private Image countdownCircle;
    
    [Header("Countdown Settings")]
    [SerializeField] private int countdownFrom = 3;
    [SerializeField] private float countdownInterval = 1f;
    [SerializeField] private string goText = "GO!";
    [SerializeField] private float goDisplayDuration = 0.5f;
    [SerializeField] private float delayBeforeFreeze = 0.5f;
    
    [Header("Animation Settings")]
    [SerializeField] private bool useScaleAnimation = true;
    [SerializeField] private float scaleAnimationDuration = 0.3f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Audio")]
    [SerializeField] private bool playCountdownSounds = true;
    
    private bool isCountingDown = false;
    
    public static StartCountdownController Instance { get; private set; }
    
    public System.Action OnCountdownComplete;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        if (countdownPanel != null)
        {
            countdownPanel.SetActive(false);
        }
    }
    
    private void Start()
    {
        if (countdownText == null)
        {
            GameLogger.LogWarning("COUNTDOWN", "CountdownText is not assigned!");
        }
        
        if (countdownPanel != null)
        {
            countdownPanel.SetActive(false);
        }
    }
    
    public void StartCountdown()
    {
        if (isCountingDown)
        {
            GameLogger.LogWarning("COUNTDOWN", "Countdown already in progress!");
            return;
        }
        
        StartCoroutine(CountdownSequence());
    }
    
    private IEnumerator CountdownSequence()
    {
        isCountingDown = true;
    
        GameLogger.Log("COUNTDOWN", $"Waiting {delayBeforeFreeze}s for level generation...");
        yield return StartCoroutine(WaitForRealtimeRespectingPause(delayBeforeFreeze));
    
        GameLogger.Log("COUNTDOWN", "Game paused for countdown");
    
        if (countdownPanel != null)
        {
            countdownPanel.SetActive(true);
        }
    
        GameLogger.Log("COUNTDOWN", "Starting countdown sequence");
    
        if (playCountdownSounds)
        {
            AudioManager.Instance?.Play(AudioManager.SoundType.CDNumbers);
        }

        for (int i = countdownFrom; i > 0; i--)
        {
            DisplayNumber(i);
        

        
            if (useScaleAnimation)
            {
                yield return StartCoroutine(AnimateCountdownNumber());
            }
            else
            {
                yield return StartCoroutine(WaitForRealtimeRespectingPause(countdownInterval));
            }
        }
    
        DisplayGo();
        
        Time.timeScale = 1f;
        GameLogger.Log("COUNTDOWN", "GO! - Game resumed and player launched!");
        OnCountdownComplete?.Invoke();

        if (useScaleAnimation)
        {
            yield return StartCoroutine(AnimateCountdownNumber());
        }
        else
        {
            yield return new WaitForSeconds(goDisplayDuration);
        }
    
        if (countdownPanel != null)
        {
            countdownPanel.SetActive(false);
        }

        yield return new WaitForSeconds(1.1f); // Small delay to ensure "GO!" sound plays
        AudioManager.Instance?.Play(AudioManager.SoundType.MusicLevel);
        AudioManager.Instance?.Play(AudioManager.SoundType.MusicInvincibility);

        isCountingDown = false;
    
        GameLogger.Log("COUNTDOWN", "Countdown UI hidden");
    }
    
    private void DisplayNumber(int number)
    {
        if (countdownText != null)
        {
            countdownText.text = number.ToString();
            GameLogger.LogVerbose("COUNTDOWN", $"Displaying: {number}");
        }
    }
    
    private void DisplayGo()
    {
        if (countdownText != null)
        {
            countdownText.text = goText;
            GameLogger.LogVerbose("COUNTDOWN", $"Displaying: {goText}");
        }
    }
    
    private IEnumerator AnimateCountdownNumber()
    {
        float elapsed = 0f;
        Vector3 originalScale = countdownText != null ? countdownText.transform.localScale : Vector3.one;
    
        while (elapsed < scaleAnimationDuration)
        {
            if (!IsPaused())
            {
                elapsed += Time.unscaledDeltaTime;
            }
        
            float normalizedTime = elapsed / scaleAnimationDuration;
            float scaleValue = scaleCurve.Evaluate(normalizedTime);
        
            if (countdownText != null)
            {
                countdownText.transform.localScale = originalScale * (1f + scaleValue * 0.5f);
            }
        
            yield return null;
        }
    
        if (countdownText != null)
        {
            countdownText.transform.localScale = originalScale;
        }
    
        yield return StartCoroutine(WaitForRealtimeRespectingPause(countdownInterval - scaleAnimationDuration));
    }
    
    public bool IsCountingDown => isCountingDown;
    
    
    private IEnumerator WaitForRealtimeRespectingPause(float duration)
    {
        float elapsed = 0f;
    
        while (elapsed < duration)
        {
            if (!IsPaused())
            {
                elapsed += Time.unscaledDeltaTime;
            }
            yield return null;
        }
    }

    private bool IsPaused()
    {
        return UIManager.Instance != null && UIManager.Instance.IsPaused;
    }

}

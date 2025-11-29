using UnityEngine;
using TMPro;

public class Timer : MonoBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private bool countUp = true;
    [SerializeField] private string timeFormat = "mm:ss.ff";
    [SerializeField] private bool waitForStartCountdown = true;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    private TextMeshProUGUI timerText;
    private float elapsedTime = 0f;
    private bool isRunning = false;
    private bool isPermanentlyStopped = false;
    private bool hasStarted = false;
    
    public float ElapsedTime => elapsedTime;
    public bool IsRunning => isRunning;
    
    void Start()
    {
        timerText = GetComponent<TextMeshProUGUI>();
        if (timerText == null)
        {
            GameLogger.LogError("TIMER", "No TextMeshProUGUI component found!");
            return;
        }
        
        ResetTimer();
        
        if (waitForStartCountdown && StartCountdownController.Instance != null)
        {
            isRunning = false;
            StartCountdownController.Instance.OnCountdownComplete += OnStartCountdownComplete;
            
            if (showDebugInfo)
                GameLogger.Log("TIMER", "Waiting for start countdown to complete");
        }
        else
        {
            isRunning = true;
            hasStarted = true;
            
            if (showDebugInfo)
                GameLogger.Log("TIMER", "Initialized and started counting");
        }
    }
    
    void OnDestroy()
    {
        if (StartCountdownController.Instance != null)
        {
            StartCountdownController.Instance.OnCountdownComplete -= OnStartCountdownComplete;
        }
    }
    
    private void OnStartCountdownComplete()
    {
        isRunning = true;
        hasStarted = true;
        
        if (showDebugInfo)
            GameLogger.Log("TIMER", "Start countdown complete - timer started");
        
        if (StartCountdownController.Instance != null)
        {
            StartCountdownController.Instance.OnCountdownComplete -= OnStartCountdownComplete;
        }
    }
    
    void Update()
    {
        if (isRunning && !isPermanentlyStopped && Time.timeScale > 0)
        {
            if (countUp)
            {
                elapsedTime += Time.deltaTime;
            }
            else
            {
                elapsedTime -= Time.deltaTime;
                if (elapsedTime <= 0)
                {
                    elapsedTime = 0;
                    StopTimerPermanently();
                }
            }
        }
        
        UpdateDisplay();
    }
    
    public void StartTimer()
    {
        if (!isPermanentlyStopped)
        {
            isRunning = true;
            hasStarted = true;
            
            if (showDebugInfo)
                GameLogger.Log("TIMER", "Started");
        }
    }
    
    public void StopTimer()
    {
        isRunning = false;
        
        if (showDebugInfo)
            GameLogger.Log("TIMER", $"Stopped at {FormatTime(elapsedTime)}");
    }
    
    public void StopTimerPermanently()
    {
        isRunning = false;
        isPermanentlyStopped = true;
        
        if (showDebugInfo)
            GameLogger.Log("TIMER", $"Permanently stopped at {FormatTime(elapsedTime)}");
    }
    
    public void PauseTimer()
    {
        isRunning = false;
        
        if (showDebugInfo)
            GameLogger.Log("TIMER", "Paused");
    }
    
    public void ResumeTimer()
    {
        if (!isPermanentlyStopped)
        {
            isRunning = true;
            
            if (showDebugInfo)
                GameLogger.Log("TIMER", "Resumed");
        }
    }
    
    public void ResetTimer()
    {
        elapsedTime = 0f;
        isRunning = waitForStartCountdown ? false : true;
        isPermanentlyStopped = false;
        hasStarted = false;
        UpdateDisplay();
        
        if (showDebugInfo)
            GameLogger.Log("TIMER", "Reset");
    }
    
    private void UpdateDisplay()
    {
        if (timerText != null)
        {
            timerText.text = FormatTime(elapsedTime);
        }
    }
    
    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        int centiseconds = Mathf.FloorToInt((time * 100f) % 100f);
        
        switch (timeFormat.ToLower())
        {
            case "mm:ss.ff":
                return $"{minutes:00}:{seconds:00}.{centiseconds:00}";
            case "mm:ss":
                return $"{minutes:00}:{seconds:00}";
            case "ss.ff":
                return $"{seconds:00}.{centiseconds:00}";
            case "ss":
                return $"{seconds:00}";
            default:
                return $"{minutes:00}:{seconds:00}.{centiseconds:00}";
        }
    }
    
    public string GetFormattedTime()
    {
        return FormatTime(elapsedTime);
    }
}

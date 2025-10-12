using UnityEngine;
using TMPro;

public class Timer : MonoBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private bool countUp = true;
    [SerializeField] private string timeFormat = "mm:ss.ff";
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    private TextMeshProUGUI timerText;
    private float elapsedTime = 0f;
    private bool isRunning = true;
    private bool isGameCompleted = false;
    
    public float ElapsedTime => elapsedTime;
    public bool IsRunning => isRunning;
    
    void Start()
    {
        // Get the TextMeshPro component
        timerText = GetComponent<TextMeshProUGUI>();
        if (timerText == null)
        {
            GameLogger.LogError("TIMER", "No TextMeshProUGUI component found! Please add one to display the timer.");
            return;
        }
        
        // Subscribe to goal reached event
        Goal goalScript = FindObjectOfType<Goal>();
        if (goalScript != null)
        {
            // We'll use a different approach since Goal doesn't have events
            // The timer will check for game pause state instead
        }
        
        // Initialize timer
        ResetTimer();
        
        if (showDebugInfo)
            GameLogger.Log("TIMER", "Initialized and started counting");
    }
    
    void Update()
    {
        // Only count if the game is running and not completed
        if (isRunning && !isGameCompleted)
        {
            // Check if game is paused (Time.timeScale == 0 means victory screen is showing)
            if (Time.timeScale > 0)
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
                        StopTimer();
                    }
                }
            }
            else
            {
                // Game is paused (likely victory screen), stop the timer
                if (!isGameCompleted)
                {
                    StopTimer();
                    isGameCompleted = true;
                    
                    if (showDebugInfo)
                        GameLogger.Log("TIMER", $"Stopped at {FormatTime(elapsedTime)} due to game completion");
                }
            }
        }
        
        // Update the display
        UpdateDisplay();
    }
    
    public void StartTimer()
    {
        isRunning = true;
        
        if (showDebugInfo)
            GameLogger.Log("TIMER", "Started");
    }
    
    public void StopTimer()
    {
        isRunning = false;
        
        if (showDebugInfo)
            GameLogger.Log("TIMER", $"Stopped at {FormatTime(elapsedTime)}");
    }
    
    public void PauseTimer()
    {
        isRunning = false;
        
        if (showDebugInfo)
            GameLogger.Log("TIMER", "Paused");
    }
    
    public void ResumeTimer()
    {
        if (!isGameCompleted)
        {
            isRunning = true;
            
            if (showDebugInfo)
                GameLogger.Log("TIMER", "Resumed");
        }
    }
    
    public void ResetTimer()
    {
        elapsedTime = 0f;
        isRunning = true;
        isGameCompleted = false;
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
    
    // Public method to get formatted time string
    public string GetFormattedTime()
    {
        return FormatTime(elapsedTime);
    }
}

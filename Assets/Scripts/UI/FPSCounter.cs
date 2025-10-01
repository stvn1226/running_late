using UnityEngine;
using TMPro;

public class FPSCounter : MonoBehaviour
{
    [Header("FPS Display Settings")]
    [SerializeField] private bool showFPS = true;
    [SerializeField] private float updateInterval = 0.5f;
    
    [Header("Display Format")]
    [SerializeField] private string fpsFormat = "FPS: {0:F1}";
    [SerializeField] private bool showAdditionalInfo = true;
    
    [Header("Color Coding")]
    [SerializeField] private bool useColorCoding = true;
    [SerializeField] private float goodFPSThreshold = 55f;
    [SerializeField] private float okFPSThreshold = 25f;
    [SerializeField] private Color goodFPSColor = Color.green;
    [SerializeField] private Color okFPSColor = Color.yellow;
    [SerializeField] private Color badFPSColor = Color.red;
    
    [SerializeField] private TextMeshProUGUI fpsText;
    private float timer;
    private int frameCount;
    private float lastInterval;
    
    private float currentFPS;
    private float averageFPS;
    private float minFPS = float.MaxValue;
    private float maxFPS = 0f;
    private int totalFrames;
    
    public float CurrentFPS => currentFPS;
    public float AverageFPS => averageFPS;
    public float MinFPS => minFPS;
    public float MaxFPS => maxFPS;
    
    void Start()
    {
        if (fpsText == null)
        {
            Debug.LogError("FPSCounter: No TextMeshProUGUI component found!");
            enabled = false;
            return;
        }
        
        lastInterval = Time.realtimeSinceStartup;
        ResetStats();
    }
    
    void Update()
    {
        if (!showFPS) return;
        
        timer += Time.unscaledDeltaTime;
        frameCount++;
        totalFrames++;
        
        if (timer >= updateInterval)
        {
            currentFPS = frameCount / timer;
            
            if (currentFPS < minFPS) minFPS = currentFPS;
            if (currentFPS > maxFPS) maxFPS = currentFPS;
            
            float currentTime = Time.realtimeSinceStartup;
            averageFPS = totalFrames / (currentTime - lastInterval + updateInterval);
            
            UpdateFPSDisplay();
            
            timer = 0f;
            frameCount = 0;
        }
    }
    
    private void UpdateFPSDisplay()
    {
        if (fpsText == null) return;
        
        string displayText = string.Format(fpsFormat, currentFPS);
        
        if (showAdditionalInfo)
        {
            displayText += $"\nAvg: {averageFPS:F1}";
            displayText += $"\nMin: {minFPS:F1} | Max: {maxFPS:F1}";
            displayText += $"\nTarget: {Application.targetFrameRate}";
            displayText += $"\nVSync: {(QualitySettings.vSyncCount > 0 ? "ON" : "OFF")}";
        }
        
        fpsText.text = displayText;
        
        // Apply color coding
        if (useColorCoding)
        {
            if (currentFPS >= goodFPSThreshold)
                fpsText.color = goodFPSColor;
            else if (currentFPS >= okFPSThreshold)
                fpsText.color = okFPSColor;
            else
                fpsText.color = badFPSColor;
        }
    }
    
    public void ResetStats()
    {
        minFPS = float.MaxValue;
        maxFPS = 0f;
        totalFrames = 0;
        lastInterval = Time.realtimeSinceStartup;
        timer = 0f;
        frameCount = 0;
    }
    
    public void ToggleDisplay()
    {
        showFPS = !showFPS;
        gameObject.SetActive(showFPS);
    }
    
    public string GetDetailedStats()
    {
        return $"FPS Stats:\n" +
               $"Current: {currentFPS:F1}\n" +
               $"Average: {averageFPS:F1}\n" +
               $"Min: {minFPS:F1}\n" +
               $"Max: {maxFPS:F1}\n" +
               $"Target: {Application.targetFrameRate}\n" +
               $"VSync: {(QualitySettings.vSyncCount > 0 ? "ON" : "OFF")}\n" +
               $"Total Frames: {totalFrames}";
    }
    
    // Context menu for testing
    [ContextMenu("Reset FPS Stats")]
    private void ResetStatsFromMenu()
    {
        ResetStats();
        Debug.Log("FPS stats reset");
    }
    
    [ContextMenu("Log Detailed Stats")]
    private void LogDetailedStats()
    {
        Debug.Log(GetDetailedStats());
    }
}

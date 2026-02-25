using UnityEngine;
using System.Collections;

public class TimeScaleManager : MonoBehaviour
{
    [Header("Time Scale Settings")]
    [SerializeField] private float[] timeScaleValues = { 0f, 0.25f, 0.5f, 1f, 2f, 5f, 10f };
    [SerializeField] private int currentScaleIndex = 3;
    [SerializeField] private bool showDebugInfo = true;
    
    private bool isGamePaused = false;
    
    public static TimeScaleManager Instance { get; private set; }
    public System.Action<float> OnTimeScaleChanged;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        currentScaleIndex = 3; // 1x speed
    }
    
    private void Start()
    {
        ApplyTimeScale(timeScaleValues[currentScaleIndex]);
    }
    
    public void CycleTimeScaleUp()
    {
        if (isGamePaused) return;
        
        currentScaleIndex = (currentScaleIndex + 1) % timeScaleValues.Length;
        ApplyTimeScale(timeScaleValues[currentScaleIndex]);
    }
    
    public void CycleTimeScaleDown()
    {
        if (isGamePaused) return;
        
        currentScaleIndex = (currentScaleIndex - 1 + timeScaleValues.Length) % timeScaleValues.Length;
        ApplyTimeScale(timeScaleValues[currentScaleIndex]);
    }
    
    public void ToggleResetPause()
    {
        if (isGamePaused) return;
        
        float currentScale = timeScaleValues[currentScaleIndex];
        
        if (currentScale == 1f)
        {
            // Si está en 1x, cambiar a 0x (pause)
            currentScaleIndex = 0;
        }
        else
        {
            // Si está en cualquier otra velocidad, cambiar a 1x (reset)
            currentScaleIndex = 3;
        }
        
        ApplyTimeScale(timeScaleValues[currentScaleIndex]);
    }
    
    private void ApplyTimeScale(float scale)
    {
        Time.timeScale = scale;
        
        if (showDebugInfo)
        {
            string scaleText = scale == 0f ? "PAUSED" : $"{scale}x";
            GameLogger.Log("TIMESCALE", $"Time scale: {scaleText}");
        }
        
        OnTimeScaleChanged?.Invoke(scale);
    }
    
    public void OnGamePaused(bool paused)
    {
        isGamePaused = paused;
        if (paused)
        {
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = timeScaleValues[currentScaleIndex];
        }
    }
    
    public float GetCurrentTimeScale() => timeScaleValues[currentScaleIndex];
    public int GetCurrentIndex() => currentScaleIndex;
    public bool IsGamePaused() => isGamePaused;

    public float[] GetAllTimeScales() => timeScaleValues;
    
    public void SetTimeScaleByIndex(int index)=>currentScaleIndex = index;

    private Coroutine slowMoCoroutine;
    
    /// <summary>
    /// Requests a temporary slow-mo effect. Centralized to ensure TimeScale is always restored.
    /// </summary>
    public void RequestSlowMotion(float duration, float targetScale)
    {
        if (slowMoCoroutine != null)
        {
            StopCoroutine(slowMoCoroutine);
        }
        slowMoCoroutine = StartCoroutine(SlowMoSequence(duration, targetScale));
    }

    private IEnumerator SlowMoSequence(float duration, float targetScale)
    {
        // Record the base scale we should return to (ignoring current if it was already in slowmo)
        float baseScale = timeScaleValues[currentScaleIndex];
        
        ApplyTimeScale(targetScale);
        
        // Use RealTime because Time.deltaTime is affected by scale
        yield return new WaitForSecondsRealtime(duration);
        
        ApplyTimeScale(baseScale);
        slowMoCoroutine = null;
    }
}

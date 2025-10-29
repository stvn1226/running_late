using UnityEngine;
using TMPro;
using System.Linq;

public class AnalyticsRuntimeMonitor : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI displayText;
    [SerializeField] private bool showOnlyInDevelopment = true;
    
    [Header("Display Settings")]
    [SerializeField] private int maxEventsToShow = 10;
    [SerializeField] private bool showParameters = false;
    [SerializeField] private float updateInterval = 0.5f;
    
    private float lastUpdateTime;
    
    private void Update()
    {
        if (showOnlyInDevelopment && !Debug.isDebugBuild)
        {
            if (displayText != null)
                displayText.gameObject.SetActive(false);
            return;
        }
        
        if (Time.time - lastUpdateTime < updateInterval)
            return;
        
        lastUpdateTime = Time.time;
        UpdateDisplay();
    }
    
    private void UpdateDisplay()
    {
        if (displayText == null || AnalyticsManager.Instance == null)
            return;
        
        var events = AnalyticsManager.Instance.EventBuffer;
        var recentEvents = events.Skip(Mathf.Max(0, events.Count - maxEventsToShow)).ToList();
        
        string text = $"<b>Analytics Monitor</b> ({events.Count} events)\n";
        text += new string('-', 40) + "\n";
        
        foreach (var evt in recentEvents)
        {
            text += $"<color=#00FF00>[{evt.timestamp}]</color> {evt.eventName}\n";
            
            if (showParameters && evt.parameters != null && evt.parameters.Count > 0)
            {
                text += $"  <size=10><color=#AAAAAA>{evt.GetParametersAsString()}</color></size>\n";
            }
        }
        
        displayText.text = text;
    }
}
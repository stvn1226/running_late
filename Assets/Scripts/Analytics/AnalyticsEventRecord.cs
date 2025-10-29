using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AnalyticsEventRecord
{
    public string eventName;
    public string timestamp;
    public string providerName;
    public Dictionary<string, object> parameters;
    public float sessionTime;
    
    public AnalyticsEventRecord(string eventName, string providerName, Dictionary<string, object> parameters)
    {
        this.eventName = eventName;
        this.providerName = providerName;
        this.parameters = parameters ?? new Dictionary<string, object>();
        this.timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        this.sessionTime = Time.realtimeSinceStartup;
    }
    
    public string GetParametersAsString()
    {
        if (parameters == null || parameters.Count == 0)
            return "No parameters";
        
        List<string> parts = new List<string>();
        foreach (var kvp in parameters)
        {
            parts.Add($"{kvp.Key}: {kvp.Value}");
        }
        return string.Join(", ", parts);
    }
}

[Serializable]
public class AnalyticsSession
{
    public string sessionId;
    public string sessionStartTime;
    public List<AnalyticsEventRecord> events = new List<AnalyticsEventRecord>();
    public int totalEvents => events.Count;
    
    public AnalyticsSession()
    {
        sessionId = Guid.NewGuid().ToString();
        sessionStartTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
    
    public void AddEvent(AnalyticsEventRecord eventRecord)
    {
        events.Add(eventRecord);
    }
    
    public Dictionary<string, int> GetEventCountByType()
    {
        Dictionary<string, int> counts = new Dictionary<string, int>();
        foreach (var evt in events)
        {
            if (!counts.ContainsKey(evt.eventName))
                counts[evt.eventName] = 0;
            counts[evt.eventName]++;
        }
        return counts;
    }
}
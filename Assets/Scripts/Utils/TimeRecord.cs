using System;

namespace Utils
{
    [Serializable]
    public class TimeRecord
    {
        public float time;
        public int seed;

        public TimeRecord(float time, int seed)
        {
            this.time = time;
            this.seed = seed;
        }

        public string GetFormattedTime()
        {
            int minutes = UnityEngine.Mathf.FloorToInt(time / 60f);
            int seconds = UnityEngine.Mathf.FloorToInt(time % 60f);
            int centiseconds = UnityEngine.Mathf.FloorToInt((time * 100f) % 100f);
        
            return $"{minutes:00}:{seconds:00}.{centiseconds:00}";
        }
    }
}
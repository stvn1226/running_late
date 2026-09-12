using System;

namespace Utils
{
    [Serializable]
    public class TimeRecord
    {
        public float time;
        public int seed;
        public string playerName;
        public string levelName;

        public TimeRecord(float time, int seed, string playerName = null, string levelName = null)
        {
            this.time = time;
            this.seed = seed;
            this.playerName = playerName;
            this.levelName = levelName;
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
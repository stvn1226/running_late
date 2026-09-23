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
        public int pathMode; // 0 = DualPath (Double Decker), 1 = SingleSidewalk (Single Decker)

        public TimeRecord(float time, int seed, string playerName = null, string levelName = null, int pathMode = 0)
        {
            this.time = time;
            this.seed = seed;
            this.playerName = playerName;
            this.levelName = levelName;
            this.pathMode = pathMode;
        }

        public static string FormatTime(float time)
        {
            int minutes = UnityEngine.Mathf.FloorToInt(time / 60f);
            int seconds = UnityEngine.Mathf.FloorToInt(time % 60f);
            int centiseconds = UnityEngine.Mathf.FloorToInt((time * 100f) % 100f);
        
            return $"{minutes:00}:{seconds:00}.{centiseconds:00}";
        }

        public string GetFormattedTime()
        {
            return FormatTime(time);
        }
    }
}
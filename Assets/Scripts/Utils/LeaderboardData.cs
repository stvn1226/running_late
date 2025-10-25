using System;
using System.Collections.Generic;

namespace Utils
{
    [Serializable]
    public class LeaderboardData
    {
        public List<TimeRecord> records = new List<TimeRecord>();
    }
}
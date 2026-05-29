using System;
using System.Collections.Generic;

class Program {
    static bool FindPerfectFit(float target, float[] available, List<float> result) {
        if (Math.Abs(target) < 0.1f) return true;
        if (target < 0) return false;

        foreach (float piece in available) {
            result.Add(piece);
            if (FindPerfectFit(target - piece, available, result)) return true;
            result.RemoveAt(result.Count - 1);
        }
        return false;
    }
}

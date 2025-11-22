using UnityEngine;

public interface ITimedPowerUp
{
    bool IsActive();
    float GetRemainingTime();
    float GetMaxTime();
    string GetPowerUpName();
}

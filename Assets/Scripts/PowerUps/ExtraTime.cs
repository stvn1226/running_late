using UnityEngine;

public class ExtraTime : PowerUp
{
    [Header("Extra Time Settings")]
    [SerializeField] private float timeToAdd = 10f;

    protected override void ApplyEffect(GameObject player)
    {
        Defeat defeatController = FindFirstObjectByType<Defeat>();

        if (defeatController != null)
        {
            defeatController.AddTime(timeToAdd);
        }
        else
        {
            GameLogger.LogError("POWERUP", "Defeat controller not found! Cannot add extra time.");
        }
    }
}

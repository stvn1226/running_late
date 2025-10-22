using UnityEngine;
using UnityEngine.UI;

public class Defeat : MonoBehaviour
{
    [Header("Time Settings")]
    [SerializeField] private float timeLimit = 120f;

    [Header("Visual Timer")]
    [SerializeField] private Image timerCountdown;

    private float elapsedTime = 0f;
    private bool isDefeated = false;

    void Update()
    {
        if (isDefeated)
            return;

        elapsedTime += Time.deltaTime;

        UpdateTimerVisual();

        if (elapsedTime >= timeLimit)
        {
            TriggerDefeat();
        }
    }

    private void UpdateTimerVisual()
    {
        if (timerCountdown != null)
        {
            float timeRemaining = Mathf.Max(0f, timeLimit - elapsedTime);
            timerCountdown.fillAmount = timeRemaining / timeLimit;
        }
    }

    private void TriggerDefeat()
    {
        isDefeated = true;
        Time.timeScale = 0f;
        Debug.Log("Time's up! Game frozen.");
    }
}

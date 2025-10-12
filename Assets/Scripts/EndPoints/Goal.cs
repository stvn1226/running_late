using UnityEngine;
using Utils;

public class Goal : MonoBehaviour
{
    [Header("Victory Settings")]
    [SerializeField] private GameObject victoryOverlayPrefab;
    [SerializeField] private bool pauseGameOnVictory = true;
    [SerializeField] private float victoryDelay = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    private bool hasTriggered = false;
    private GameObject spawnedVictoryOverlay;

    void Start()
    {
        // Ensure this GameObject has a trigger collider
        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            GameLogger.LogError("GOAL", "No Collider2D found! Please add a BoxCollider2D and set IsTrigger to true.");
        }
        else if (!collider.isTrigger)
        {
            GameLogger.LogWarning("GOAL", "Collider should be set as Trigger for proper collision detection.");
        }

    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the collider belongs to the Player
        if (other.CompareTag(TagUtils.PLAYER_TAG) && !hasTriggered)
        {
            if (showDebugInfo)
                GameLogger.Log("GOAL", $"Goal reached! Player '{other.name}' has reached the school!");

            TriggerVictory();
        }
    }

    private void TriggerVictory()
    {
        hasTriggered = true;

        if (showDebugInfo)
            GameLogger.Log("GOAL", "=== VICTORY TRIGGERED ===");

        // Start victory sequence
        StartCoroutine(VictorySequence());
    }

    private System.Collections.IEnumerator VictorySequence()
    {
        // Optional delay before showing victory screen
        if (victoryDelay > 0)
        {
            yield return new WaitForSeconds(victoryDelay);
        }

        // Show victory overlay
        ShowVictoryOverlay();

        // Pause the game if specified
        if (pauseGameOnVictory)
        {
            Time.timeScale = 0f;

            if (showDebugInfo)
                GameLogger.Log("GOAL", "Game paused for victory screen");
        }
    }

    private void ShowVictoryOverlay()
    {
        if (victoryOverlayPrefab != null)
        {
            // Spawn the victory overlay at the camera position
            Camera mainCamera = Camera.main;
            Vector3 spawnPosition = mainCamera != null ? mainCamera.transform.position : Vector3.zero;
            spawnPosition.z = 0; // Ensure it's in the 2D plane

            spawnedVictoryOverlay = Instantiate(victoryOverlayPrefab, spawnPosition, Quaternion.identity);

            if (showDebugInfo)
                GameLogger.Log("GOAL", "Victory overlay displayed!");
        }
        else
        {
            GameLogger.LogWarning("GOAL", "No Victory Overlay Prefab assigned!");
        }
    }

    // Public methods for UI buttons
    public void RestartGame()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void LoadNextLevel(string sceneName)
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }

    public void ReturnToMainMenu(string mainMenuScene = "MainMenu")
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuScene);
    }

    void Update()
    {
        
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;

public class SpawnManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject playerPrefab;
    public PlayerConfig playerConfig;
    public float initialImpulseForce = 10f;
    
    private GameObject currentPlayer;
    private SpawnPoint homeSpawnPoint;
    
    public static SpawnManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Subscribe to scene loaded events to handle respawning after scene reload
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void Start()
    {
        InitializeAndSpawnPlayer();
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Scene loaded: {scene.name}. Initializing SpawnManager...");
        
        // Reset references since scene objects were destroyed/recreated
        homeSpawnPoint = null;
        currentPlayer = null;
        
        // Initialize and spawn player for the new scene
        InitializeAndSpawnPlayer();
    }
    
    private void InitializeAndSpawnPlayer()
    {
        Debug.Log("SpawnManager: Starting initialization...");
        
        if (playerPrefab == null)
        {
            Debug.LogError("SpawnManager: No player prefab assigned! Please assign a player prefab in the inspector.");
            return;
        }
        
        if (playerConfig == null)
        {
            Debug.LogWarning("SpawnManager: No player config assigned! Using fallback spawn method.");
        }
        
        FindHomeSpawnPoint();
        SpawnPlayerAtHome();
    }
    
    private void FindHomeSpawnPoint()
    {
        GameObject homeObject = GameObject.FindGameObjectWithTag("Respawn");
        if (homeObject != null)
        {
            SpawnPoint spawnPoint = homeObject.GetComponentInChildren<SpawnPoint>();
            if (spawnPoint != null)
            {
                spawnPoint.isHomeSpawn = true;
                homeSpawnPoint = spawnPoint;
                Debug.Log($"Home spawn point found at: {spawnPoint.transform.position}");
            }
            else
            {
                Debug.LogError("SpawnPoint child not found in Home object!");
            }
        }
        else
        {
            Debug.LogError("No Home object or home spawn point found in scene!");
        }
    }
    
    public void SpawnPlayerAtHome()
    {
        if (homeSpawnPoint == null)
        {
            Debug.LogError("Cannot spawn player: no home spawn point found!");
            return;
        }
        
        SpawnPlayerAt(homeSpawnPoint.transform.position);
    }
    
    public void SpawnPlayerAt(Vector3 position)
    {
        CleanupAllPlayers();
        
        if (playerPrefab != null)
        {
            currentPlayer = Instantiate(playerPrefab, position, Quaternion.identity);
            
            Rigidbody2D playerRb = currentPlayer.GetComponent<Rigidbody2D>();
            PlayerStates playerStates = currentPlayer.GetComponent<PlayerStates>();
            
            if (playerRb != null && playerConfig != null && playerStates != null)
            {
                float maxSpeed = playerConfig.maxRunSpeed;
                
                // Set the velocity directly
                playerRb.linearVelocity = new Vector2(maxSpeed, 0f);
                
                // Activate the momentum preservation system
                playerStates.justLanded = true;
                playerStates.landingGraceTimer = playerConfig.landingGracePeriod;
                playerStates.landingHorizontalVelocity = maxSpeed;
                
                Debug.Log($"Player spawned with momentum preservation active: {maxSpeed}");
            }
            else if (playerRb != null)
            {
                playerRb.AddForce(Vector2.right * initialImpulseForce, ForceMode2D.Impulse);
                Debug.Log("Missing components, using fallback impulse method");
            }
            else
            {
                Debug.LogWarning("Player prefab doesn't have a Rigidbody2D component!");
            }
        }
        else
        {
            Debug.LogError("No player prefab assigned!");
            return;
        }
        
        Debug.Log($"Player spawned at: {position}");
    }
    
    private void CleanupAllPlayers()
    {
        GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");
        
        foreach (GameObject player in allPlayers)
        {
            if (player != null)
            {
                if (player == currentPlayer)
                {
                    currentPlayer = null;
                }
                Destroy(player);
                Debug.Log($"Removed existing player: {player.name}");
            }
        }
        
        if (currentPlayer != null)
        {
            Destroy(currentPlayer);
            currentPlayer = null;
            Debug.Log("Removed tracked current player");
        }
    }
        
    public void RespawnPlayer()
    {
        SpawnPlayerAtHome();
    }
    
    public GameObject GetCurrentPlayer()
    {
        return currentPlayer;
    }
    
    public Vector3 GetHomeSpawnPosition()
    {
        if (homeSpawnPoint != null)
        {
            return homeSpawnPoint.transform.position;
        }
        return Vector3.zero;
    }
    
    public void EnsureOnlyOnePlayer()
    {
        GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");
        
        if (allPlayers.Length <= 1)
        {
            if (allPlayers.Length == 1)
            {
                currentPlayer = allPlayers[0];
            }
            return;
        }
        
        Debug.LogWarning($"Found {allPlayers.Length} players in scene. Cleaning up duplicates...");
        
        GameObject playerToKeep = currentPlayer != null ? currentPlayer : allPlayers[0];
        
        foreach (GameObject player in allPlayers)
        {
            if (player != playerToKeep && player != null)
            {
                Debug.Log($"Removing duplicate player: {player.name}");
                Destroy(player);
            }
        }
        
        currentPlayer = playerToKeep;
    }
    
    public int GetPlayerCount()
    {
        return GameObject.FindGameObjectsWithTag("Player").Length;
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from scene events to prevent memory leaks
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
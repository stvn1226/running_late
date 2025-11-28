using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Utils;

public class SpawnManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject playerPrefab;
    public PlayerConfig playerConfig;
    public float initialImpulseForce = 10f;
    
    [Header("Countdown Settings")]
    [SerializeField] private bool useCountdown = true;
    [SerializeField] private float delayBeforeCountdown = 0.2f;
    
    private GameObject currentPlayer;
    private SpawnPoint homeSpawnPoint;
    private bool waitingForLevel = false;
    private bool playerInputDisabled = false;
    
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
        
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void Start()
    {
        if (LevelStarter.Instance != null && LevelStarter.Instance.HasGeneratedLevel)
        {
            InitializeAndSpawnPlayer();
        }
        else
        {
            waitingForLevel = true;
        }
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        homeSpawnPoint = null;
        currentPlayer = null;
        waitingForLevel = true;
        GameReferences.Instance.OnSceneChanged();
    }
    
    public void OnLevelGenerated()
    {
        if (waitingForLevel)
        {
            waitingForLevel = false;
            StartCoroutine(DelayedSpawn());
        }
    }
    
    private IEnumerator DelayedSpawn()
    {
        yield return new WaitForEndOfFrame();
        InitializeAndSpawnPlayer();
    }
    
    public void InitializeAndSpawnPlayer()
    {
        if (playerPrefab == null)
        {
            GameLogger.LogError("SPAWN", "No player prefab assigned!");
            return;
        }
        
        FindHomeSpawnPoint();
        SpawnPlayerAtHome();
        
        if (useCountdown && StartCountdownController.Instance != null)
        {
            DisablePlayerInput();
            StartCoroutine(StartCountdownSequence());
        }
    }
    
    private IEnumerator StartCountdownSequence()
    {
        yield return new WaitForSeconds(delayBeforeCountdown);
        
        StartCountdownController.Instance.OnCountdownComplete += OnCountdownFinished;
        StartCountdownController.Instance.StartCountdown();
    }
    
    private void OnCountdownFinished()
    {
        ApplyInitialVelocity();
        EnablePlayerInput();
        
        if (StartCountdownController.Instance != null)
        {
            StartCountdownController.Instance.OnCountdownComplete -= OnCountdownFinished;
        }
        
        GameLogger.Log("SPAWN", "Countdown complete - player launched!");
    }
    
    private void DisablePlayerInput()
    {
        if (currentPlayer != null)
        {
            PlayerControllerModular controller = currentPlayer.GetComponent<PlayerControllerModular>();
            if (controller != null)
            {
                controller.SetInputEnabled(false);
                playerInputDisabled = true;
                GameLogger.Log("SPAWN", "Player input disabled for countdown");
            }
        }
    }
    
    private void EnablePlayerInput()
    {
        if (currentPlayer != null)
        {
            PlayerControllerModular controller = currentPlayer.GetComponent<PlayerControllerModular>();
            if (controller != null)
            {
                controller.SetInputEnabled(true);
                playerInputDisabled = false;
                GameLogger.Log("SPAWN", "Player input enabled after countdown");
            }
        }
    }
    
    private void FindHomeSpawnPoint()
    {
        if (homeSpawnPoint != null) return;
        
        GameObject homeObject = GameReferences.Instance.RespawnObject;
        if (homeObject != null)
        {
            SpawnPoint spawnPoint = homeObject.GetComponentInChildren<SpawnPoint>();
            if (spawnPoint != null)
            {
                spawnPoint.isHomeSpawn = true;
                homeSpawnPoint = spawnPoint;
            }
            else
            {
                GameLogger.LogError("SPAWN", "SpawnPoint child not found in Home object!");
            }
        }
        else
        {
            GameLogger.LogError("SPAWN", "No Home object found in scene!");
        }
    }
    
    public void SpawnPlayerAtHome()
    {
        if (homeSpawnPoint == null)
        {
            GameLogger.LogError("SPAWN", "Cannot spawn player: no home spawn point found!");
            return;
        }

        CleanupAllPlayers();
        SpawnPlayerAt(homeSpawnPoint.transform.position);
    }

    private int counter = 1;
    public void SpawnPlayerAt(Vector3 position)
    {
        if (playerPrefab != null)
        {
            currentPlayer = Instantiate(playerPrefab, position, Quaternion.identity);
            currentPlayer.name = $"{playerPrefab.name}_Instance_{counter++}";

            GameReferences.Instance.OnPlayerSpawned(currentPlayer);

            if (!useCountdown)
            {
                ApplyInitialVelocity();
            }
        }
    }
    
    private void ApplyInitialVelocity()
    {
        if (currentPlayer == null) return;
        
        Rigidbody2D playerRb = currentPlayer.GetComponent<Rigidbody2D>();
        PlayerStates playerStates = currentPlayer.GetComponent<PlayerStates>();
        
        if (playerRb != null && playerConfig != null && playerStates != null)
        {
            float maxSpeed = playerConfig.maxRunSpeed;
            
            playerRb.linearVelocity = new Vector2(maxSpeed, 0f);
            
            playerStates.justLanded = true;
            playerStates.landingGraceTimer = playerConfig.landingGracePeriod;
            playerStates.landingHorizontalVelocity = maxSpeed;
            
            GameLogger.Log("SPAWN", "Applied initial velocity to player");
        }
        else if (playerRb != null)
        {
            playerRb.AddForce(Vector2.right * initialImpulseForce, ForceMode2D.Impulse);
        }
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
            }
        }
        
        if (currentPlayer != null)
        {
            Destroy(currentPlayer);
            currentPlayer = null;
        }

        GameReferences.Instance.OnPlayerDestroyed();
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
        
        GameObject playerToKeep = currentPlayer != null ? currentPlayer : allPlayers[0];
        
        foreach (GameObject player in allPlayers)
        {
            if (player != playerToKeep && player != null)
            {
                Destroy(player);
            }
        }
        
        currentPlayer = playerToKeep;
    }
    
    public int GetPlayerCount()
    {
        return GameObject.FindGameObjectsWithTag(TagUtils.PLAYER_TAG).Length;
    }
    
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}

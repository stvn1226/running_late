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
    public static System.Action<GameObject> OnPlayerSpawned;
    
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
        
        GameLogger.Log("SPAWN", "SpawnManager AWAKE");
    }
    
    private void Start()
    {
        GameLogger.Log("SPAWN", "SpawnManager START");
        GameLogger.Log("SPAWN", $"  LevelStarter.Instance exists: {LevelStarter.Instance != null}");
        GameLogger.Log("SPAWN", $"  Level generated: {LevelStarter.Instance != null && LevelStarter.Instance.HasGeneratedLevel}");
        
        if (LevelStarter.Instance != null && LevelStarter.Instance.HasGeneratedLevel)
        {
            GameLogger.Log("SPAWN", "Level already generated, initializing spawn");
            InitializeAndSpawnPlayer();
        }
        else
        {
            waitingForLevel = true;
            GameLogger.Log("SPAWN", "Waiting for level generation");
        }
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        GameLogger.Log("SPAWN", $"Scene Loaded: {scene.name}, Mode: {mode}");
        homeSpawnPoint = null;
        currentPlayer = null;
        waitingForLevel = true;
        GameReferences.Instance.OnSceneChanged();
    }
    
    public void OnLevelGenerated()
    {
        GameLogger.Log("SPAWN", "OnLevelGenerated EVENT RECEIVED");
        
        if (waitingForLevel)
        {
            waitingForLevel = false;
            StartCoroutine(DelayedSpawn());
        }
        else
        {
            GameLogger.LogWarning("SPAWN", "OnLevelGenerated called but not waiting for level");
        }
    }
    
    private IEnumerator DelayedSpawn()
    {
        GameLogger.Log("SPAWN", "DelayedSpawn coroutine started, waiting one frame");
        yield return new WaitForEndOfFrame();
        InitializeAndSpawnPlayer();
    }
    
    public void InitializeAndSpawnPlayer()
    {
        GameLogger.Log("SPAWN", "InitializeAndSpawnPlayer CALLED");
        GameLogger.Log("SPAWN", $"  Player prefab assigned: {playerPrefab != null}");
        GameLogger.Log("SPAWN", $"  Player config assigned: {playerConfig != null}");
        
        if (playerPrefab == null)
        {
            GameLogger.LogError("SPAWN", "No player prefab assigned");
            return;
        }
        
        FindHomeSpawnPoint();
        SpawnPlayerAtHome();
        
        if (useCountdown && StartCountdownController.Instance != null)
        {
            DisablePlayerInput();
            Time.timeScale = 0f;
            StartCoroutine(StartCountdownSequence());
        }
    }
    
    private IEnumerator StartCountdownSequence()
    {
        yield return new WaitForSecondsRealtime(delayBeforeCountdown);
        
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
        if (homeSpawnPoint != null)
        {
            GameLogger.Log("SPAWN", $"Home spawn point already cached: {homeSpawnPoint.name} at {homeSpawnPoint.transform.position}");
            return;
        }
        
        GameLogger.Log("SPAWN", "Searching for Home spawn point");
        
        GameObject homeObject = GameReferences.Instance.RespawnObject;
        if (homeObject != null)
        {
            GameLogger.Log("SPAWN", $"  Found Home object: '{homeObject.name}' at {homeObject.transform.position}");
            
            SpawnPoint spawnPoint = homeObject.GetComponentInChildren<SpawnPoint>();
            if (spawnPoint != null)
            {
                spawnPoint.isHomeSpawn = true;
                homeSpawnPoint = spawnPoint;
                GameLogger.Log("SPAWN", $"  Home spawn point SET: '{spawnPoint.name}' at {spawnPoint.transform.position}");
            }
            else
            {
                GameLogger.LogError("SPAWN", "  SpawnPoint component NOT FOUND in Home children");
            }
        }
        else
        {
            GameLogger.LogError("SPAWN", "  Home object NOT FOUND in scene (tag 'Respawn')");
        }
    }
    
    public void SpawnPlayerAtHome()
    {
        if (homeSpawnPoint == null)
        {
            GameLogger.LogError("SPAWN", "Cannot spawn: no home spawn point");
            return;
        }

        Vector3 spawnPos = homeSpawnPoint.transform.position;
        GameLogger.Log("SPAWN", $"SpawnPlayerAtHome - Position: {spawnPos}");
        
        CleanupAllPlayers();
        StartCoroutine(SpawnPlayerAtCoroutine(spawnPos));
    }

    private int spawnCounter = 1;
    
    private IEnumerator SpawnPlayerAtCoroutine(Vector3 position)
    {
        GameLogger.Log("SPAWN", "SpawnPlayerAtCoroutine START");
        GameLogger.Log("SPAWN", $"  Target position: {position}");
        GameLogger.Log("SPAWN", $"  Time.timeScale: {Time.timeScale}");
        GameLogger.Log("SPAWN", $"  Physics2D.simulationMode: {Physics2D.simulationMode}");
        GameLogger.Log("SPAWN", $"  Physics2D.autoSimulation: {Physics2D.simulationMode == SimulationMode2D.FixedUpdate}");
        
        currentPlayer = Instantiate(playerPrefab, position, Quaternion.identity);
        currentPlayer.name = $"{playerPrefab.name}_Instance_{spawnCounter++}";
        
        GameLogger.Log("SPAWN", $"  Player instantiated: '{currentPlayer.name}'");
        GameLogger.Log("SPAWN", $"  Position immediately after instantiate: {currentPlayer.transform.position}");

        PlayerPhysicsDebugger debugger = currentPlayer.GetComponent<PlayerPhysicsDebugger>();
        if (debugger == null)
        {
            debugger = currentPlayer.AddComponent<PlayerPhysicsDebugger>();
            GameLogger.Log("SPAWN", "  Added PlayerPhysicsDebugger component");
        }
        else
        {
            GameLogger.Log("SPAWN", "  PlayerPhysicsDebugger already exists");
        }

        GameReferences.Instance.OnPlayerSpawned(currentPlayer);
        OnPlayerSpawned?.Invoke(currentPlayer);
        GameLogger.Log("SPAWN", "  GameReferences and OnPlayerSpawned event notified");

        Rigidbody2D playerRb = currentPlayer.GetComponent<Rigidbody2D>();
        PlayerStates playerStates = currentPlayer.GetComponent<PlayerStates>();
        
        if (playerRb != null)
        {
            GameLogger.Log("SPAWN", "  Rigidbody2D found:");
            GameLogger.Log("SPAWN", $"    Initial velocity: {playerRb.linearVelocity}");
            GameLogger.Log("SPAWN", $"    Mass: {playerRb.mass}");
            GameLogger.Log("SPAWN", $"    Gravity scale: {playerRb.gravityScale}");
            GameLogger.Log("SPAWN", $"    Body type: {playerRb.bodyType}");
            GameLogger.Log("SPAWN", $"    Simulated: {playerRb.simulated}");
            GameLogger.Log("SPAWN", $"    Is kinematic: {playerRb.isKinematic}");
        }
        else
        {
            GameLogger.LogError("SPAWN", "  Rigidbody2D NOT FOUND");
        }
        
        if (playerStates != null)
        {
            GameLogger.Log("SPAWN", "  PlayerStates found:");
            GameLogger.Log("SPAWN", $"    isGrounded: {playerStates.isGrounded}");
            GameLogger.Log("SPAWN", $"    wasGrounded: {playerStates.wasGrounded}");
        }
        else
        {
            GameLogger.LogError("SPAWN", "  PlayerStates NOT FOUND");
        }

        if (!useCountdown)
        {
            ApplyInitialVelocity();
        }
        
        yield return new WaitForFixedUpdate();
        
        if (currentPlayer != null)
        {
            Vector3 posAfterPhysics = currentPlayer.transform.position;
            Vector3 movement = posAfterPhysics - position;
            
            GameLogger.Log("SPAWN", "After FIRST PHYSICS UPDATE:");
            GameLogger.Log("SPAWN", $"  Position: {posAfterPhysics}");
            GameLogger.Log("SPAWN", $"  Movement from spawn: {movement}");
            GameLogger.Log("SPAWN", $"  Movement magnitude: {movement.magnitude:F3}");
            
            if (playerRb != null)
            {
                GameLogger.Log("SPAWN", $"  Velocity: {playerRb.linearVelocity}");
            }
            
            if (movement.magnitude > 5f)
            {
                GameLogger.LogWarning("SPAWN", $"WARNING: Player moved {movement.magnitude:F2} units in first physics frame");
            }
            
            if (posAfterPhysics.y < position.y - 1f)
            {
                GameLogger.LogWarning("SPAWN", $"WARNING: Player is falling - Y dropped from {position.y:F2} to {posAfterPhysics.y:F2}");
                
                if (playerRb != null && playerStates != null)
                {
                    GameLogger.LogWarning("SPAWN", $"  Grounded state: {playerStates.isGrounded}");
                    GameLogger.LogWarning("SPAWN", $"  Velocity: {playerRb.linearVelocity}");
                }
            }
        }
        else
        {
            GameLogger.LogError("SPAWN", "Player was destroyed during spawn coroutine");
        }
        
        GameLogger.Log("SPAWN", "SpawnPlayerAtCoroutine COMPLETE");
    }
    
    private void ApplyInitialVelocity()
    {
        if (currentPlayer == null) return;
        
        Rigidbody2D playerRb = currentPlayer.GetComponent<Rigidbody2D>();
        PlayerStates playerStates = currentPlayer.GetComponent<PlayerStates>();
        
        if (playerRb != null && playerConfig != null && playerStates != null)
        {
            float maxSpeed = playerConfig.maxRunSpeed;
            
            GameLogger.Log("SPAWN", $"  Setting initial velocity to ({maxSpeed}, 0)");
            playerRb.linearVelocity = new Vector2(maxSpeed, 0f);
            
            playerStates.justLanded = true;
            playerStates.landingGraceTimer = playerConfig.landingGracePeriod;
            playerStates.landingHorizontalVelocity = maxSpeed;
            
            GameLogger.Log("SPAWN", $"  Player configured:");
            GameLogger.Log("SPAWN", $"    Velocity SET: {playerRb.linearVelocity}");
            GameLogger.Log("SPAWN", $"    Landing grace active: {playerConfig.landingGracePeriod}s");
            GameLogger.Log("SPAWN", $"    Landing horizontal velocity: {maxSpeed}");
        }
        else if (playerRb != null)
        {
            GameLogger.LogWarning("SPAWN", "  PlayerConfig or PlayerStates missing, using impulse fallback");
            playerRb.AddForce(Vector2.right * initialImpulseForce, ForceMode2D.Impulse);
            GameLogger.Log("SPAWN", $"  Applied impulse force: {initialImpulseForce}");
        }
    }
    
    public void SpawnPlayerAt(Vector3 position)
    {
        GameLogger.Log("SPAWN", $"SpawnPlayerAt called - Position: {position}");
        StartCoroutine(SpawnPlayerAtCoroutine(position));
    }
    
    public void CleanupAllPlayers()
    {
        GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");
        
        GameLogger.Log("SPAWN", $"CleanupAllPlayers - Found {allPlayers.Length} player(s)");
        
        foreach (GameObject player in allPlayers)
        {
            if (player != null)
            {
                GameLogger.Log("SPAWN", $"  Destroying: {player.name}");
                if (player == currentPlayer)
                {
                    currentPlayer = null;
                }
                Destroy(player);
            }
        }
        
        if (currentPlayer != null)
        {
            GameLogger.Log("SPAWN", "  Destroying current player reference");
            Destroy(currentPlayer);
            currentPlayer = null;
        }

        GameReferences.Instance.OnPlayerDestroyed();
        GameLogger.Log("SPAWN", "  GameReferences notified of player destruction");
    }
        
    public void RespawnPlayer()
    {
        GameLogger.Log("SPAWN", "RespawnPlayer called");
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
        
        GameLogger.LogWarning("SPAWN", $"DUPLICATE PLAYERS FOUND: {allPlayers.Length} players exist, cleaning up");
        
        GameObject playerToKeep = currentPlayer != null ? currentPlayer : allPlayers[0];
        
        foreach (GameObject player in allPlayers)
        {
            if (player != playerToKeep && player != null)
            {
                GameLogger.Log("SPAWN", $"  Destroying duplicate: {player.name}");
                Destroy(player);
            }
        }
        
        currentPlayer = playerToKeep;
        GameLogger.Log("SPAWN", $"  Kept player: {currentPlayer.name}");
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

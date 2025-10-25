using UnityEngine;

public class GameReferences : MonoBehaviour
{
    private static GameReferences instance;
    public static GameReferences Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GameReferences>();
                if (instance == null)
                {
                    GameObject go = new GameObject("[GameReferences]");
                    instance = go.AddComponent<GameReferences>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    private Transform playerTransform;
    private Camera mainCamera;
    private PlayerMovement playerMovement;
    private GameObject respawnObject;

    public Transform PlayerTransform
    {
        get
        {
            if (playerTransform == null && SpawnManager.Instance != null)
            {
                GameObject player = SpawnManager.Instance.GetCurrentPlayer();
                if (player != null)
                {
                    playerTransform = player.transform;
                    playerMovement = player.GetComponent<PlayerMovement>();
                }
            }
            return playerTransform;
        }
    }

    public PlayerMovement PlayerMovement => playerMovement;

    public Camera MainCamera
    {
        get
        {
            if (mainCamera == null)
                mainCamera = Camera.main;
            return mainCamera;
        }
    }

    public GameObject RespawnObject
    {
        get
        {
            if (respawnObject == null)
                respawnObject = GameObject.FindGameObjectWithTag("Respawn");
            return respawnObject;
        }
    }

    public void OnPlayerSpawned(GameObject player)
    {
        playerTransform = player.transform;
        playerMovement = player.GetComponent<PlayerMovement>();
    }

    public void OnPlayerDestroyed()
    {
        playerTransform = null;
        playerMovement = null;
    }

    public void OnSceneChanged()
    {
        respawnObject = null;
        playerTransform = null;
        playerMovement = null;
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
}

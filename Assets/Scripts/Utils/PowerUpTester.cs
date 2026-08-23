using UnityEngine;

public class PowerUpTester : MonoBehaviour
{
    [Header("Power Up Prefabs")]
    public GameObject doubleJumpPrefab;
    public GameObject shieldPrefab;
    public GameObject extraSpeedPrefab;
    public GameObject invincibilityPrefab;
    public GameObject extraHeightPrefab;
    public GameObject airFloatPrefab;
    public GameObject toastPrefab;
    public GameObject extraTimePrefab;

    [Header("Settings")]
    public bool showOnScreenButtons = true;

    private GameObject player;

    private void Start()
    {
        FindPlayer();
    }

    private void FindPlayer()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            var states = FindObjectOfType<PlayerStates>();
            if (states != null) player = states.gameObject;
        }
    }

    private void Update()
    {
        if (player == null)
        {
            FindPlayer();
            if (player == null) return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) TriggerPowerUp(doubleJumpPrefab);
        if (Input.GetKeyDown(KeyCode.Alpha2)) TriggerPowerUp(shieldPrefab);
        if (Input.GetKeyDown(KeyCode.Alpha3)) TriggerPowerUp(extraSpeedPrefab);
        if (Input.GetKeyDown(KeyCode.Alpha4)) TriggerPowerUp(invincibilityPrefab);
        if (Input.GetKeyDown(KeyCode.Alpha5)) TriggerPowerUp(extraHeightPrefab);
        if (Input.GetKeyDown(KeyCode.Alpha6)) TriggerPowerUp(airFloatPrefab);
        if (Input.GetKeyDown(KeyCode.Alpha7)) TriggerPowerUp(toastPrefab);
        if (Input.GetKeyDown(KeyCode.Alpha8)) TriggerPowerUp(extraTimePrefab);
        if (Input.GetKeyDown(KeyCode.R)) ResetAllPowerUps();
    }

    public void TriggerPowerUp(GameObject prefab)
    {
        if (prefab == null) return;
        if (player == null) FindPlayer();
        if (player == null) return;

        GameObject tempObj = Instantiate(prefab, player.transform.position, Quaternion.identity);
        PowerUp powerUp = tempObj.GetComponent<PowerUp>();
        if (powerUp != null)
        {
            powerUp.PickUp(player);
        }
    }

    public void ResetAllPowerUps()
    {
        if (player == null) FindPlayer();
        if (player != null)
        {
            var states = player.GetComponentInParent<PlayerStates>();
            if (states != null)
            {
                states.hasDoubleJump = false;
                states.hasHighJump = false;
                states.hasSlowFall = false;
                states.hasFastRun = false;
                states.hasInvincibilityPowerUp = false;
                states.isInvincible = false;
                states.RemoveShield();
            }
        }
        
        if (UIPowerUpHandler.Instance != null)
        {
            UIPowerUpHandler.Instance.UnregisterPowerUp("DoubleJump");
            UIPowerUpHandler.Instance.UnregisterPowerUp("Shield");
            UIPowerUpHandler.Instance.UnregisterPowerUp("ExtraSpeed");
            UIPowerUpHandler.Instance.UnregisterPowerUp("Invincibility");
            UIPowerUpHandler.Instance.UnregisterPowerUp("ExtraHeight");
            UIPowerUpHandler.Instance.UnregisterPowerUp("AirFloat");
        }
    }

    private void OnGUI()
    {
        if (!showOnScreenButtons) return;

        GUILayout.BeginArea(new Rect(10, 10, 230, 370), "Power-Up Tester", GUI.skin.window);
        
        if (GUILayout.Button("1: Double Jump")) TriggerPowerUp(doubleJumpPrefab);
        if (GUILayout.Button("2: Shield (Chicle)")) TriggerPowerUp(shieldPrefab);
        if (GUILayout.Button("3: Extra Speed (Audífonos)")) TriggerPowerUp(extraSpeedPrefab);
        if (GUILayout.Button("4: Invincibility")) TriggerPowerUp(invincibilityPrefab);
        if (GUILayout.Button("5: Super Jump (Altura)")) TriggerPowerUp(extraHeightPrefab);
        if (GUILayout.Button("6: Air Float (Flotar)")) TriggerPowerUp(airFloatPrefab);
        if (GUILayout.Button("7: Toast (Curar Vida)")) TriggerPowerUp(toastPrefab);
        if (GUILayout.Button("8: Extra Time (+Seg)")) TriggerPowerUp(extraTimePrefab);
        
        GUILayout.Space(10);
        GUI.color = new Color(1f, 0.4f, 0.4f);
        if (GUILayout.Button("R: Reset All")) ResetAllPowerUps();
        GUI.color = Color.white;

        GUILayout.EndArea();
    }
}

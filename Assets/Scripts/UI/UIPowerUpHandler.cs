using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Unity.VisualScripting;
using System.Linq;

public class UIPowerUpHandler : MonoBehaviour
{
    public static UIPowerUpHandler Instance;
    private List<UIPowerUp> _UIPowerUps;
    [SerializeField] private GameObject _template;

    void Start()
    {
        Instance = this;
        _UIPowerUps = new List<UIPowerUp>();

    }

    public bool RegisterPowerUp(PowerUpContract powerUpContract)
    {
        if (!IsValidPowerUp(powerUpContract.powerUpName)) return false;

        // Custom priority logic: ExtraSpeed (Headphones) vs Shield (Gum)
        if (powerUpContract.powerUpName == "ExtraSpeed")
        {
            if (_UIPowerUps.Any(x => x.powerUpName == "Shield"))
            {
                UnregisterPowerUp("Shield");
                // Remove the actual shield effect from the player
                FindObjectOfType<PlayerStates>()?.RemoveShield();
                GameLogger.Log("POWERUP", "Priority conflict: Removed Gum because Headphones were picked up");
            }
        }
        else if (powerUpContract.powerUpName == "Shield")
        {
            if (_UIPowerUps.Any(x => x.powerUpName == "ExtraSpeed"))
            {
                GameLogger.Log("POWERUP", "Priority conflict: Gum ignored because Headphones are active");
                return false;
            }
        }

        //if power up is new...
        if (!_UIPowerUps.Any(x => x.powerUpName == powerUpContract.powerUpName))
        {
            GameObject myObject = Instantiate(_template, this.transform);
            myObject.transform.SetParent(this.transform);
            UIPowerUp myUIPowerUp = myObject.GetComponent<UIPowerUp>();
            myUIPowerUp.Initialize(powerUpContract);
            myUIPowerUp.AssignHandler(this);
            _UIPowerUps.Add(myUIPowerUp);
        }
        else //if power up already exists... 
        {
            UIPowerUp powerUp = _UIPowerUps.First(x => x.powerUpName == powerUpContract.powerUpName);
            powerUp.Initialize(powerUpContract);
        }

        return true;
    }

    public void UnregisterPowerUp(string powerUpName)
    {
        UIPowerUp powerUp = _UIPowerUps.FirstOrDefault(x => x.powerUpName == powerUpName);
        if (powerUp != null)
        {
            _UIPowerUps.Remove(powerUp);
            Destroy(powerUp.gameObject);
        }
    }

    private bool IsValidPowerUp(string powerUpName)
    {
        if (powerUpName == "ExtraTime") return false;
        return true;
    }
}

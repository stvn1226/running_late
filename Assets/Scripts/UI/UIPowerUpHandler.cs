using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class UIPowerUpHandler : MonoBehaviour
{
    public static UIPowerUpHandler Instance;
    private List<UIPowerUp> _UIPowerUps;
    [SerializeField] private GameObject _template;

    void Awake()
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
            UIPowerUp myUIPowerUp = myObject.GetComponentInChildren<UIPowerUp>();
            if (myUIPowerUp != null)
            {
                myUIPowerUp.AssignHandler(this);
                myUIPowerUp.Initialize(powerUpContract);
                _UIPowerUps.Add(myUIPowerUp);
            }
            else
            {
                GameLogger.LogError("POWERUP", $"UIPowerUpHandler: No UIPowerUp component found on template '{_template.name}' or its children!");
            }
        }
        else //if power up already exists... 
        {
            UIPowerUp powerUp = _UIPowerUps.FirstOrDefault(x => x.powerUpName == powerUpContract.powerUpName);
            if (powerUp != null)
            {
                powerUp.Initialize(powerUpContract);
            }
        }

        return true;
    }

    public void UnregisterPowerUp(string powerUpName)
    {
        UIPowerUp powerUp = _UIPowerUps.FirstOrDefault(x => x.powerUpName == powerUpName);
        if (powerUp != null)
        {
            _UIPowerUps.Remove(powerUp);
            
            // Si el objeto está dentro de un contenedor padre (ej. PowerUpContainer), destruye el contenedor completo
            if (powerUp.transform.parent != null && powerUp.transform.parent != this.transform)
            {
                Destroy(powerUp.transform.parent.gameObject);
            }
            else
            {
                Destroy(powerUp.gameObject);
            }
        }
    }

    private bool IsValidPowerUp(string powerUpName)
    {
        return !string.IsNullOrEmpty(powerUpName);
    }
}

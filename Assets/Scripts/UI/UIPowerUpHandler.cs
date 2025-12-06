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

    public void RegisterPowerUp(PowerUpContract powerUpContract)
    {
        if (!IsValidPowerUp(powerUpContract.powerUpName)) return;

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

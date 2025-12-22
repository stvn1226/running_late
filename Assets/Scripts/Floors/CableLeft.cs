using UnityEngine;

public class CableLeft : MonoBehaviour
{
    public bool hasBeenUsed = false;
    void Start()
    {
        CableManager.instance.AddCableEnd(this);
    }
}

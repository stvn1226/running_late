using System.Collections.Generic;
using UnityEngine;

public class CableRight : MonoBehaviour
{
    public float minDistance;
    public float maxDistance;

    void Start()
    {
        CableManager.instance.AddCableStart(this);
    }
}

using System.Collections.Generic;
using UnityEngine;

public class CableRight : MonoBehaviour
{
    private List<CableLeft> leftCornersInRange;
    [SerializeField] private float minDistance;
    [SerializeField] private float maxDistance;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //float distance = 0;
        //CableLeft closestCorner = null;
        //foreach (CableLeft corner in leftCornersInRange)
        //{
        //    if (corner.transform.position.x > gameObject.transform.position.x)
        //    {
        //        if ((corner.transform.position.x - gameObject.transform.position.x) > distance)
        //        {
        //            distance = (corner.transform.position.x - gameObject.transform.position.x);
        //            closestCorner = corner;
        //        }
        //    }
        //}
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        //CableLeft tryGetComponent = collision.GetComponent<CableLeft>();
        //if (tryGetComponent != null)
        //{
        //    leftCornersInRange.Add(tryGetComponent);
        //}
    }
}

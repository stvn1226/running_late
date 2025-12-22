using GogoGaga.OptimizedRopesAndCables;
using System.Collections.Generic;
using UnityEngine;

public class CableManager : MonoBehaviour
{
    public static CableManager instance { get; private set; }
    private List<CableRight> _rightCorners;
    private List<CableLeft> _leftCorners;

    [SerializeField] private Rope _cablePrefab;
    [SerializeField] private float _cableChance = .4f; //40% chance cable will be built if requirements are met 

    private bool _cablesSet;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
        }
        else instance = this;

        _rightCorners = new List<CableRight>();
        _leftCorners = new List<CableLeft>();
        _cablesSet = false;
    }

    private void Start()
    {
        //SetupCables();
    }

    private void Update()
    {
        if (_rightCorners.Count > 0 && _cablesSet == false)
        {
            SetupCables();
            _cablesSet = true;
        }
    }

    public void AddCableStart(CableRight startPoint)
    {
        _rightCorners.Add(startPoint);
    }

    public void AddCableEnd(CableLeft endPoint)
    {
        _leftCorners.Add(endPoint);
    }

    private void SetupCables()
    {
        foreach (CableRight startPoint in _rightCorners)
        {
            float willCable = Random.Range(0f, 1f);
            if (willCable > _cableChance) continue; //skip cable endpoint detection if we will not place a cable on that startpoint
            foreach (CableLeft endPoint in _leftCorners)
            {
                if (endPoint.hasBeenUsed) continue;
                if (endPoint.transform.position.x - startPoint.transform.position.x <= 0) continue; //endPoint building should be further right than startPoint
                if (endPoint.transform.position.y - startPoint.transform.position.y > 0) continue; //endPoint building should be lower than startPoint
                if (endPoint.transform.position.x > (startPoint.transform.position.x + startPoint.minDistance) &&
                    endPoint.transform.position.x < (startPoint.transform.position.x + startPoint.maxDistance) &&
                    endPoint.transform.position.y < (startPoint.transform.position.y - startPoint.minDistance) &&
                    endPoint.transform.position.y > (startPoint.transform.position.y - startPoint.maxDistance))
                {
                    endPoint.hasBeenUsed = BuildCable(startPoint, endPoint);
                    break;
                }
            }
        }
    }

    private bool BuildCable(CableRight start, CableLeft end)
    {
        Rope newCable = Instantiate(_cablePrefab, transform);
        newCable.SetStartPoint(start.transform);
        newCable.SetEndPoint(end.transform);
        newCable.ropeLength = Vector3.Distance(start.transform.position, end.transform.position) + 10f;

        LineRenderer lineRenderer = newCable.GetComponent<LineRenderer>();
        MeshCollider meshCollider = newCable.GetComponent<MeshCollider>();

        Mesh mesh = new Mesh();
        lineRenderer.BakeMesh(mesh, true);
        meshCollider.sharedMesh = mesh;

        return true;
    }
}

using GogoGaga.OptimizedRopesAndCables;
using System.Collections.Generic;
using UnityEngine;

public class CableManager : MonoBehaviour
{    
    public static CableManager instance { get; private set; }
    private List<CableRight> _rightCorners;
    private List<CableLeft> _leftCorners;

    private List<Rope> _cablesList;

    [SerializeField] private Rope _cablePrefab;
    [SerializeField] private float _cableChance = .4f; //40% chance cable will be built if requirements are met 

    private bool _cablesSet;
    private bool _collidersSet;

    public bool isFastFalling = false;

    private (bool, EdgeCollider2D) _cableDisableState = (false, null);

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

        _cablesList = new List<Rope>();
        _collidersSet = false;
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

        if (_cablesSet == true && _collidersSet == false)
        {
            _collidersSet = true;
            foreach (Rope c in _cablesList)
            {
                if (c.GetComponent<LineRenderer>().positionCount <= 0)
                {
                    _collidersSet = false;
                    break;
                }
                c.GetComponent<EdgeCollider2D>().SetPoints(MakeCollider(c));
            }
        }

        if (_cablesSet && _collidersSet && isFastFalling)
        {
            foreach (Rope c in _cablesList)
            {
                EdgeCollider2D cableCol = c.GetComponent<EdgeCollider2D>();
                if (_cableDisableState.Item1 && cableCol != _cableDisableState.Item2)
                {
                    cableCol.isTrigger = true;
                }
                else
                {
                    // Only make it solid if we are actually intending to use it (isFastFalling)
                    // AND it's the one we are not disabling.
                    cableCol.isTrigger = false;
                }
            }
        }
        else
        {
            foreach (Rope c in _cablesList)
            {
                c.GetComponent<EdgeCollider2D>().isTrigger = true;
            }
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
                    Rope newCable = BuildCable(startPoint, endPoint);
                    //bool crosses = false;
                    //for (int i = 0; i < newCable.linePoints; i++)
                    //{
                    //    Vector3 point = newCable.GetComponent<LineRenderer>().GetPosition(i);
                    //    crosses = false;
                    //    foreach (Rope c in _cablesList)
                    //    {
                    //        for (int j = 0; j < c.linePoints; j++)
                    //        {
                    //            Vector3 otherPoint = c.GetComponent<LineRenderer>().GetPosition(j);
                    //            if (Vector3.Distance(point, otherPoint) < 1f)
                    //            {
                    //                crosses = true;
                    //                break;
                    //            }
                    //        }
                    //        if (crosses) break;
                    //    }
                    //    if (crosses) break;
                    //}
                    //if (crosses)
                    //{
                    //    Destroy(newCable.gameObject); //destroy the new cable after checking if it crosses any existing cables
                    //    continue;
                    //}//if the new cable crosses any existing cables, skip it and try the next endpoint
                    _cablesList.Add(newCable);
                    endPoint.hasBeenUsed = true;
                    break;
                }
            }
        }
    }

    private Rope BuildCable(CableRight start, CableLeft end)
    {
        Rope newCable = Instantiate(_cablePrefab, transform);
        newCable.SetStartPoint(start.transform);
        newCable.SetEndPoint(end.transform);
        newCable.ropeLength = Vector3.Distance(start.transform.position, end.transform.position) + 10f;

        return newCable;
    }

    private List<Vector2> MakeCollider(Rope thisCable)
    {
        List<Vector2> positions = new List<Vector2>();
        for (int i = 0; i <= thisCable.linePoints; i++)
        {
            positions.Add(thisCable.GetComponent<LineRenderer>().GetPosition(i));
        }

        return positions;
    }

    public void DisableOtherCables(bool truth, EdgeCollider2D cableCollider)
    {
        _cableDisableState = (truth, cableCollider);
    }
}

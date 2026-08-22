using UnityEngine;

public class FloorAssetPicker : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _base;

    [SerializeField] private Sprite[] _baseSprites;

    /// <summary>
    /// The authoritative background SpriteRenderer for this floor/roof/ground piece.
    /// StructureBuilder uses this to get the correct sorting order for Z-depth calculation.
    /// </summary>
    public SpriteRenderer Base => _base;

    private void Start()
    {
        RandomizeBaseSprite();
    }

    public void RandomizeBaseSprite()
    {
        if (_baseSprites == null || _baseSprites.Length == 0) return;
        if (_base == null) _base = GetComponentInChildren<SpriteRenderer>();
        if (_base != null)
        {
            _base.sprite = _baseSprites[Random.Range(0, _baseSprites.Length)];
        }
    }
}

using UnityEngine;

public class FloorAssetPicker : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _base;

    [SerializeField] private Sprite[] _baseSprites;

    private void Start()
    {
        RandomizeBaseSprite();
    }

    private void RandomizeBaseSprite()
    {
        if (_baseSprites.Length == 0) return;
        _base.sprite = _baseSprites[Random.Range(0, _baseSprites.Length)];
    }
}

using UnityEngine;
using Utils;

public class Barriers : MonoBehaviour, IWarningIndicator
{
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag(TagUtils.PLAYERKICK_TAG))
        {
            if (showDebugInfo) GameLogger.Log("BARRIER", "Barrier hit by PlayerKick trigger, destroying barrier.");
            Destroy(transform.parent.gameObject);
        }
    }
    
    public virtual Sprite GetWarningSprite()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            return spriteRenderer.sprite;
        }
        return null;
    }
    
    public virtual Vector3 GetPosition()
    {
        return transform.position;
    }
    
    public virtual bool ShouldShowWarning()
    {
        return true;
    }
}

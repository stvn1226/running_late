using UnityEngine;

public interface IWarningIndicator
{
    Sprite GetWarningSprite();
    Vector3 GetPosition();
    bool ShouldShowWarning();
}

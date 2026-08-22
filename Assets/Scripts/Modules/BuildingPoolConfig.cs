using UnityEngine;

[CreateAssetMenu(fileName = "BuildingPoolConfig", menuName = "Runner/Building Pool Config")]
public class BuildingPoolConfig : ScriptableObject
{
    [Header("Anti-Repeat Rules")]
    [Tooltip("Si true, dos edificios adyacentes no pueden tener el mismo themeId.")]
    public bool avoidAdjacentRepeat = true;

    [Tooltip("Si true, el anti-repeat aplica también al primer edificio del módulo siguiente vs el último del módulo anterior.")]
    public bool avoidRepeatAcrossModules = true;
}

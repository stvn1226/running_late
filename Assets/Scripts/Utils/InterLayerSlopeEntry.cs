using UnityEngine;

/// <summary>
/// Define un prefab de Inter-Layer Slope y cuántas unidades de altura del diseñador cubre.
///
/// Uso en LevelStarter (Inspector):
///   - Slot 0: prefab = SlopeSize1,  designerHeightUnits = 1
///   - Slot 1: prefab = SlopeSize2,  designerHeightUnits = 2
///
/// El generador los combina automáticamente (greedy: más grande primero)
/// para cubrir exactamente la altura de la vereda en cada punto.
///
/// Ejemplo: vereda en altura 3 → usa slope×2 + slope×1.
/// </summary>
[System.Serializable]
public class InterLayerSlopeEntry
{
    [Tooltip("Prefab del slope a instanciar.")]
    public GameObject prefab;

    [Tooltip("Cuántas unidades de altura del diseñador cubre este slope.\n" +
             "Ej: un slope que sube 1 nivel → 1. Un slope que sube 2 niveles → 2.")]
    [Min(1)]
    public int designerHeightUnits = 1;
}

using UnityEngine;

[CreateAssetMenu(fileName = "ModulePlan", menuName = "Scriptable Objects/ModulePlan")]
public class ModulePlan : ScriptableObject
{
    [Header("Base Terrain")]
    public int minTerrainHeight = 1;
    public int maxTerrainHeight = 5;
    public int currentTerrainWidth = 10;

    [Tooltip("Cuántas unidades de Unity equivale 1 bloque de altura.\n" +
             "Este valor afecta TODO: terreno, obstáculos voladores y la escala visual.\n" +
             "Coordinarlo con 'blocksPerDesignerUnit' en LevelStarter.")]
    public int groundBlockHeight = 3;

    [Header("Building Generation")]
    [Range(1, 10)]
    public int buildingsInModule = 2;
    [Range(0, 5)]
    public int extraRandomBuildings = 1;
}
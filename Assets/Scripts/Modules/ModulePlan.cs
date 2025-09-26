using UnityEngine;

[CreateAssetMenu(fileName = "ModulePlan", menuName = "Scriptable Objects/ModulePlan")]
public class ModulePlan : ScriptableObject
{
    [Header("Base Terrain")]
    public int minTerrainHeight = 1;
    public int maxTerrainHeight = 5;
    public int currentTerrainWidth = 10;
    public int groundBlockHeight = 3;

    [Header("Building Generation")]
    [Range(1, 10)]
    public int buildingsInModule = 2;
    [Range(0, 5)]
    public int extraRandomBuildings = 1;
}
using UnityEngine;

public enum GameDifficulty { Easy, Medium, Hard, Extreme }
public enum TargetPlatform { PC_Console, Mobile, Web }

[CreateAssetMenu(fileName = "DifficultyConfig", menuName = "Game/Difficulty Config")]
public class DifficultyConfig : ScriptableObject
{
    [Header("Profile Info")]
    public GameDifficulty difficulty = GameDifficulty.Medium;
    public TargetPlatform platform = TargetPlatform.PC_Console;

    [Header("Global Spawn Rates")]
    [Range(0, 100)] 
    [Tooltip("Percentage chance to spawn a ground obstacle if the module allows it.")]
    public int groundObstacleChance = 40;
    
    [Range(0, 100)] 
    [Tooltip("Percentage chance to spawn a flying obstacle if the module allows it.")]
    public int flyingObstacleChance = 20;
}

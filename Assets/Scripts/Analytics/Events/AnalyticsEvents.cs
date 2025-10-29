using System.Collections.Generic;

public static class AnalyticsEvents
{
    public const string LEVEL_START = "level_start";
    public const string LEVEL_COMPLETE = "level_complete";
    public const string LEVEL_RETRY = "level_retry";
    public const string PLAYER_DEATH = "player_death";
    public const string PLAYER_JUMP = "player_jump";
    public const string PLAYER_SLIDE = "player_slide";
    public const string PLAYER_WALLRUN = "player_wallrun";
    public const string PLAYER_KICK = "player_kick";
    public const string PLAYER_STOMP = "player_stomp";
    public const string OBSTACLE_HIT = "obstacle_hit";
    public const string UI_PAUSE = "ui_pause";
    public const string UI_RESUME = "ui_resume";
    
    public static Dictionary<string, object> CreateLevelContext(int seed, string levelType, int width)
    {
        return new Dictionary<string, object>
        {
            { "seed", seed },
            { "level_type", levelType },
            { "level_width", width }
        };
    }
    
    public static Dictionary<string, object> CreatePlayerActionContext(string actionName, float positionX, float velocityX)
    {
        return new Dictionary<string, object>
        {
            { "action", actionName },
            { "position_x", positionX },
            { "velocity_x", velocityX }
        };
    }
}
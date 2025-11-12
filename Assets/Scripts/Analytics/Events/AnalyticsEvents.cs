using System.Collections.Generic;
using Unity.Services.Analytics;

public static class AnalyticsEvents
{
    public const string LEVEL_START = "level_start";
    public const string LEVEL_COMPLETE = "level_complete";
    public const string LEVEL_DEFEAT = "level_defeat";
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

public class LevelStartEvent : Unity.Services.Analytics.Event
{
    public LevelStartEvent() : base("level_start") { }
    
    public int Seed { set { SetParameter("seed", value); } }
    public string LevelType { set { SetParameter("level_type", value); } }
    public int LevelWidth { set { SetParameter("level_width", value); } }
    public int ModuleCount { set { SetParameter("module_count", value); } }
}

public class LevelCompleteEvent : Unity.Services.Analytics.Event
{
    public LevelCompleteEvent() : base("level_complete") { }
    
    public int Seed { set { SetParameter("seed", value); } }
    public float CompletionTime { set { SetParameter("completion_time", value); } }
    public bool IsPersonalBest { set { SetParameter("is_personal_best", value); } }
    public string LevelType { set { SetParameter("level_type", value); } }
}

public class LevelDefeatEvent : Unity.Services.Analytics.Event
{
    public LevelDefeatEvent() : base("level_defeat") { }
    
    public int Seed { set { SetParameter("seed", value); } }
    public float SurvivalTime { set { SetParameter("survival_time", value); } }
    public string DefeatReason { set { SetParameter("defeat_reason", value); } }
}

public class PlayerActionEvent : Unity.Services.Analytics.Event
{
    public PlayerActionEvent() : base("player_action") { }
    
    public string ActionType { set { SetParameter("action_type", value); } }
    public float PositionX { set { SetParameter("position_x", value); } }
    public float VelocityX { set { SetParameter("velocity_x", value); } }
    public bool IsGrounded { set { SetParameter("is_grounded", value); } }
}

public class ObstacleHitEvent : Unity.Services.Analytics.Event
{
    public ObstacleHitEvent() : base("obstacle_hit") { }
    
    public string ObstacleType { set { SetParameter("obstacle_type", value); } }
    public float PositionX { set { SetParameter("position_x", value); } }
    public float PlayerSpeed { set { SetParameter("player_speed", value); } }
}

public class UIInteractionEvent : Unity.Services.Analytics.Event
{
    public UIInteractionEvent() : base("ui_interaction") { }
    
    public string InteractionType { set { SetParameter("interaction_type", value); } }
    public string PanelName { set { SetParameter("panel_name", value); } }
}

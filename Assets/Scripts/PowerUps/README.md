# Power-Up System

This folder contains the power-up system for the game. The system uses an inheritance-based approach where all power-ups inherit from a base `PowerUp` class.

## Files

- **PowerUp.cs** - Abstract base class for all power-ups
- **ITimedPowerUp.cs** - Interface for power-ups with duration and UI display support
- **ExtraTime.cs** - Power-up that adds extra time to the level timer
- **DoubleJump.cs** - Power-up that grants double jump ability for a limited time
- **ScoreBoost.cs** - Power-up that adds bonus score points
- **PowerUpTemplate.cs.txt** - Template for creating new power-ups
- **TimedPowerUpTemplate.cs.txt** - Template for creating timed power-ups with UI displays

## Creating Timed Power-Ups with UI

For power-ups that have a duration and need a UI display:

1. Implement the `ITimedPowerUp` interface on the component that manages the power-up state
2. Add the required methods: `IsActive()`, `GetRemainingTime()`, `GetMaxTime()`, `GetPowerUpName()`
3. Create a UI display using the generic `PowerUpDisplay` component (found in `/Assets/Scripts/UI/`)
4. See `TimedPowerUpTemplate.cs.txt` for a complete example

## How to Use the ExtraTime Power-Up

1. **Add the ExtraTime script** to your ExtraTime prefab
2. **Configure the settings** in the Inspector:
   - **Time To Add**: Amount of seconds to add (default: 10)
   - **Destroy On Pickup**: Whether the power-up disappears after pickup (default: true)
   - **Show Debug Info**: Enable logging for debugging (default: true)
   - **Pickup Sound**: Optional audio clip to play when picked up
   - **Pickup Effect**: Optional particle effect or GameObject to spawn on pickup
   - **Effect Duration**: How long the pickup effect lasts (default: 1 second)

3. **Set up the collider**:
   - Make sure the Collider child GameObject has **Is Trigger** enabled
   - The collider should be on the same GameObject as the ExtraTime script OR on a child

4. **Player tag**: Ensure your player GameObject has the "Player" tag

## How to Create New Power-Ups

1. **Create a new script** in `/Assets/Scripts/PowerUps/`
2. **Inherit from PowerUp**: `public class MyPowerUp : PowerUp`
3. **Override ApplyEffect**: Implement your power-up logic
4. **Add custom settings**: Use `[Header]` and `[SerializeField]` for Inspector fields

### Example: Speed Boost Power-Up

```csharp
using UnityEngine;

public class SpeedBoost : PowerUp
{
    [Header("Speed Boost Settings")]
    [SerializeField] private float speedMultiplier = 2f;
    [SerializeField] private float duration = 5f;

    protected override void ApplyEffect(GameObject player)
    {
        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        if (movement != null)
        {
            StartCoroutine(ApplySpeedBoost(movement));
        }
    }

    private System.Collections.IEnumerator ApplySpeedBoost(PlayerMovement movement)
    {
        // Apply boost
        float originalSpeed = movement.maxSpeed;
        movement.maxSpeed *= speedMultiplier;
        
        GameLogger.Log("POWERUP", $"Speed boosted to {movement.maxSpeed}");
        
        yield return new WaitForSeconds(duration);
        
        // Restore original speed
        movement.maxSpeed = originalSpeed;
        GameLogger.Log("POWERUP", "Speed boost ended");
    }
}
```

## Architecture

The `PowerUp` base class handles:
- Collision detection with the player
- Preventing multiple pickups
- Playing pickup sounds
- Spawning visual effects
- Destroying or deactivating the GameObject

Each specific power-up only needs to implement `ApplyEffect(GameObject player)` to define what happens when picked up.

## Integration with Other Systems

### Defeat/Timer System
- The `Defeat.cs` script has been updated with public methods:
  - `AddTime(float seconds)` - Adds time to the level
  - `GetTimeRemaining()` - Returns remaining time
  - `GetTimeLimit()` - Returns total time limit

### Player
- Power-ups detect the player using the "Player" tag
- The player GameObject is passed to `ApplyEffect()` for component access

## Tips

- Use `GameLogger.Log("POWERUP", message)` for consistent logging
- Set **Is Trigger** to true on colliders for power-ups
- Use coroutines for timed effects
- Consider using `FindFirstObjectByType<T>()` for singleton managers
- Add [Header] attributes to organize Inspector fields

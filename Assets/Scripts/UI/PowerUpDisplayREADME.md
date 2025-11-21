# PowerUpDisplay System

The `PowerUpDisplay` component provides a generic, reusable UI system for displaying timed power-ups.

## Features

- **Generic**: Works with any component implementing `ITimedPowerUp`
- **Automatic fade in/out**: Smoothly appears when active, disappears when inactive
- **Fill bar visualization**: Shows remaining time as a radial or linear fill
- **Auto-find player**: Optionally finds the player GameObject automatically

## Setup Instructions

### 1. Implement ITimedPowerUp

Your power-up manager component (e.g., `PlayerJump`, `PlayerMovement`) must implement the `ITimedPowerUp` interface:

```csharp
public class PlayerJump : MonoBehaviour, ITimedPowerUp
{
    private bool allowDoubleJump = false;
    private float currentTime = 0f;
    private float maxTime = 10f;

    public bool IsActive() => allowDoubleJump;
    public float GetRemainingTime() => allowDoubleJump ? (maxTime - currentTime) : 0f;
    public float GetMaxTime() => maxTime;
    public string GetPowerUpName() => "DoubleJump";
}
```

### 2. Create UI Hierarchy

Create a UI structure like:
```
Canvas
└── PowerUpDisplay (parent GameObject)
    └── Fill (child Image)
```

### 3. Configure Components

**On the parent GameObject:**
- Add `PowerUpDisplay` component
- Add `Image` component (optional background)
- `CanvasGroup` will be auto-created if needed

**On the child GameObject:**
- Add `Image` component
- Set Image Type to **Filled**
- Set Fill Method (e.g., Radial 360 or Horizontal)

### 4. Assign References

**PowerUpDisplay settings:**
- **Power Up Source**: Drag the component implementing ITimedPowerUp (e.g., PlayerJump)
- **Fill Image**: Drag the child Image component
- **Canvas Group**: Leave empty to auto-create
- **Hide When Inactive**: Check to hide when power-up is not active
- **Fade Speed**: Adjust fade transition speed (default: 5)
- **Auto Find Player**: Check to automatically find player by tag

## Example Use Cases

### DoubleJump Display
- Shows when double jump is available
- Fill empties as time runs out
- Fades out when expired

### Speed Boost Display
- Appears when speed boost is active
- Displays remaining boost duration
- Can be positioned anywhere on screen

### Shield Display
- Shows active shield duration
- Different color/icon from other power-ups
- Uses same generic component

## Tips

- Use different colors for different power-ups
- Position multiple displays in a consistent location
- Add icons or sprites to the background Image for clarity
- Adjust fade speed for dramatic or subtle transitions

# Start Countdown Setup Guide

## Overview
This system adds a 3-2-1 countdown before the player can start moving. The level timer waits until the countdown finishes before starting.

## Components Created
1. **StartCountdownController.cs** - Manages the 3-2-1 GO countdown display
2. Modified **SpawnManager.cs** - Triggers countdown after player spawn
3. Modified **PlayerControllerModular.cs** - Disables input during countdown
4. Modified **Timer.cs** - Waits for countdown before starting the level timer

## Setup Instructions

### 1. Create Countdown UI
You need to create a UI panel for the start countdown (separate from the existing timer UI):

1. In the Canvas, create a new GameObject called `StartCountdownPanel`
2. Add a TextMeshProUGUI component as a child called `CountdownText`
3. Style the text (large, bold, centered)
4. Optionally add background images for visual appeal

### 2. Add StartCountdownController Component
1. Create an empty GameObject in the scene called `StartCountdownManager`
2. Add the `StartCountdownController` component to it
3. Assign references in the Inspector:
   - **Countdown Panel**: Your StartCountdownPanel GameObject
   - **Countdown Text**: The CountdownText TextMeshProUGUI component
   - **Countdown From**: 3 (default)
   - **Countdown Interval**: 1.0 (seconds between numbers)
   - **Go Text**: "GO!" (or customize)
   - **Use Scale Animation**: true (for animated text)
   - **Play Countdown Sounds**: true (uses existing audio system)

### 3. Configure SpawnManager
The SpawnManager is already updated with:
- **Use Countdown**: true (enable/disable countdown)
- **Delay Before Countdown**: 0.2 seconds (small delay after spawn)

You can toggle `useCountdown` to false if you want to disable the feature.

### 4. Configure Timer
The Timer component now has:
- **Wait For Start Countdown**: true (waits for 3-2-1 countdown to finish)

Set this to false on any timer that shouldn't wait for the countdown.

## How It Works

1. **Level generates** → LevelStarter completes generation
2. **Player spawns** → SpawnManager spawns player at home
3. **Input disabled** → Player cannot move yet
4. **Countdown starts** → Shows 3, 2, 1, GO!
5. **Countdown completes** → Player input enabled, timer starts
6. **Player can move** → Game begins!

## Customization Options

### StartCountdownController Settings
- **Countdown From**: Change starting number (default: 3)
- **Countdown Interval**: Time between numbers in seconds
- **Go Text**: Customize the final message
- **Go Display Duration**: How long "GO!" shows
- **Use Scale Animation**: Enable/disable text animation
- **Scale Animation Duration**: Animation speed
- **Play Countdown Sounds**: Use audio feedback

### Visual Customization
Style your countdown UI with:
- Large text (recommended: 100-200 font size)
- Bold font weight
- Bright colors for visibility
- Optional background panel with transparency
- Optional circle or countdown visual elements

## Testing
1. Enter Play mode
2. Level should generate and load
3. Player spawns but cannot move
4. Countdown displays: 3... 2... 1... GO!
5. Player can now move
6. Timer starts counting

## Troubleshooting

**Countdown doesn't appear:**
- Check StartCountdownController is in the scene
- Verify UI references are assigned
- Check countdownPanel is active in hierarchy

**Player can move during countdown:**
- Verify `useCountdown` is true in SpawnManager
- Check StartCountdownController.Instance is not null

**Timer starts too early:**
- Check `waitForStartCountdown` is true on Timer component
- Verify StartCountdownController is initialized before Timer

**No sounds during countdown:**
- Set `playCountdownSounds` to true
- Ensure AudioManager is in the scene
- Check that Jump and PowerUp sounds are assigned

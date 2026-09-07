# Project Optimization Tools Guide

This document describes the editor tools available under `Tools > Optimization` designed to optimize rendering performance, asset memory footprint, and clean up unreferenced project clutter in *Running Late*.

---

## Summary of Optimization Tools

| Tool | Menu Path | Primary Purpose | Impact |
| :--- | :--- | :--- | :--- |
| **Sprite Atlas Auto Creator** | `Tools > Optimization > Sprite Atlas Auto Creator` | Packages loose sprites into single texture atlases. | **Massive Draw Call reduction** (from 600+ to ~30-50). |
| **Quick Generate Atlases** | `Tools > Optimization > Generate Running Late Atlases` | 1-Click automatic generator for all game atlases. | Instant batch generation with safe 2D defaults. |
| **Unused Asset Finder** | `Tools > Optimization > Unused Asset Finder` | Identifies unreferenced assets and moves them to Quarantine. | Cleans up disk space and build size safely without breaking references. |
| **Atlas Optimizer** | `Tools > Optimization > Atlas Optimizer` | Applies platform compression settings to existing `.spriteatlas` files. | Optimizes texture memory and build size for PC/Mobile. |
| **Texture Optimizer** | `Tools > Optimization > Texture Optimizer` | Bulk configures loose texture resolutions and compression. | Reduces VRAM usage and loading times. |
| **Audio Optimizer** | `Tools > Optimization > Audio Optimizer` | Bulk configures audio clip compression, load types, and sample rates. | Frees up RAM by streaming long music tracks and pre-loading SFX. |

---

## 1. Sprite Atlas Auto Creator

### Why it matters
In 2D games, rendering separate `.png` files forces Unity to issue an individual Draw Call for every single sprite. By packing them into Sprite Atlases:
- Unity combines dozens of sprites into a single 2048x2048 GPU texture sheet.
- All environment modules, buildings, sidewalks, and obstacles that share a material will batch into single draw calls.

### Target Atlas Groups
- **`Atlas_Environment`**: `Assets/Sprites/Environment` (Buildings, sidewalks, windows, props)
- **`Atlas_Obstacles`**: `Assets/Sprites/Obstacles` (Vehicles, dogs, pigeons, barriers)
- **`Atlas_Pickups`**: `Assets/Sprites/Pickups` (Toast, shields, pickups)
- **`Atlas_Player`**: `Assets/Sprites/Player` (Player character spritesheets)
- **`Atlas_PlayerExpressions`**: `Assets/Sprites/PlayerExpressions` (Facial animations/emotes)
- **`Atlas_KenneyInput`**: `Assets/Sprites/Kenney_Input` (HUD Gamepad and Keyboard icons)
- **`Atlas_Tiles`**: `Assets/tiles` (Modular level ground/platform tiles)

### Recommended Settings for 2D
- **Output Path**: `Assets/Atlases`
- **Tight Packing**: `True`
- **Allow Rotation**: `False` *(Must remain false to prevent pixel distortion and edge bleeding on 2D tiles)*
- **Padding**: `4px`
- **Max Texture Size**: `2048`

---

## 2. Unused Asset Finder & Quarantine Tool

### Why it matters
Over time, messy folder structures and abandoned art assets clutter the project and increase build sizes. Deleting files directly can break scenes and prefabs. The **Unused Asset Finder** resolves this safely via a two-step quarantine process.

### How it works
1. **Dependency Analysis**: Scans all `.unity` scenes, all prefabs under `Assets/Prefabs/`, all ScriptableObject configs under `Assets/Configs/`, and `Assets/Resources/`.
2. **Recursive Traversal**: Collects every texture, material, audio, and shader asset actually referenced in the game.
3. **Unused Detection**: Scans folders like `Assets/Sprites`, `Assets/tiles`, `Assets/Art`, `Assets/Materials`, and finds files missing from the active reference graph.
4. **Quarantine (Zero-Risk)**:
   - Clicking **`Move Selected to Quarantine Folder`** moves the unreferenced files to `Assets/_Quarantine_Unused/` preserving all `.meta` files and GUIDs.
   - Run playtests in `ModularLevel`. If an asset was missing, click **`Restore All from Quarantine`** to immediately restore it.
   - Once verified, delete `Assets/_Quarantine_Unused/` safely.

---

## 3. Audio Optimizer Tool

### Recommended Audio Configurations
- **Short Sound Effects (Jumps, Kicks, UI clicks)**:
  - *Load Type*: `Decompress On Load`
  - *Compression*: `ADPCM` or `PCM`
  - *Sample Rate*: `Optimize Sample Rate`
- **Ambient & Obstacle Sounds (Engines, barking, pigeons)**:
  - *Load Type*: `Compressed In Memory`
  - *Compression*: `Vorbis` (Quality ~0.7)
- **Music Tracks (Background Music, Title Theme)**:
  - *Load Type*: `Streaming` *(Prevents allocating entire music track into RAM)*
  - *Compression*: `Vorbis` (Quality ~0.7)
  - *Force To Mono*: Optional (saves 50% memory if spatial audio is not stereo-dependent)

---

## 4. Texture Optimizer Tool

### Recommended Texture Configurations
- **Sprites / 2D Assets**:
  - *Generate Mip Maps*: `False` *(2D sprites viewed with orthographic/fixed-distance cameras waste 33% extra VRAM if MipMaps are generated)*
  - *Max Size*: `2048` (or `1024` for smaller props/icons)
  - *Compression*: `Normal Quality` (DXT5 on PC, ASTC 6x6 on Mobile)

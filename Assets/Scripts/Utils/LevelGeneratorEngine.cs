using UnityEngine;
using System.Collections.Generic;

// --- DATA STRUCTURES (BLUEPRINTS) ---
// These classes store abstract level information before instantiation.

[System.Serializable]
public class SegmentData
{
    public ModuleBuilder Template; // Prefab to instantiate
    public float X_Position;       // World position
    public int Y_Elevation;        // Logical height
    public float Width;            // Visual width
    public bool IsSlope;           // Is a ramp?
    public string Label;           // Etiqueta para debugging (ej: "Road_Segment", "Sidewalk_Home")
    public bool SpawnObstacles;    // Obstacle control
    public int MaxObstacleLevel;   // Max obstacle level allowed (1=pedestrian/moto, 2=car, 3=truck)

    // --- Phase 2: Dual-Layer Height Extension ---
    // All default to 0. Ignored by rendering when useRelativeSidewalkHeights = false.
    public int RoadElevationAtX;       // Road Y_Elevation at this segment's X (Sidewalk segments)
    public int SidewalkRelativeLevel;  // Sidewalk level relative to road: 0=flush, 1=+1.5u, 2=+3.0u
    public int ConnectorRoadElevation; // Road elevation at connector X (DiagonalRamps only)
    public int ConnectorRelLevelFrom;  // Sidewalk relative level at ramp start (DiagonalRamps only)
    public int ConnectorRelLevelTo;    // Sidewalk relative level at ramp end   (DiagonalRamps only)
}

[System.Serializable]
public class TrackBlueprint
{
    public List<SegmentData> Segments = new List<SegmentData>();
    
    // Returns where this track exactly ends
    public float GetEndPosition()
    {
        if (Segments.Count == 0) return 0f;
        var last = Segments[Segments.Count - 1];
        return last.X_Position + last.Width;
    }
}

[System.Serializable]
public class LevelBlueprint
{
    public SegmentData HouseAnchor;
    public SegmentData SchoolAnchor;
    
    public TrackBlueprint RoadTrack = new TrackBlueprint();
    public TrackBlueprint SidewalkTrack = new TrackBlueprint();
    public List<SegmentData> DiagonalRamps = new List<SegmentData>(); // Ramps connecting Road and Sidewalk (Z-axis)
    
    public float TotalLength;
}

// --- GENERATOR ENGINE ---
// Fill and return the LevelBlueprint without using Instantiate()
public class LevelGeneratorEngine
{
    public static LevelBlueprint GenerateBlueprint(
        ModuleBuilder houseModule, 
        ModuleBuilder schoolModule, 
        ModuleBuilder[] roadModules, 
        ModuleBuilder[] sidewalkModules,
        ModuleBuilder slopeLeft,
        ModuleBuilder slopeRight,
        ModuleBuilder roadSlopeLeft,
        ModuleBuilder roadSlopeRight,
        ModuleBuilder interlayerSlopeLeft,
        ModuleBuilder interlayerSlopeRight,
        int targetLengthUnits,
        int safeZoneModules,
        int roadMaxHeight,
        int sidewalkMaxRelHeight,
        int seed,
        bool logDualLayer = false)
    {
        // Initialize seed for deterministic generation
        Random.InitState(seed);
        
        LevelBlueprint blueprint = new LevelBlueprint();

        // Pass 1: Anchors (House & School)
        Pass1_SetAnchors(blueprint, houseModule, schoolModule, targetLengthUnits);

        // Pass 2: Road with Elevations
        Pass2_GenerateRoad(blueprint, roadModules, roadSlopeLeft, roadSlopeRight, safeZoneModules, roadMaxHeight);

        // Pass 3: Sidewalk (synced to road) + Interlayer connection ramps
        Pass3_GenerateSidewalk(blueprint, sidewalkModules, slopeLeft, slopeRight, interlayerSlopeLeft, interlayerSlopeRight, safeZoneModules, sidewalkMaxRelHeight, logDualLayer);

        // Pass 4: Finalize Obstacles
        Pass4_FinalizeObstacles(blueprint);

        return blueprint;
    }

    private static void Pass1_SetAnchors(LevelBlueprint blueprint, ModuleBuilder houseModule, ModuleBuilder schoolModule, int targetLengthUnits)
    {
        float houseWidth = houseModule.moduleConfig.currentTerrainWidth;
        // Offset start position for correct camera framing
        float startX = -houseWidth / 2f;
        
        blueprint.HouseAnchor = new SegmentData {
            Template = houseModule,
            X_Position = startX,
            Y_Elevation = 0,
            Width = houseWidth,
            Label = "Road_Home",
            SpawnObstacles = false
        };

        // Estimate school position
        float schoolX = targetLengthUnits * 25f; 
        blueprint.SchoolAnchor = new SegmentData {
            Template = schoolModule,
            X_Position = schoolX,
            Y_Elevation = 0,
            Width = schoolModule.moduleConfig.currentTerrainWidth,
            Label = "Road_School",
            SpawnObstacles = false
        };
        
        blueprint.TotalLength = schoolX + schoolModule.moduleConfig.currentTerrainWidth;
    }

    private static void Pass2_GenerateRoad(LevelBlueprint blueprint, ModuleBuilder[] roadModules, ModuleBuilder slopeUp, ModuleBuilder slopeDown, int safeZoneModules, int roadMaxHeight)
    {
        float currentX = blueprint.HouseAnchor.X_Position + blueprint.HouseAnchor.Width;
        float targetX = blueprint.SchoolAnchor.X_Position;
        int segmentsCreated = 0;
        int currentH = 0;
        int slopeCooldown = 0; // Flat segments required before next slope allowed

        while (currentX < targetX)
        {
            float remaining = targetX - currentX;
            if (remaining <= 0) break;

            int nextH = currentH;

            // Prevent any random slopes in the final descent zone to guarantee reaching 0
            bool inEndZone = remaining < (35f + roadMaxHeight * 60f);

            if (inEndZone)
            {
                if (currentH > 0 && slopeCooldown == 0)
                {
                    nextH = currentH - 1;
                }
            }
            else if (slopeCooldown == 0 && segmentsCreated > safeZoneModules && Random.value < 0.25f)
            {
                nextH = Mathf.Clamp(currentH + (Random.value > 0.5f ? 1 : -1), 0, roadMaxHeight);
            }

            if (nextH != currentH)
            {
                ModuleBuilder sTemplate = (nextH > currentH) ? slopeUp : slopeDown;
                if (sTemplate != null)
                {
                    float sw = sTemplate.moduleConfig.currentTerrainWidth;
                    blueprint.RoadTrack.Segments.Add(new SegmentData {
                        Template    = sTemplate,
                        X_Position  = currentX,
                        Y_Elevation = (nextH > currentH) ? nextH : currentH,
                        Width       = sw,
                        Label       = (nextH > currentH) ? "Road_Slope_Up" : "Road_Slope_Down",
                        IsSlope     = true
                    });
                    currentX += sw;
                    slopeCooldown = 2;
                }
                currentH = nextH;
            }
            else
            {
                ModuleBuilder bestFit = GetBestFit(roadModules, remaining);
                if (bestFit == null) break;

                float pieceW = bestFit.moduleConfig.currentTerrainWidth;
                blueprint.RoadTrack.Segments.Add(new SegmentData {
                    Template         = bestFit,
                    X_Position       = currentX,
                    Y_Elevation      = currentH,
                    Width            = pieceW,
                    Label            = "Road_Segment",
                    SpawnObstacles   = (segmentsCreated >= safeZoneModules),
                    MaxObstacleLevel = 3
                });
                currentX += pieceW;
                segmentsCreated++;
                if (slopeCooldown > 0) slopeCooldown--;
            }
        }
        blueprint.SchoolAnchor.X_Position = currentX;
        blueprint.TotalLength = currentX + blueprint.SchoolAnchor.Width;
    }

    private static void Pass3_GenerateSidewalk(LevelBlueprint blueprint, ModuleBuilder[] sidewalkModules, ModuleBuilder slopeUp, ModuleBuilder slopeDown, ModuleBuilder diagUp, ModuleBuilder diagDown, int safeZoneModules, int sidewalkMaxRelHeight, bool logDualLayer = false)
    {
        if (sidewalkModules == null || sidewalkModules.Length == 0) return;

        float singleSpace = sidewalkModules[0].moduleConfig.currentTerrainWidth;
        float startRampWidth = diagUp != null ? diagUp.moduleConfig.currentTerrainWidth : singleSpace;
        float endRampWidth = diagDown != null ? diagDown.moduleConfig.currentTerrainWidth : singleSpace;

        float targetStartRampX = blueprint.HouseAnchor.X_Position + blueprint.HouseAnchor.Width + singleSpace;
        float targetEndRampX = blueprint.SchoolAnchor.X_Position - singleSpace - endRampWidth;

        // Ensure the end ramp is placed perfectly on top of a FLAT road segment of matching width.
        // Otherwise, it might be placed on top of a road slope, creating a visual gap and height mismatch.
        if (blueprint.RoadTrack != null && blueprint.RoadTrack.Segments != null)
        {
            for (int i = blueprint.RoadTrack.Segments.Count - 1; i >= 0; i--)
            {
                var rSeg = blueprint.RoadTrack.Segments[i];
                if (rSeg.X_Position <= targetEndRampX && Mathf.Abs(rSeg.Width - endRampWidth) < 0.1f && !rSeg.IsSlope) {
                    targetEndRampX = rSeg.X_Position;
                    break;
                }
            }
        }

        float startRampX = targetStartRampX;
        
        if (blueprint.RoadTrack != null && blueprint.RoadTrack.Segments != null)
        {
            foreach (var rSeg in blueprint.RoadTrack.Segments)
            {
                if (rSeg.X_Position >= targetStartRampX && Mathf.Abs(rSeg.Width - startRampWidth) < 0.1f && !rSeg.IsSlope) {
                    startRampX = rSeg.X_Position;
                    break;
                }
            }
        }

        float sidewalkStartX = startRampX + startRampWidth;

        int roadElevAtStart = GetRoadElevationAt(blueprint, startRampX);
        blueprint.DiagonalRamps.Add(new SegmentData {
            Template = diagUp,
            X_Position = startRampX,
            Y_Elevation = roadElevAtStart,
            Width = startRampWidth,
            Label = "Diagonal_Ramp_Start",
            IsSlope = false,
            SpawnObstacles = false,
            RoadElevationAtX = roadElevAtStart,
            SidewalkRelativeLevel = 0,
            ConnectorRoadElevation = roadElevAtStart
        });

        float currentX = sidewalkStartX;
        int currentH = GetRoadElevationAt(blueprint, currentX) + 1; 
        int slopeCooldown = 0;
        int segmentsCreated = 0;

        int targetRoadElev = GetRoadElevationAt(blueprint, targetEndRampX);

        while (currentX < targetEndRampX)
        {
            float remaining = targetEndRampX - currentX;
            if (remaining <= 0) break;

            int roadElev = GetRoadElevationAt(blueprint, currentX);
            int nextH = currentH;
            
            int targetH = targetRoadElev + 1;
            int diff = Mathf.Abs(currentH - targetH);
            float panicThreshold = diff * 15f; // Reserve 15 units of space per level we need to drop
            
            bool inEndZone = remaining < 100f;
            if (inEndZone)
            {
                if (currentH > targetH)
                {
                    if (slopeCooldown <= 0 || remaining <= panicThreshold) 
                        nextH = currentH - 1;
                }
                else if (currentH < targetH)
                {
                    if (slopeCooldown <= 0 || remaining <= panicThreshold) 
                        nextH = currentH + 1;
                }
            }
            else 
            {
                if (currentH > roadElev + sidewalkMaxRelHeight)
                    nextH = currentH - 1; 
                else if (currentH < roadElev + 1)
                    nextH = currentH + 1; 
                else if (slopeCooldown <= 0 && segmentsCreated > safeZoneModules && UnityEngine.Random.value < 0.25f)
                    nextH = UnityEngine.Mathf.Clamp(currentH + (UnityEngine.Random.value > 0.5f ? 1 : -1), roadElev + 1, roadElev + sidewalkMaxRelHeight);
            }

            ModuleBuilder bestFit = null;
            if (nextH == currentH)
            {
                bestFit = GetBestFitSidewalk(blueprint, sidewalkModules, remaining, currentX, currentH, 1);
                if (bestFit == null) 
                {
                    nextH = currentH + 1;
                }
            }

            if (nextH != currentH)
            {
                ModuleBuilder sTemplate = (nextH > currentH) ? slopeUp : slopeDown;
                if (sTemplate != null)
                {
                    float sw = sTemplate.moduleConfig.currentTerrainWidth;
                    blueprint.SidewalkTrack.Segments.Add(new SegmentData {
                        Template = sTemplate,
                        X_Position = currentX,
                        Y_Elevation = (nextH > currentH) ? nextH : currentH,
                        Width = sw,
                        Label = (nextH > currentH) ? "Sidewalk_Slope_Up" : "Sidewalk_Slope_Down",
                        IsSlope = true,
                        SidewalkRelativeLevel = nextH - roadElev
                    });
                    currentX += sw;
                    slopeCooldown = 2;
                }
                currentH = nextH;
            }
            else
            {
                float pieceW = bestFit.moduleConfig.currentTerrainWidth;
                blueprint.SidewalkTrack.Segments.Add(new SegmentData {
                    Template = bestFit,
                    X_Position = currentX,
                    Y_Elevation = currentH,
                    Width = pieceW,
                    Label = "Sidewalk_Segment",
                    SpawnObstacles = (segmentsCreated >= safeZoneModules),
                    MaxObstacleLevel = 3,
                    SidewalkRelativeLevel = currentH - roadElev
                });
                currentX += pieceW;
                segmentsCreated++;
                if (slopeCooldown > 0) slopeCooldown--;
            }
        }

        int roadElevAtEnd = GetRoadElevationAt(blueprint, currentX);
        blueprint.DiagonalRamps.Add(new SegmentData {
            Template = diagDown,
            X_Position = currentX,
            Y_Elevation = roadElevAtEnd,
            Width = endRampWidth,
            Label = "Diagonal_Ramp_End",
            IsSlope = false,
            SpawnObstacles = false,
            RoadElevationAtX = roadElevAtEnd,
            SidewalkRelativeLevel = 0,
            ConnectorRoadElevation = roadElevAtEnd
        });
    }

    private static int GetRoadElevationAt(LevelBlueprint blueprint, float x)
    {
        if (blueprint.RoadTrack != null && blueprint.RoadTrack.Segments != null)
        {
            foreach (var rSeg in blueprint.RoadTrack.Segments)
            {
                if (x >= rSeg.X_Position && x < rSeg.X_Position + rSeg.Width)
                {
                    return rSeg.Y_Elevation;
                }
            }
        }
        return 0;
    }

    private static void Pass4_FinalizeObstacles(LevelBlueprint blueprint)
    {
        foreach (var roadSeg in blueprint.RoadTrack.Segments)
        {
            if (!roadSeg.SpawnObstacles) continue;

            // Clearance check ultra-preciso
            int sideH = GetSidewalkHeightAt(blueprint, roadSeg.X_Position + 0.5f);
            int roadH = roadSeg.Y_Elevation;
            int clearance = sideH - roadH;

            // Si hay poco techo (clearance <= 1), solo peatones/motos. Si hay espacio, camiones.
            roadSeg.MaxObstacleLevel = (clearance <= 1) ? 1 : 3;
        }
    }

    private static int GetRoadHeightAt(LevelBlueprint blueprint, float x)
    {
        foreach (var seg in blueprint.RoadTrack.Segments)
        {
            if (x >= seg.X_Position && x < seg.X_Position + seg.Width) return seg.Y_Elevation;
        }
        return 0;
    }

    private static int GetSidewalkHeightAt(LevelBlueprint blueprint, float x)
    {
        foreach (var seg in blueprint.SidewalkTrack.Segments)
        {
            if (x >= seg.X_Position && x < seg.X_Position + seg.Width) return seg.Y_Elevation;
        }
        return 0;
    }

    private static int GetMaxRoadElevationInRange(LevelBlueprint blueprint, float startX, float endX)
    {
        int maxElev = -999;
        if (blueprint.RoadTrack != null && blueprint.RoadTrack.Segments != null)
        {
            foreach (var rSeg in blueprint.RoadTrack.Segments)
            {
                if (rSeg.X_Position < endX && rSeg.X_Position + rSeg.Width > startX)
                {
                    if (rSeg.Y_Elevation > maxElev) maxElev = rSeg.Y_Elevation;
                }
            }
        }
        return maxElev == -999 ? 0 : maxElev;
    }

    private static ModuleBuilder GetBestFitSidewalk(LevelBlueprint blueprint, ModuleBuilder[] options, float remainingSpace, float currentX, int currentH, int minClearance)
    {
        if (options == null || options.Length == 0) return null;
        
        System.Collections.Generic.List<ModuleBuilder> validOptions = new System.Collections.Generic.List<ModuleBuilder>();
        ModuleBuilder smallestBlock = options[0];
        float minW = float.MaxValue;

        for (int i = 0; i < options.Length; i++) {
            if (options[i] != null) {
                float w = options[i].moduleConfig.currentTerrainWidth;
                if (w < minW) {
                    minW = w;
                    smallestBlock = options[i];
                }

                if (w <= remainingSpace + 0.1f) {
                    int maxRoad = GetMaxRoadElevationInRange(blueprint, currentX, currentX + w - 0.1f);
                    if (currentH >= maxRoad + minClearance) {
                        validOptions.Add(options[i]);
                    }
                }
            }
        }
        
        if (validOptions.Count > 0) {
            return validOptions[UnityEngine.Random.Range(0, validOptions.Count)];
        }

        // If no block fits perfectly, and we are just trying to close a tiny gap (< 16f)
        if (remainingSpace < 16f) {
            return smallestBlock; // Force the smallest block to minimize overshoot
        }

        return null; // Force slope UP to avoid collision
    }

    private static ModuleBuilder GetBestFit(ModuleBuilder[] options, float remainingSpace)
    {
        if (options == null || options.Length == 0) return null;
        
        System.Collections.Generic.List<ModuleBuilder> validOptions = new System.Collections.Generic.List<ModuleBuilder>();
        for (int i = 0; i < options.Length; i++) {
            if (options[i] != null && options[i].moduleConfig.currentTerrainWidth <= remainingSpace + 0.1f) {
                validOptions.Add(options[i]);
            }
        }
        
        if (validOptions.Count > 0) {
            return validOptions[UnityEngine.Random.Range(0, validOptions.Count)];
        }
        
        // Fallback: Si el espacio restante es demasiado pequeño para cualquier módulo,
        // devolvemos el más pequeño para no pasarnos por mucho.
        ModuleBuilder smallest = options[0];
        float minW = float.MaxValue;
        foreach (var opt in options) {
            if (opt != null && opt.moduleConfig.currentTerrainWidth < minW) {
                minW = opt.moduleConfig.currentTerrainWidth;
                smallest = opt;
            }
        }
        return smallest;
    }
}

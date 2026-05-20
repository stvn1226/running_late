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

            // Near school: step down ONE level at a time (avoids consecutive down-slopes)
            if (remaining < 60f && currentH > 0)
            {
                nextH = currentH - 1;
            }
            // Random height change only when cooldown has expired
            else if (slopeCooldown == 0 && segmentsCreated > safeZoneModules && Random.value < 0.12f)
            {
                nextH = Mathf.Clamp(currentH + (Random.value > 0.5f ? 1 : -1), 0, roadMaxHeight);
            }

            if (nextH != currentH)
            {
                // Place ONE slope this iteration. Cooldown prevents the next iteration
                // from placing another slope immediately, fixing V-shapes and staircases.
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
                    slopeCooldown = 3; // Require 3 flat segments before next slope
                }
                currentH = nextH;
            }
            else
            {
                // Place ONE flat segment this iteration, count down cooldown
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
                    MaxObstacleLevel = 3 // Pass 4 will refine this
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
        const float endpointFlushDistance = 80f; // Flush sidewalk to road level within this distance of Home/School

        int currentH = 0;
        int segmentsCreated = 0;
        float roadStartX = blueprint.HouseAnchor.X_Position + blueprint.HouseAnchor.Width;

        // --- Connector state: tracks sidewalk presence and relative level across iterations ---
        bool prevHasSidewalk = false;
        int  prevRel         = 0;

        foreach (var roadSeg in blueprint.RoadTrack.Segments)
        {
            float segX = roadSeg.X_Position;
            float segW = roadSeg.Width;

            int roadH_start = roadSeg.Y_Elevation;
            int roadH_end   = roadSeg.Y_Elevation;

            if (roadSeg.IsSlope)
            {
                bool isUp = roadSeg.Label.Contains("Up");
                if (isUp) {
                    roadH_end   = roadSeg.Y_Elevation;
                    roadH_start = roadSeg.Y_Elevation - 1;
                } else {
                    roadH_start = roadSeg.Y_Elevation;
                    roadH_end   = roadSeg.Y_Elevation - 1;
                }
            }

            // Sidewalk cannot drop below road level
            if (currentH < roadH_start) currentH = roadH_start;

            int nextH = currentH;

            // Determine whether sidewalk exists at this X
            bool nearEndpoint = (segX - roadStartX < endpointFlushDistance)
                             || (blueprint.TotalLength - segX < endpointFlushDistance);
            bool hasSidewalk  = !nearEndpoint;

            // ── GEOMETRY: Sidewalk segments (unchanged structure, DiagonalRamps.Add removed) ──

            if (roadSeg.IsSlope)
            {
                bool isUp  = roadSeg.Label.Contains("Up");
                nextH = isUp ? currentH + 1 : currentH - 1;
                if (nextH < roadH_end) nextH = roadH_end;

                int newRelLevel   = Mathf.Clamp(nextH - roadH_end, 0, 2);
                ModuleBuilder sTemplate = isUp ? slopeUp : slopeDown;
                int slopeY        = (nextH > currentH) ? nextH : currentH;

                var swSeg = new SegmentData {
                    Template              = sTemplate != null ? sTemplate : roadSeg.Template,
                    X_Position            = segX,
                    Y_Elevation           = slopeY,
                    Width                 = segW,
                    Label                 = "Sidewalk_Follow_Slope",
                    IsSlope               = true,
                    RoadElevationAtX      = roadH_end,
                    SidewalkRelativeLevel = newRelLevel
                };
                blueprint.SidewalkTrack.Segments.Add(swSeg);

                if (logDualLayer)
                    GameLogger.Log("DualLayerData", $"Sidewalk X={segX:F1} W={segW} OldYElev={slopeY} RoadElev={roadH_end} Rel={newRelLevel} Label=Sidewalk_Follow_Slope");

                // NOTE: No DiagonalRamp here. Road slope does NOT trigger an interlayer.
                currentH = nextH;
            }
            else
            {
                // Flat road segment — sidewalk may vary height independently
                if (segmentsCreated > safeZoneModules && Random.value < 0.18f)
                {
                    int randomStep = Random.value > 0.5f ? 1 : -1;
                    nextH = Mathf.Clamp(currentH + randomStep, roadH_end, roadH_end + sidewalkMaxRelHeight);
                }

                if (nearEndpoint) nextH = roadH_end;

                float fillStartX = segX;
                float fillWidth  = segW;

                if (nextH != currentH)
                {
                    int newRelLevel       = Mathf.Clamp(nextH - roadH_end, 0, 2);
                    ModuleBuilder sTemplate = (nextH > currentH) ? slopeUp : slopeDown;

                    if (sTemplate != null)
                    {
                        float sw     = sTemplate.moduleConfig.currentTerrainWidth;
                        int   slopeY = (nextH > currentH) ? nextH : currentH;

                        var swSlopeSeg = new SegmentData {
                            Template              = sTemplate,
                            X_Position            = fillStartX,
                            Y_Elevation           = slopeY,
                            Width                 = sw,
                            Label                 = "Sidewalk_Slope",
                            IsSlope               = true,
                            SpawnObstacles        = false,
                            RoadElevationAtX      = roadH_end,
                            SidewalkRelativeLevel = newRelLevel
                        };
                        blueprint.SidewalkTrack.Segments.Add(swSlopeSeg);

                        if (logDualLayer)
                            GameLogger.Log("DualLayerData", $"Sidewalk X={fillStartX:F1} W={sw} OldYElev={slopeY} RoadElev={roadH_end} Rel={newRelLevel} Label=Sidewalk_Slope");

                        // NOTE: No DiagonalRamp here. Sidewalk rel change is caught by the
                        // event system below (sidewalkRelChanged) using the updated currentRel.
                        fillStartX += sw;
                        fillWidth  -= sw;
                    }
                    currentH = nextH;
                }

                if (fillWidth > 0.1f)
                {
                    int curRelLevel = Mathf.Clamp(currentH - roadH_end, 0, 2);
                    FillSpaceExact(blueprint.SidewalkTrack, sidewalkModules, fillStartX, fillWidth, currentH, segmentsCreated >= safeZoneModules, roadH_end, curRelLevel, logDualLayer);
                }
            }

            // ── CONNECTOR DECISION: event-based, independent of road slope ──
            // currentRel is computed after geometry so currentH is fully updated.
            int currentRel = hasSidewalk ? Mathf.Clamp(currentH - roadH_end, 0, 2) : 0;

            bool sidewalkStarted    = !prevHasSidewalk && hasSidewalk;
            bool sidewalkEnded      =  prevHasSidewalk && !hasSidewalk;
            bool sidewalkRelChanged =  prevHasSidewalk && hasSidewalk && prevRel != currentRel;
            bool shouldAddConnector = sidewalkStarted || sidewalkEnded || sidewalkRelChanged;

            string reason = sidewalkStarted    ? "SidewalkStarted"
                          : sidewalkEnded      ? "SidewalkEnded"
                          : sidewalkRelChanged ? "RelLevelChanged"
                          : "None";

            GameLogger.Log("ConnectorDecisionAudit",
                $"X={segX:F1} " +
                $"prevHasSidewalk={prevHasSidewalk} " +
                $"hasSidewalk={hasSidewalk} " +
                $"prevRel={prevRel} " +
                $"currentRel={currentRel} " +
                $"roadIsSlope={roadSeg.IsSlope} " +
                $"shouldAddConnector={shouldAddConnector} " +
                $"reason={reason}");

            if (shouldAddConnector)
            {
                int fromRel = sidewalkStarted ? 0 : prevRel;
                int toRel   = sidewalkEnded   ? 0 : currentRel;

                ModuleBuilder interlayerTemplate = (toRel >= fromRel) ? diagUp : diagDown;

                if (interlayerTemplate != null)
                {
                    var rampSeg = new SegmentData {
                        Template               = interlayerTemplate,
                        X_Position             = segX,
                        Y_Elevation            = roadH_end,
                        Width                  = segW,
                        Label                  = $"Diagonal_Ramp_{reason}",
                        IsSlope                = false,
                        SpawnObstacles         = false,
                        RoadElevationAtX       = roadH_end,
                        SidewalkRelativeLevel  = toRel,
                        ConnectorRoadElevation = roadH_end,
                        ConnectorRelLevelFrom  = fromRel,
                        ConnectorRelLevelTo    = toRel
                    };
                    blueprint.DiagonalRamps.Add(rampSeg);

                    if (logDualLayer)
                        GameLogger.Log("DualLayerData",
                            $"Connector X={segX:F1} W={segW} RoadElev={roadH_end} " +
                            $"FromRel={fromRel} ToRel={toRel} Label={rampSeg.Label}");
                }
                else
                {
                    // Warn about missing prefab so the developer knows what to assign.
                    string missing = (toRel >= fromRel)
                        ? "interlayerSlopeLeftModule (diagUp)"
                        : "interlayerSlopeRightModule (diagDown)";
                    GameLogger.LogWarning("ConnectorDecisionAudit",
                        $"Connector needed at X={segX:F1} (reason={reason} from={fromRel} to={toRel}) " +
                        $"but template is null. Assign '{missing}' on LevelStarter in the Inspector.");
                }
            }

            prevHasSidewalk = hasSidewalk;
            prevRel         = currentRel;
            segmentsCreated++;
        }
    }

    // Algoritmo de Relleno Exacto para evitar "Gaps" (Huecos)
    private static void FillSpaceExact(TrackBlueprint track, ModuleBuilder[] options, float startX, float targetWidth, int elevation, bool spawnObstacles, int roadElevationAtX = 0, int sidewalkRelativeLevel = 0, bool logDualLayer = false)
    {
        if (options == null || options.Length == 0 || targetWidth <= 0) return;

        float currentX = startX;
        float endX = startX + targetWidth;

        while (currentX < endX - 0.1f)
        {
            float remaining = endX - currentX;
            ModuleBuilder bestFit = GetBestFit(options, remaining);
            if (bestFit == null) break;

            float pieceW = bestFit.moduleConfig.currentTerrainWidth;

            if (pieceW > remaining)
            {
                // OVERLAP-PACKING: Si la pieza es muy grande para el hueco restante, 
                // la desplazamos hacia atrás para que termine EXACTAMENTE en endX.
                // Esto solapa dos piezas planas, cerrando el gap sin invadir el terreno vecino (ej: evitar invadir una rampa).
                float adjustedX = endX - pieceW;
                
                if (adjustedX < startX) 
                {
                    // La pieza es incluso más grande que todo el slot disponible.
                    // Permitimos un ligero clip si no es gigante, si no, lo saltamos (gap menor a un mal overlap).
                    if (pieceW > targetWidth * 1.5f) break; 
                    adjustedX = startX; 
                }
                
                var seg = new SegmentData {
                    Template = bestFit, X_Position = adjustedX, Y_Elevation = elevation, Width = pieceW,
                    Label = "Sidewalk_Segment_Packed", SpawnObstacles = spawnObstacles,
                    RoadElevationAtX      = roadElevationAtX,
                    SidewalkRelativeLevel = sidewalkRelativeLevel
                };
                track.Segments.Add(seg);

                if (logDualLayer)
                    GameLogger.Log("DualLayerData", $"Sidewalk X={adjustedX:F1} W={pieceW} OldYElev={elevation} RoadElev={roadElevationAtX} Rel={sidewalkRelativeLevel} AbsOld={elevation} Label=Sidewalk_Segment_Packed");
                break;
            }
            else
            {
                var seg = new SegmentData {
                    Template = bestFit, X_Position = currentX, Y_Elevation = elevation, Width = pieceW,
                    Label = "Sidewalk_Segment", SpawnObstacles = spawnObstacles,
                    RoadElevationAtX      = roadElevationAtX,
                    SidewalkRelativeLevel = sidewalkRelativeLevel
                };
                track.Segments.Add(seg);

                if (logDualLayer)
                    GameLogger.Log("DualLayerData", $"Sidewalk X={currentX:F1} W={pieceW} OldYElev={elevation} RoadElev={roadElevationAtX} Rel={sidewalkRelativeLevel} AbsOld={elevation} Label=Sidewalk_Segment");
                currentX += pieceW;
            }
        }
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

    private static ModuleBuilder GetBestFit(ModuleBuilder[] options, float remainingSpace)
    {
        if (options == null || options.Length == 0) return null;
        
        // 0=Short, 1=Medium, 2=Wide. 
        // Priority for wider modules to minimize gaps.
        if (options.Length >= 3)
        {
            if (remainingSpace >= options[2].moduleConfig.currentTerrainWidth) return options[2];
            if (remainingSpace >= options[1].moduleConfig.currentTerrainWidth) return options[1];
            return options[0];
        }
        
        return options[Random.Range(0, options.Length)];
    }
}

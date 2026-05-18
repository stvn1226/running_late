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
        int targetLengthUnits,
        int safeZoneModules,
        int roadMaxHeight,
        int sidewalkMaxRelHeight,
        int seed)
    {
        // Initialize seed for deterministic generation
        Random.InitState(seed);
        
        LevelBlueprint blueprint = new LevelBlueprint();

        // Pass 1: Anchors (House & School)
        Pass1_SetAnchors(blueprint, houseModule, schoolModule, targetLengthUnits);

        // Pass 2: Road with Elevations
        Pass2_GenerateRoad(blueprint, roadModules, roadSlopeLeft, roadSlopeRight, safeZoneModules, roadMaxHeight);

        // Pass 3: Sidewalk with Elevations (Sincronizado tipo Radar con Pista)
        Pass3_GenerateSidewalk(blueprint, sidewalkModules, slopeLeft, slopeRight, roadSlopeLeft, roadSlopeRight, safeZoneModules, sidewalkMaxRelHeight);

        // Pass 4: Finalize Obstacles (Clearance check real tras construir ambas capas)
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

        while (currentX < targetX)
        {
            float remaining = targetX - currentX;
            int nextH = currentH;
            if (remaining < 60f) nextH = 0;
            else if (segmentsCreated > safeZoneModules && Random.value < 0.12f)
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
                        Template = sTemplate, X_Position = currentX, Y_Elevation = (nextH > currentH ? nextH : currentH), Width = sw, Label = (nextH > currentH) ? "Road_Slope_Up" : "Road_Slope_Down", IsSlope = true
                    });
                    currentX += sw;
                }
                currentH = nextH;
            }

            remaining = targetX - currentX;
            if (remaining <= 0) break;

            ModuleBuilder bestFit = GetBestFit(roadModules, remaining);
            if (bestFit == null) break;

            float pieceW = bestFit.moduleConfig.currentTerrainWidth;
            
            // Obstacle constraint: Clearance check for trucks
            int sideH = GetSidewalkHeightAt(blueprint, currentX);
            int clearance = sideH - currentH;
            int maxObs = (clearance <= 1) ? 1 : 3;

            blueprint.RoadTrack.Segments.Add(new SegmentData {
                Template = bestFit, X_Position = currentX, Y_Elevation = currentH, Width = pieceW, Label = "Road_Segment", 
                SpawnObstacles = (segmentsCreated >= safeZoneModules),
                MaxObstacleLevel = 3 // Temporal, Pass 4 lo ajustará con precisión
            });
            currentX += pieceW;
            segmentsCreated++;
        }
        blueprint.SchoolAnchor.X_Position = currentX;
    }

    private static void Pass3_GenerateSidewalk(LevelBlueprint blueprint, ModuleBuilder[] sidewalkModules, ModuleBuilder slopeUp, ModuleBuilder slopeDown, ModuleBuilder diagUp, ModuleBuilder diagDown, int safeZoneModules, int sidewalkMaxRelHeight)
    {
        int currentH = 0;
        int segmentsCreated = 0;

        foreach (var roadSeg in blueprint.RoadTrack.Segments)
        {
            float segX = roadSeg.X_Position;
            float segW = roadSeg.Width;
            
            int roadH_start = roadSeg.Y_Elevation;
            int roadH_end = roadSeg.Y_Elevation;

            if (roadSeg.IsSlope)
            {
                bool isUp = roadSeg.Label.Contains("Up");
                if (isUp) {
                    roadH_end = roadSeg.Y_Elevation;
                    roadH_start = roadSeg.Y_Elevation - 1;
                } else {
                    roadH_start = roadSeg.Y_Elevation;
                    roadH_end = roadSeg.Y_Elevation - 1;
                }
            }

            // La vereda no debe hundirse bajo la pista al inicio de este segmento
            if (currentH < roadH_start) currentH = roadH_start;

            int nextH = currentH;
            
            if (roadSeg.IsSlope)
            {
                bool isUp = roadSeg.Label.Contains("Up");
                nextH = isUp ? currentH + 1 : currentH - 1;
                if (nextH < roadH_end) nextH = roadH_end; // Safety clamp
                
                ModuleBuilder sTemplate = isUp ? slopeUp : slopeDown;
                int slopeY = (nextH > currentH) ? nextH : currentH;
                
                blueprint.SidewalkTrack.Segments.Add(new SegmentData {
                    Template = sTemplate != null ? sTemplate : roadSeg.Template, 
                    X_Position = segX, Y_Elevation = slopeY, Width = segW, Label = "Sidewalk_Follow_Slope", IsSlope = true
                });
                
                currentH = nextH;
            }
            else
            {
                // Segmento plano de pista. La vereda puede decidir cambiar de altura.
                if (segmentsCreated > safeZoneModules && Random.value < 0.18f)
                {
                    int randomStep = Random.value > 0.5f ? 1 : -1;
                    nextH = Mathf.Clamp(currentH + randomStep, roadH_end, roadH_end + sidewalkMaxRelHeight);
                }
                
                if (blueprint.TotalLength - segX < 80f) nextH = roadH_end;

                if (nextH != currentH)
                {
                    // CAMBIO DE ALTURA: Usar rampas diagonales para transición
                    ModuleBuilder rTemplate = (nextH > currentH) ? diagUp : diagDown;
                    
                    if (rTemplate != null)
                    {
                        float rampW = rTemplate.moduleConfig.currentTerrainWidth;
                        int stepH = (nextH > currentH) ? (currentH + 1) : currentH;
                        
                        blueprint.DiagonalRamps.Add(new SegmentData {
                            Template = rTemplate, X_Position = segX, Y_Elevation = stepH, Width = rampW, Label = "Diagonal_Ramp_Connection"
                        });
                        
                        currentH = nextH;
                        
                        float remainingW = segW - rampW;
                        if (remainingW > 0) {
                            FillSpaceExact(blueprint.SidewalkTrack, sidewalkModules, segX + rampW, remainingW, currentH, segmentsCreated >= safeZoneModules);
                        }
                    }
                    else
                    {
                        currentH = nextH; 
                        FillSpaceExact(blueprint.SidewalkTrack, sidewalkModules, segX, segW, currentH, segmentsCreated >= safeZoneModules);
                    }
                }
                else
                {
                    FillSpaceExact(blueprint.SidewalkTrack, sidewalkModules, segX, segW, currentH, segmentsCreated >= safeZoneModules);
                }
            }
            segmentsCreated++;
        }
    }

    // Algoritmo de Relleno Exacto para evitar "Gaps" (Huecos)
    private static void FillSpaceExact(TrackBlueprint track, ModuleBuilder[] options, float startX, float targetWidth, int elevation, bool spawnObstacles)
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
                
                track.Segments.Add(new SegmentData {
                    Template = bestFit, X_Position = adjustedX, Y_Elevation = elevation, Width = pieceW, Label = "Sidewalk_Segment_Packed", SpawnObstacles = spawnObstacles
                });
                break;
            }
            else
            {
                track.Segments.Add(new SegmentData {
                    Template = bestFit, X_Position = currentX, Y_Elevation = elevation, Width = pieceW, Label = "Sidewalk_Segment", SpawnObstacles = spawnObstacles
                });
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

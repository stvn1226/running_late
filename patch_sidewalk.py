import os

path = '/Volumes/DiscoAccesoRapido/UnityExternalJobs/unbreakable/running_late/Assets/Scripts/Utils/LevelGeneratorEngine.cs'
with open(path, 'r') as f:
    content = f.read()

start_str = "    private static void Pass3_GenerateSidewalk("
end_str = "    private static void Pass4_FinalizeObstacles("

start_idx = content.find(start_str)
end_idx = content.find(end_str)

new_pass3 = """    private static void Pass3_GenerateSidewalk(LevelBlueprint blueprint, ModuleBuilder[] sidewalkModules, ModuleBuilder slopeUp, ModuleBuilder slopeDown, ModuleBuilder diagUp, ModuleBuilder diagDown, int safeZoneModules, int sidewalkMaxRelHeight, bool logDualLayer = false)
    {
        if (sidewalkModules == null || sidewalkModules.Length == 0) return;

        float singleSpace = sidewalkModules[0].moduleConfig.currentTerrainWidth;
        float startRampWidth = diagUp != null ? diagUp.moduleConfig.currentTerrainWidth : singleSpace;
        float endRampWidth = diagDown != null ? diagDown.moduleConfig.currentTerrainWidth : singleSpace;

        float targetStartRampX = blueprint.HouseAnchor.X_Position + blueprint.HouseAnchor.Width + singleSpace;
        float targetEndRampX = blueprint.SchoolAnchor.X_Position - singleSpace - endRampWidth;

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

        blueprint.DiagonalRamps.Add(new DiagonalRampData {
            Template = diagUp,
            X_Position = startRampX,
            Width = startRampWidth,
            ConnectorRoadElevation = GetRoadElevationAt(blueprint, startRampX),
            Label = "DiagRamp_Start"
        });

        float currentX = sidewalkStartX;
        int currentH = GetRoadElevationAt(blueprint, currentX) + 1; 
        int slopeCooldown = 0;
        int segmentsCreated = 0;

        while (currentX < targetEndRampX)
        {
            float remaining = targetEndRampX - currentX;
            if (remaining <= 0) break;

            int roadElev = GetRoadElevationAt(blueprint, currentX);
            int nextH = currentH;
            
            bool inEndZone = remaining < 40f;
            if (inEndZone)
            {
                if (currentH > roadElev + 1 && slopeCooldown == 0)
                    nextH = currentH - 1;
                else if (currentH < roadElev + 1 && slopeCooldown == 0)
                    nextH = currentH + 1; 
            }
            else 
            {
                if (currentH > roadElev + sidewalkMaxRelHeight && slopeCooldown == 0)
                {
                    nextH = currentH - 1; 
                }
                else if (currentH < roadElev + 1 && slopeCooldown == 0)
                {
                    nextH = currentH + 1; 
                }
                else if (slopeCooldown == 0 && segmentsCreated > safeZoneModules && UnityEngine.Random.value < 0.25f)
                {
                    nextH = UnityEngine.Mathf.Clamp(currentH + (UnityEngine.Random.value > 0.5f ? 1 : -1), roadElev + 1, roadElev + sidewalkMaxRelHeight);
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
                ModuleBuilder bestFit = GetBestFit(sidewalkModules, remaining);
                if (bestFit == null) break;

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

        blueprint.DiagonalRamps.Add(new DiagonalRampData {
            Template = diagDown,
            X_Position = currentX,
            Width = endRampWidth,
            ConnectorRoadElevation = GetRoadElevationAt(blueprint, currentX),
            Label = "DiagRamp_End"
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

"""

new_content = content[:start_idx] + new_pass3 + content[end_idx:]

with open(path, 'w') as f:
    f.write(new_content)

print("Patch applied successfully.")

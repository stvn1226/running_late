import os

path = '/Volumes/DiscoAccesoRapido/UnityExternalJobs/unbreakable/running_late/Assets/Scripts/Utils/LevelGeneratorEngine.cs'
with open(path, 'r') as f:
    content = f.read()

# Add GetMaxRoadElevationInRange
max_elev_func = """    private static int GetMaxRoadElevationInRange(LevelBlueprint blueprint, float startX, float endX)
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
        for (int i = 0; i < options.Length; i++) {
            if (options[i] != null) {
                float w = options[i].moduleConfig.currentTerrainWidth;
                // Accept blocks that fit in remaining space OR if remaining space is very small, we accept the smallest block
                if (w <= remainingSpace + 0.1f || remainingSpace < 16f) {
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
        return null;
    }
"""

# Insert right before GetBestFit
content = content.replace("    private static ModuleBuilder GetBestFit(", max_elev_func + "\n    private static ModuleBuilder GetBestFit(")

# Now update the Pass3 while loop
old_while = """        while (currentX < targetEndRampX)
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
        }"""

new_while = """        while (currentX < targetEndRampX)
        {
            float remaining = targetEndRampX - currentX;
            if (remaining <= 0) break;

            int roadElev = GetRoadElevationAt(blueprint, currentX);
            int nextH = currentH;
            
            bool inEndZone = remaining < 40f;
            if (inEndZone)
            {
                if (currentH > roadElev + 1)
                    nextH = currentH - 1;
                else if (currentH < roadElev + 1)
                    nextH = currentH + 1; 
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
        }"""

content = content.replace(old_while, new_while)

with open(path, 'w') as f:
    f.write(content)

print("Patch applied")

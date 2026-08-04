import os

path = '/Volumes/DiscoAccesoRapido/UnityExternalJobs/unbreakable/running_late/Assets/Scripts/Utils/LevelGeneratorEngine.cs'
with open(path, 'r') as f:
    content = f.read()

old_get_best_fit = """    private static ModuleBuilder GetBestFitSidewalk(LevelBlueprint blueprint, ModuleBuilder[] options, float remainingSpace, float currentX, int currentH, int minClearance)
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
    }"""

new_get_best_fit = """    private static ModuleBuilder GetBestFitSidewalk(LevelBlueprint blueprint, ModuleBuilder[] options, float remainingSpace, float currentX, int currentH, int minClearance)
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
    }"""

content = content.replace(old_get_best_fit, new_get_best_fit)

with open(path, 'w') as f:
    f.write(content)

print("Patch applied")

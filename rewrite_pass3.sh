#!/bin/bash
awk '
BEGIN { in_func = 0; }
/private static void Pass3_GenerateSidewalk/ {
    in_func = 1;
    print "    private static void Pass3_GenerateSidewalk(LevelBlueprint blueprint, ModuleBuilder[] sidewalkModules, ModuleBuilder slopeUp, ModuleBuilder slopeDown, ModuleBuilder diagUp, ModuleBuilder diagDown, int safeZoneModules, int sidewalkMaxRelHeight, bool logDualLayer = false)";
    print "    {";
    print "        float roadStartX = blueprint.HouseAnchor.X_Position + blueprint.HouseAnchor.Width;";
    print "        float singleSpace = sidewalkModules != null && sidewalkModules.Length > 0 ? sidewalkModules[0].moduleConfig.currentTerrainWidth : 15f;";
    print "        float startRampWidth = diagUp != null ? diagUp.moduleConfig.currentTerrainWidth : singleSpace;";
    print "        float endRampWidth = diagDown != null ? diagDown.moduleConfig.currentTerrainWidth : singleSpace;";
    print "        float targetStartRampX = roadStartX + singleSpace;";
    print "        float targetSidewalkStartX = targetStartRampX + startRampWidth;";
    print "        float targetEndRampX = blueprint.SchoolAnchor.X_Position - singleSpace - endRampWidth;";
    print "        float startRampX = targetStartRampX;";
    print "        float sidewalkStartX = targetSidewalkStartX;";
    print "        float endRampX = targetEndRampX;";
    print "        float sidewalkEndX = targetEndRampX;";
    print "";
    print "        if (diagUp != null) {";
    print "            int roadElevAtStart = GetRoadHeightAt(blueprint, startRampX);";
    print "            blueprint.DiagonalRamps.Add(new SegmentData {";
    print "                Template = diagUp, X_Position = startRampX, Y_Elevation = roadElevAtStart, Width = startRampWidth, Label = \"Diagonal_Ramp_Start\",";
    print "                IsSlope = false, SpawnObstacles = false, RoadElevationAtX = roadElevAtStart, SidewalkRelativeLevel = 0, ConnectorRoadElevation = roadElevAtStart, ConnectorRelLevelFrom = 0, ConnectorRelLevelTo = 1";
    print "            });";
    print "        }";
    print "";
    print "        // Generate flat Sidewalk from sidewalkStartX to sidewalkEndX at Y_Elevation = 1";
    print "        float currentX = sidewalkStartX;";
    print "        while (currentX < sidewalkEndX) {";
    print "            float fillWidth = sidewalkEndX - currentX;";
    print "            if (fillWidth < 0.1f) break;";
    print "            float w = Mathf.Min(fillWidth, singleSpace * 5f);";
    print "            FillSpaceExact(blueprint.SidewalkTrack, sidewalkModules, currentX, w, 1, false, 0, 1, logDualLayer);";
    print "            currentX += w;";
    print "        }";
    print "";
    print "        if (diagDown != null) {";
    print "            int roadElevAtEnd = GetRoadHeightAt(blueprint, endRampX);";
    print "            blueprint.DiagonalRamps.Add(new SegmentData {";
    print "                Template = diagDown, X_Position = endRampX, Y_Elevation = roadElevAtEnd, Width = endRampWidth, Label = \"Diagonal_Ramp_End\",";
    print "                IsSlope = false, SpawnObstacles = false, RoadElevationAtX = roadElevAtEnd, SidewalkRelativeLevel = 1, ConnectorRoadElevation = roadElevAtEnd, ConnectorRelLevelFrom = 1, ConnectorRelLevelTo = 0";
    print "            });";
    print "        }";
    print "    }";
    next;
}
in_func == 1 && /private static void FillSpaceExact/ {
    in_func = 0;
}
in_func == 0 { print; }
' Assets/Scripts/Utils/LevelGeneratorEngine.cs > temp.cs && mv temp.cs Assets/Scripts/Utils/LevelGeneratorEngine.cs

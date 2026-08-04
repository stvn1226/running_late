#!/bin/bash
awk '
BEGIN { in_func = 0; }
/private void Pass4_RenderDiagonalRamps/ {
    in_func = 1;
    print "    private void Pass4_RenderDiagonalRamps(LevelBlueprint blueprint, float baseY, int baselineH)"
    print "    {"
    print "        if (blueprint.DiagonalRamps == null) return;"
    print "        foreach (var ramp in blueprint.DiagonalRamps)"
    print "        {"
    print "            if (ramp.Template == null) continue;"
    print "            float gbh = ramp.Template.moduleConfig != null ? ramp.Template.moduleConfig.groundBlockHeight : 3;"
    print "            float rampCenter = ramp.X_Position + ramp.Width / 2f;"
    print "            float rampZ = sidewalkZOffset / 2f;"
    print ""
    print "            // The Sidewalk is exactly at baseY + sidewalkYOffset (which is 1.5)."
    print "            // So the Sidewalk surface is 1.5 units higher than the Road surface."
    print "            // A slope prefab is exactly 3.0 units tall."
    print "            // By spawning the slope at Y + 1.5, its bottom half (1.5 to 3.0) is hidden in the road,"
    print "            // and its top half (3.0 to 4.5) sticks out, perfectly bridging the 1.5 unit gap."
    print "            float rampTransformY = baseY + ramp.ConnectorRoadElevation * gbh + sidewalkYOffset;"
    print "            int rampAbsH = 1;"
    print ""
    print "            SpawnModularSlope(ramp.Template, rampCenter, rampTransformY, rampZ, rampAbsH, ramp.Label, true);"
    print "        }"
    print "    }"
    next;
}
/private void RenderSidewalkTrack/ {
    in_func = 0;
}
!in_func { print }
' Assets/Scripts/Utils/LevelStarter.cs > tmp.cs && mv tmp.cs Assets/Scripts/Utils/LevelStarter.cs

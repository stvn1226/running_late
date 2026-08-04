#!/bin/bash
awk '
BEGIN { in_func = 0; }
/private void RenderSidewalkTrack/ {
    in_func = 1;
    print "    private void RenderSidewalkTrack(LevelBlueprint blueprint, float baseY, int baselineH)"
    print "    {"
    print "        // OLD PATH: the Y_Elevation in Pass3 already accounts for +1 (e.g., roadH_end + 1)."
    print "        // We RESTORE sidewalkYOffset here because the user relies on it for the 1.5 unit vertical shift."
    print "        // And we pass absH = 1 inside RenderTrack implicitly by letting seg.Y_Elevation be 0 relative to road."
    print "        RenderTrack(blueprint.SidewalkTrack, baseY + sidewalkYOffset, baselineH, sidewalkZOffset);"
    print "    }"
    next;
}
/private void RenderTrack/ {
    in_func = 0;
}
!in_func { print }
' Assets/Scripts/Utils/LevelStarter.cs > tmp.cs && mv tmp.cs Assets/Scripts/Utils/LevelStarter.cs

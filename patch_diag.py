import os

path = '/Volumes/DiscoAccesoRapido/UnityExternalJobs/unbreakable/running_late/Assets/Scripts/Utils/LevelGeneratorEngine.cs'
with open(path, 'r') as f:
    content = f.read()

# Replace DiagRamp_Start
old_start = """        blueprint.DiagonalRamps.Add(new DiagonalRampData {
            Template = diagUp,
            X_Position = startRampX,
            Width = startRampWidth,
            ConnectorRoadElevation = GetRoadElevationAt(blueprint, startRampX),
            Label = "DiagRamp_Start"
        });"""

new_start = """        int roadElevAtStart = GetRoadElevationAt(blueprint, startRampX);
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
        });"""

content = content.replace(old_start, new_start)

# Replace DiagRamp_End
old_end = """        blueprint.DiagonalRamps.Add(new DiagonalRampData {
            Template = diagDown,
            X_Position = currentX,
            Width = endRampWidth,
            ConnectorRoadElevation = GetRoadElevationAt(blueprint, currentX),
            Label = "DiagRamp_End"
        });"""

new_end = """        int roadElevAtEnd = GetRoadElevationAt(blueprint, currentX);
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
        });"""

content = content.replace(old_end, new_end)

with open(path, 'w') as f:
    f.write(content)

print("Patch applied")

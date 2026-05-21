# Project Overview
- **Game Title**: (Not specified, environment enhancement)
- **High-Level Concept**: Adding environmental detail (grass) to a garden/outdoor scene to make it look less "flat" or "empty".
- **Players**: Single player (assumed)
- **Render Pipeline**: URP (PC_RPAsset)
- **Target Platform**: PC (StandaloneWindows64)

# Game Mechanics
## Core Gameplay Loop
N/A (Environmental enhancement)

## Controls and Input Methods
N/A

# UI
N/A

# Key Asset & Context
- **Target Objects**:
    - `Outdoor_Environment/Corridor_Side/Flowerbed/Soil_Base` (Reference for bounds)
    - `Outdoor_Environment/Ground` (Reference for bounds)
- **Grass Prefabs**:
    - A variety of prefabs from `Assets/Imported/EnvironmentAssetKit/Atlas/Prefabs_Atlas/Grass/` (e.g., Grass_A_01_01, Grass_B_01_01, Grass_C_01_01, etc.)

# Implementation Steps
1. **Analyze Placement Areas**:
    - Identify the exact world-space bounds for the `Soil_Base` mesh (within the Flowerbed) and the `Ground` mesh.
    - `Soil_Base` Bounds (Approx): X: [-2.7, -1.3], Z: [-10, 60], Y: 0.06
    - `Ground` Bounds (Approx): X: [-21.2, 31.2], Z: [-6.35, 56.35], Y: -0.05

2. **Prefab Selection**:
    - Randomly pick from the 50+ available grass prefabs in the `Assets/Imported/EnvironmentAssetKit/Atlas/Prefabs_Atlas/Grass/` directory.

3. **Distribution Logic**:
    - **Flowerbed (Soil_Base)**: Place approximately 50 grass instances.
    - **Ground**: Place approximately 250 grass instances (intended to be sparse over the large area).
    - For each instance:
        - Calculate a random (X, Z) coordinate within the target object's bounds.
        - Set Y coordinate to match the surface height.
        - Apply a random rotation around the Y-axis (0-360 degrees).
        - Apply a random scale variation (0.8x to 1.2x) for natural look.

4. **Instantiation**:
    - Use `PrefabUtility.InstantiatePrefab` to maintain prefab links.
    - Parent the new grass objects under `Outdoor_Environment/Corridor_Side/Flowerbed` and `Outdoor_Environment/Ground` respectively.

# Verification & Testing
- **Visual Check**: Open the scene in the Unity Editor and verify that the grass is distributed naturally and doesn't float or clip significantly in a way that looks broken.
- **Performance Check**: Ensure the addition of ~300 grass prefabs doesn't cause significant FPS drops (given they are simple atlas-based prefabs, this should be fine).
- **Hierarchy Check**: Verify that the grass objects are correctly parented and not cluttering the root scene.

# Project Overview
- Game Title: Fire Safety Simulation (Assumed)
- High-Level Concept: A 3D environment where the user is setting up fire safety equipment.
- Players: Single player
- Inspiration / Reference Games: N/A
- Tone / Art Direction: Realistic / Industrial
- Target Platform: PC (Standalone Windows 64)
- Screen Orientation / Resolution: Landscape
- Render Pipeline: PC_RPAsset (Custom RP or URP)

# Game Mechanics
## Core Gameplay Loop
The user is arranging interior elements and safety equipment in a building layout.
## Controls and Input Methods
Standard Unity Editor controls for object placement and scene management.

# UI
N/A (Editor-based task)

# Key Asset & Context
- Extinguisher Prefab: `Assets/Imported/Fire Extinguisher Pack/prefabs/ms_extinguisher.prefab`
- Left Wall Segments: Located at `x = -1.25`, `z` from `0` to `47.5`.
- Window Mesh: `WallFloor1_09` (identified as the window wall based on user description and scene scan).
- Current Window Positions (z): `2.5, 10.0, 17.5, 25.0, 32.5, 40.0, 47.5`.

# Implementation Steps
1. **Create Container**:
   - Create a new empty GameObject named `FireExtinguishers` at the root or under an appropriate parent.
2. **Identify Target Positions**:
   - Calculate the midpoint between consecutive window segments on the left wall (`x = -1.25`).
   - Targets (z): `6.25, 13.75, 21.25, 28.75, 36.25, 43.75`.
3. **Instantiate Extinguishers**:
   - Instantiate `ms_extinguisher.prefab` at each target position.
   - Position: `Vector3(-0.95f, 0.0f, z)` (Offset from the wall to sit on the floor).
   - Rotation: `Quaternion.Euler(0, 90, 0)` to face the interior of the room.
4. **Parenting**:
   - Parent all instantiated extinguishers under the `FireExtinguishers` GameObject created in Step 1.

# Verification & Testing
- **Visual Check**: Open the scene and verify that each extinguisher is centered between two windows.
- **Hierarchy Check**: Verify all extinguishers are children of the `FireExtinguishers` object.
- **Collision Check**: Ensure the extinguishers are not clipping into the wall or floating above the floor.
- **Orientation Check**: Confirm the extinguishers are facing the correct direction (into the room).

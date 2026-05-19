# Project Overview
- Game Title: Mini Game Project
- High-Level Concept: Environment and Atmosphere refinement.
- Context: A night scene where the map edges are currently visible and look "fake".

# Game Mechanics
## Atmosphere (Fog and Post-Processing)
- Use global fog to hide the map's geometry edges.
- Use post-processing (Vignette) to focus the player's vision on the play area.

# UI
- N/A

# Key Asset & Context
- **RenderSettings**: Global Unity class for fog settings.
- **Global Volume**: Volume object handling Post-Processing overrides (Vignette, etc.).
- **Scene Bounds**: Approximately 80x70 units.

# Implementation Steps
1. **Configure Linear Fog**:
   - Change `RenderSettings.fogMode` to `Linear`.
   - Set `RenderSettings.fogStartDistance` to `10`.
   - Set `RenderSettings.fogEndDistance` to `60` (to ensure everything beyond the playable area is hidden).
   - Set `RenderSettings.fogColor` to a dark midnight blue `(0.05, 0.05, 0.1)`.

2. **Adjust Post-Processing**:
   - Update `Global Volume` profile's `Vignette` component.
   - Set `intensity` to `0.3` and ensure `smoothness` is around `0.4` for a natural focus effect.

3. **Verification**:
   - Check the scene view and game view to ensure the map edges are no longer visible.
   - Ensure the transition from the map to the fog looks seamless.

# Verification & Testing
- Move the camera to the edge of the playable area and verify that the "void" or "end of map" is not visible.
- Verify that objects near the center are still clearly visible (Start distance = 10).

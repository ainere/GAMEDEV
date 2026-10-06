# Design Review

## Core experience

The game has one clear objective: survive a 90-second laser run, then reach the exit. A fixed, centered CCTV view gives the corridor a surveillance-room feel while keeping the player, hazards and destination in one frame. The HUD carries health, time and active effects around the camera image.

## Readability and fairness

The room's floor plates share the same shape and material. Their changing light communicates function: steady light marks helpful effects, and irregular flicker marks traps. This keeps the floor legible without turning its geometry into a set of visual clues.

Lasers are the immediate warning. Their straight-line layouts are visible in the corridor; the interface does not preview patterns or show suggested dodge routes. Before a layout is selected, the safety validator checks for a continuous escape route using the player's movement and posture limits. Blue exit lasers stay lethal until shutdown, even when the player has a shield.

## Variety and pacing

Sixteen pattern families each have four authored designs, for 64 designs and 128 layouts when mirrored orientations are counted. Patterns unlock through the run, and the selected speed and number of simultaneous waves increase challenge. Each run starts a fresh selection sequence; recent families and variants are downweighted when alternatives exist. These rules change the encounter order without changing the survival objective.

Healing, shields and temporary slowdowns give players reasons to read the floor while moving. Traps use the same plate geometry and remain avoidable through their light behavior and the reserved escape route.

## Source references

- [Gameplay and project map](README.md)
- [Asset sources and licenses](ASSET_CREDITS.md)
- [Verification results](VERIFICATION.md)
- Pattern selection and safety checks: `Assets/LaserCorridor/Runtime/LaserDirector.cs`, `PatternLibrary.cs` and `SafetyValidator.cs`
- Floor and run rules: `Assets/LaserCorridor/Runtime/PanelDirector.cs` and `RunModel.cs`

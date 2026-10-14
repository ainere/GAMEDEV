# Gameplay: Knockdown

A balloon-popping carnival game for the **Gameplay: Knockdown** assignment. One editable Unity scene supports **Static** and **Moving** target modes.

## Open and play

1. Open this project in Unity **6000.6.0f1**.
2. Open `Assets/Scenes/Knockdown.unity`.
3. Enter Play mode, choose **Static** or **Moving**, and press **Start**.

Move the mouse to turn the camera and aim with the centered crosshair. Click or hold the left mouse button to throw darts. During play, the cursor is locked and hidden. Press **Escape** to pause and release the cursor, then use the on-screen controls to resume, restart, or select a mode.

## Rules

- A three-second countdown starts a 60-second round with unlimited darts.
- Each balloon pops with one dart and awards 10 points. Balloons respawn after 0.75 seconds.
- Static targets stay in place. Moving rows cross the booth in alternating directions, bob up and down, and loop behind its side panels.
- Darts have visible travel time, so moving targets require a little lead.
- At the bell, throwing and respawning stop. Darts already in flight have another 0.5 seconds to score.
- Results show score, balloons popped, and accuracy. Misses have no score penalty.

Changing modes resets the round and camera within the same scene. Pause freezes the timer, camera, targets, darts, and respawns. Returning from a menu discards its mouse movement before aiming resumes.

## Editing

The scene contains editable booth geometry, target objects, lighting, camera, and UI. Art lives in [Assets/Art](Assets/Art), shared prefabs in [Assets/Prefabs](Assets/Prefabs), gameplay code in [Assets/Scripts](Assets/Scripts), and tests in [Assets/Tests](Assets/Tests). Edit the supplied scene and prefabs directly in Unity.

The gameplay controller exposes round and dart settings in the Inspector. Balloon components expose lane movement and respawn settings. The camera's `KnockdownCameraFraming` component exposes mouse sensitivity and look limits, and fits the booth to the Game view aspect ratio.

## Tests

Use **Window / General / Test Runner** to run the EditMode round-state tests and PlayMode gameplay tests, including mouse look, centered aiming, pause, mode switching, scoring, respawns, and dart collisions.

## Assets

The booth, balloons, darts, and game interface are original. Kenney's CC0 impact sounds accompany missed darts; the pop sound is included with the project's audio. Coaster Kit props are available in [Assets/Art/External](Assets/Art/External) for further editing. See [ASSET_CREDITS.md](ASSET_CREDITS.md) and the included licences.

The Unity scene is [Assets/Scenes/Knockdown.unity](Assets/Scenes/Knockdown.unity). The project is playable in the Editor; no standalone build is included. Find the other course submissions in the [GAMEDEV directory](https://github.com/ainere/GAMEDEV/tree/main).

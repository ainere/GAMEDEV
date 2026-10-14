# Gameplay: Knockdown

A carnival dart game for the **Gameplay: Knockdown** assignment. Knock cardboard duck targets backward on their hinges, with **Static** and **Moving** modes in one Unity scene.

## Open and play

1. Open this project in Unity **6000.6.0f1**.
2. Open `Assets/Scenes/Knockdown.unity`.
3. Enter Play mode, choose **Static** or **Moving**, and press **Start**.

Move the mouse to turn the camera and aim with the centered crosshair. Click or hold the left mouse button to throw darts. During play, the cursor is locked and hidden. Press **Escape** to pause and release the cursor, then use the on-screen controls to resume, restart, or select a mode.

## Rules

- A three-second countdown starts a 60-second round with unlimited darts.
- Twelve cutouts sit on three recessed conveyor shelves. Every standing target takes one hit and awards 10 points, including hits outside its painted bullseye.
- Static targets fold backward, stay down for 0.75 seconds, then stand up in place. Folding takes 0.16 seconds and raising takes 0.24 seconds.
- Moving targets travel at 1.4 metres per second in alternating row directions. A knocked-down cutout stays on its carriage until it passes completely behind a side cover, where it stands up before its next crossing. Looking away does not reset it.
- Darts have visible travel time, so moving targets require a little lead.
- At the bell, throwing and target resets stop. Darts already in flight have another 0.5 seconds to score.
- Results show score, targets knocked down, and accuracy: successful knockdowns divided by darts thrown. Misses have no score penalty.

Changing modes resets the round, targets, darts, score, timer, and camera within the same scene. Pause freezes the timer, camera, targets, darts, and hinge animations. Returning from a menu discards its mouse movement before aiming resumes.

Darts travel at 28 metres per second with gravity disabled. Holding the fire button throws one dart every 0.30 seconds. Firing is blocked over UI and outside the target area.

## Project contents

- [Knockdown scene](Assets/Scenes/Knockdown.unity): booth, three target rows, camera, lighting, and interface.
- [Prefabs](Assets/Prefabs): reusable targets, darts, booth, impact dust, and score feedback.
- [Gameplay source](Assets/Scripts): round state, scoring, projectile collisions, conveyor motion, hinge animation, and FPS aiming.

## Assets

The duck artwork comes from Kenney's CC0 Shooting Gallery pack. The booth, cardboard thickness, hinges, conveyors, darts, and interface are part of this project. Kenney's CC0 impact sounds accompany hits and misses. Selected Coaster Kit props are also included in [Assets/Art/External](Assets/Art/External). See [ASSET_CREDITS.md](ASSET_CREDITS.md) and the included licences.

The Unity scene is [Assets/Scenes/Knockdown.unity](Assets/Scenes/Knockdown.unity). The project is playable in the Editor; no standalone build is included. Find the other course submissions in the [GAMEDEV directory](https://github.com/ainere/GAMEDEV/tree/main).

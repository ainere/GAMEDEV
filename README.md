<h1 align="center">Ray-OE Zoning</h1>
<p align="center"><strong>Elkan Ainer M. La Madrid · 12413739 · GAMEDEV S01</strong><br>De La Salle University · Unity 6000.6.0f1 · Universal Render Pipeline 17.6.0</p>

A first person CCTV infiltration mission inside a guarded security annex. Move between surveillance sectors, use server cabinets to break camera sight, and reveal concealed maintenance markings with your flashlight. Decode the archive terminal and reach the protected archive. The game is silent and has no jump scares.

## Open the project

```bash
git clone --branch ray-oe-zoning --single-branch https://github.com/ainere/GAMEDEV.git
```

Add the cloned project folder in Unity Hub and open it with **Unity 6000.6.0f1**. Open `Assets/RayOEZoning/Scenes/RayOEZoning.unity` and press Play. This branch contains the editable Unity project; standalone builds and verification artifacts are kept locally.

## Controls

| Input | Action |
| --- | --- |
| WASD | Move |
| Mouse | Look |
| F | Toggle flashlight |
| E | Use the nearby exit keypad |
| N | Open or close field notes |
| Escape | Close notes/keypad, or pause/resume |
| 0–9, Backspace, Enter | Enter, edit and submit the keypad code |
| F3 | Toggle the detection diagnostic overlay |

Movement is deliberately paced. Sprint and jump are disabled. Notebook, keypad and pause screens freeze movement, patrols and alert until closed.

## How to play

Find four hidden glyph objects. Their symbols, digits and ordering clues appear only when the flashlight is on, the object lies inside its cone and range, and no wall or cover blocks the beam. Looking at a clue within three metres for 0.8 seconds records it in the notebook. Turning the flashlight off or looking away hides the object again; your recorded notes remain available.

Panoramic CCTV cameras travel along ceiling rails in three sectors: Perimeter, Signal and Records. Their amber floor markings show the moving surveillance zones. Observe them, cross when their fields move away, and use solid server cabinets to break line of sight. Confirmed detection raises a shared alert meter. Breaking sight drains it. A full meter returns you to the entrance and resets the patrols, keypad and exit; field notes survive the retry. Notes are held for the current session.

Deduce the symbol order from the recorded clues and enter its four digits at the archive terminal. A correct answer disables the security grid. Walk through the archive entrance to finish. The terminal accepts a correct answer regardless of how many clues were recorded.

## Detection mechanics

| Moving machine | Functional detection query |
| --- | --- |
| CAM 01 / Perimeter | `Physics.CheckSphere` |
| CAM 02 / Signal | `Physics.CheckBox` (the box/cube check) |
| CAM 03 / Records | `Physics.CheckCapsule` |
| All three | Obstruction `Physics.Raycast` checks confirm sight to the player |
| Flashlight clues | Cone angle, range, on-state and obstruction raycast checks |

Sight rays start at the actual mounted camera lenses. The F3 overlay shows the real query shapes and per-camera sight state. Layer masks restrict player checks and distinguish solid obstructions from decorative geometry. Server cover has real colliders.

The Unity Starter Assets first person controller and Input System assets are included. A regular camera follows its camera target, with no Cinemachine dependency.

## Source and tests

- `Assets/RayOEZoning/Runtime/`: stealth state, spatial detection, flashlight reveal, puzzle, interface and visual atmosphere.
- `Assets/RayOEZoning/Editor/`: reproducible scene authoring and Windows build commands.
- `Assets/RayOEZoning/Tests/`: focused EditMode and PlayMode tests.
- `Assets/LaserCorridor/`: retained corridor art and original source used as the basis.
- `ASSET_CREDITS.md`: included asset sources and licenses.
- `VERIFICATION.md`: executed verification and its limits.

Run tests through Unity's Test Runner. The Ray-OE Zoning editor menu rebuilds the authored scene and creates a local Windows build. Legacy corridor tests exercise the retained original source scene, rather than the new stealth level.

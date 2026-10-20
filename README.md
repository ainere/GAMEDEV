<h1 align="center">Ray-OE Zoning</h1>
<p align="center"><strong>Elkan Ainer M. La Madrid · 12413739 · GAMEDEV S01</strong><br>De La Salle University · Unity 6000.6.0f1 · Universal Render Pipeline 17.6.0</p>

A first person stealth puzzle set in an abandoned industrial corridor. Use a flashlight to reveal concealed symbols and their clues, work out the four-digit exit code, and avoid three moving surveillance machines. The mood relies on dim lighting, enclosed spaces and moving surveillance equipment, without jump scares. The game and demo are silent.

## Gameplay demo

[Short gameplay and detection demonstration](Demo/Ray-OE-Zoning-demo.mp4)

The 93-second demo shows flashlight visibility, all three moving detection volumes, raycast occlusion behind cover, a capture and retry with retained field notes, and the solved exit puzzle. It records automated controller playback in the real Windows game at 1080p/30 fps, with no audio stream.

## Open the project

```bash
git clone --branch ray-oe-zoning --single-branch https://github.com/ainere/GAMEDEV.git
```

Add the cloned project folder in Unity Hub and open it with **Unity 6000.6.0f1**. Open `Assets/RayOEZoning/Scenes/RayOEZoning.unity` and press Play. This branch contains the editable Unity project and demo; standalone builds and QA recordings are kept locally.

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

The scanner machines move on repeating patrols. Observe them, cross when their fields move away, and use solid cover to break line of sight. Confirmed detection raises a shared alert meter. Breaking sight drains it. A full meter quietly returns you to the entrance and resets the patrols, keypad and exit; field notes survive the retry. Notes are held for the current session.

Deduce the symbol order from the recorded clues and enter its four digits at the exit keypad. A correct answer disables the blue grid. Walk through the exit to finish. The keypad accepts a correct answer regardless of how many clues were recorded.

## Detection mechanics

| Moving machine | Functional detection query |
| --- | --- |
| Lens rover | `Physics.CheckSphere` |
| Rectangular scanner | `Physics.CheckBox` (the box/cube check) |
| Capsule scanner | `Physics.CheckCapsule` |
| All three | Obstruction `Physics.Raycast` checks confirm sight to the player |
| Flashlight clues | Cone angle, range, on-state and obstruction raycast checks |

The F3 overlay uses the same shape parameters as the actual queries. Layer masks restrict player checks and distinguish solid obstructions from decorative geometry. Cover has real colliders.

The Unity Starter Assets first person controller and Input System assets are included. A regular camera follows its camera target, with no Cinemachine dependency.

## Source and tests

- `Assets/RayOEZoning/Runtime/`: stealth state, spatial detection, flashlight reveal, puzzle, interface and visual atmosphere.
- `Assets/RayOEZoning/Editor/`: reproducible scene authoring and Windows build commands.
- `Assets/RayOEZoning/Tests/`: focused EditMode and PlayMode tests.
- `Assets/LaserCorridor/`: retained corridor art and original source used as the basis.
- `ASSET_CREDITS.md`: included asset sources and licenses.
- `VERIFICATION.md`: executed verification and its limits.

Run tests through Unity's Test Runner. The Ray-OE Zoning editor menu rebuilds the authored scene and creates a local Windows build. Legacy corridor tests exercise the retained original source scene, rather than the new stealth level.

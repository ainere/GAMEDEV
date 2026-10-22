<h1 align="center">Ray-OE Zoning</h1>
<p align="center"><strong>Elkan Ainer M. La Madrid · 12413739 · GAMEDEV S01</strong><br>De La Salle University · Unity 6000.6.0f1 · Universal Render Pipeline 17.6.0</p>

A first person stealth infiltration of a research laboratory after hours. The lights are off, so the flashlight is your main tool: it shows the way and it is the only thing that reveals the hidden access records. Six CCTV searchlights sweep the rooms. If a camera's light lands on you and the lens can see you, you are being recorded. Collect the four records, work out the archive code, and reach the protected sample in the vault. The game is silent and has no jump scares.

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
| C or Left Ctrl | Toggle crouch |
| Z | Toggle prone |
| Left Shift + forward movement | Sprint while standing |
| F | Toggle flashlight |
| E | Use the nearby archive terminal |
| N | Open or close field notes |
| Escape | Close notes/keypad, or pause/resume |
| 0–9, Backspace, Enter | Enter, edit and submit the keypad code |
| F3 | Toggle detection diagnostics (query wireframes, cones and sight rays) |

A key legend appears at the start of a run and on the pause screen. Standing, crouched and prone movement use different speeds and body heights. Standing up is blocked when there is not enough headroom. Jump is disabled. The notebook, keypad and pause screens freeze movement, patrols and alert until closed.

## How to play

The laboratory is dark but not pitch black: a few night lights, monitors and status LEDs outline each room, and the flashlight does the rest. Four access records are mounted around the lab. Their symbol, digit and ordering statement show only while the flashlight is on and the record is inside its cone and range with nothing in the way. Hold the light on a record within three metres for 0.8 seconds to copy it into your notes.

Each CCTV camera throws a white, hard-edged searchlight that moves with the camera housing. The lit pool on the floor, traced by a thin outline, is exactly the area that camera covers. Standing in a pool fills the segmented detection meter at the bottom of the screen if the lens has a clear sight line to your head or torso. Tall equipment casts safe shadows. Behind low benches you must crouch so your head drops out of sight. Red chevrons around the crosshair point at any camera that is recording you. Cyan chevrons mean you are inside a beam but hidden by cover. The searchlight and its outline turn red while it sees you.

Full exposure takes 1.35 seconds; breaking sight drains the meter over 1.5 seconds. Getting caught returns you to the staff entrance and resets the patrols, stance, keypad and archive door, but your notes are kept.

Deduce the symbol order from the records and enter its four digits at the archive terminal. A correct code shuts off the laser grid and raises the blast door. Walk into the vault to finish. A known route takes a little over a minute; a first run is comfortably under three.

### Rooms

| Room | What it asks of you |
| --- | --- |
| Staff entry | A wide camera sweeps the floor between a supply divider and the lab door. Wait behind the divider and cross on the sweep. |
| Analytical laboratory | Long bench islands run along the route. Their service spines hide you from the side camera if you crouch. A storage island shades the left lane from the far camera, leaving one gap to time. |
| Specimen cold storage | Freezer rows form a serpentine. The long way round is watched; a cryo-line crawlspace lets you go under it prone. |
| Secure research | Two cameras watch the server lanes and the archive door. The glass research chamber does not block their view. |
| Archive vault | The prototype sample on its pedestal is the objective. |

## Detection mechanics

| Moving camera | Spatial query |
| --- | --- |
| Cameras 1 and 4 (wide) | `Physics.CheckSphere` |
| Cameras 2 and 5 (narrow, long range) | `Physics.CheckBox` (the box/cube check, oriented to the lens) |
| Cameras 3 and 6 (ceiling mounted) | `Physics.CheckCapsule` |
| All six | `Physics.Raycast` sight checks from the actual lens to the player's head and torso |
| Flashlight records | Cone angle, range, on-state and `Physics.Raycast` obstruction checks from the held lamp and the eye |

Each camera first runs its real sphere, box or capsule query against the player's collider. It then checks that the player's footprint lies inside the lens's view cone and range, which are the same angle and range as its searchlight, so the footprint test matches the lit floor pool. Finally, sight raycasts must reach the head or torso sample, both of which follow the current stance height. F3 adds the query wireframes, the clipped cone and the last sight ray for every camera.

The Unity Starter Assets first person controller and Input System are used. A regular camera follows the controller's camera target, with no Cinemachine dependency.

## Source and tests

- `Assets/RayOEZoning/Runtime/`: stealth state, spatial detection, flashlight reveal, stance, puzzle, interface and visuals (searchlight shafts, coverage outline, first person body and hand).
- `Assets/RayOEZoning/Editor/`: reproducible level authoring (`ZoningBuilder*.cs`, including generated surface textures and the post-processing profile) and the Windows build command.
- `Assets/RayOEZoning/Art/`: materials, generated textures and the `LightShaft` shader.
- `Assets/RayOEZoning/Tests/`: EditMode and PlayMode tests.
- `Assets/LaserCorridor/`: the original laser corridor level the project started from.
- `ASSET_CREDITS.md`: asset sources and licenses.
- `VERIFICATION.md`: what was verified and its limits.

Run tests through Unity's Test Runner. **Ray-OE Zoning → Create level** rebuilds the scene from code and **Ray-OE Zoning → Build Windows** makes a local Windows build. The legacy corridor tests exercise the original source scene, not the stealth level.

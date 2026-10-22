<h1 align="center">Prototyping 2: Ray-OE Zoning</h1>
<p align="center"><strong>Elkan Ainer M. La Madrid · 12413739 · GAMEDEV S01</strong><br>De La Salle University · Unity 6000.6.0f1 · Universal Render Pipeline 17.6.0</p>

A first person stealth infiltration of a research laboratory after hours. The lights are off, so the flashlight is your main tool: it shows the way and it is the only thing that reveals the hidden access records. Six CCTV searchlights sweep the rooms. If a camera's light lands on you and the lens can see you, you are being recorded. Find the four code digits and the two order notes, work out the archive code, and reach the protected sample in the vault. The code, its symbols and where the notes are hidden change every run. The game is silent and has no jump scares.

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
| Escape | Close the keypad |
| 0–9, Backspace, Enter | Enter, edit and submit the keypad code |
| F3 | Toggle detection diagnostics (query wireframes, cones and sight rays) |
| R | Play again after reaching the vault |

There is no pause menu or start-up key legend. Standing, crouched and prone movement use different speeds and body heights. Standing up is blocked when there is not enough headroom. Jump is disabled. The keypad freezes movement, patrols and alert until closed.

## How to play

The laboratory is dark but not pitch black: a few night lights, monitors and status LEDs outline each room, and the flashlight does the rest. Hidden writing appears only where the flashlight's beam falls: sweep the light across a page and the strokes outside the beam stay invisible. The flashlight adjusts its brightness to the distance of what it hits, so close-up paper is not washed out. Nothing is copied for you; remember the digits and order notes.

Each run rolls a new four-digit code from a pool of 16 symbols. Every room hides one **code digit**: a symbol with its name and digit. Two other surfaces carry the **order notes**, short statements such as `KEY RIGHT AFTER MOON` or `STAR IS NOT FIRST`. The two notes together pin down exactly one symbol order; neither does on its own. The writing matches whatever it is on: an engraved wall plaque, ballpoint on a clipboard, pencil on loose paper, marker on a sticky note or whiteboard, green text on a sleeping monitor, an LCD control panel, label-maker tape on a freezer, a spray-painted stencil, or a typed memo. Fourteen surfaces can carry writing, so the hiding places differ between runs.

Each CCTV camera throws a white, hard-edged searchlight that moves with the camera. The lit pool on the floor, traced by a thin outline, is exactly the area that camera covers. Tall equipment casts safe shadows. Behind low benches you must crouch so your head drops out of sight. The searchlight and its outline turn red while it sees you.

The screen is clear until a camera sees you. Then a detection ring fades in around the centre of the screen. It is a thin, faint circle that fills from the bottom up both sides at once, shading smoothly from white through amber to red (the same red as the soft screen-edge glow) at the top. A small red arrow outside the ring points at the watching camera. When the ring is full, a small **SPOTTED** label appears under it. Once you are out of sight the ring drains and fades away.

Full exposure takes 1.35 seconds; breaking sight drains the meter over 1.5 seconds. Getting caught returns you to the staff entrance and resets the patrols, stance, keypad and archive door.

Sort the digits by the order notes and enter them at the archive terminal. A correct code shuts off the laser grid and raises the blast door. Walk into the vault to finish. A known route takes a little over a minute; a first run is comfortably under three.

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
| Flashlight clues | Cone angle, range, on-state and `Physics.Raycast` obstruction checks from the held lamp and the eye |

Each camera first runs its real sphere, box or capsule query against the player's collider. It then checks that the player's footprint lies inside the lens's view cone and range, which are the same angle and range as its searchlight, so the footprint test matches the lit floor pool. Finally, sight raycasts must reach the head or torso sample, both of which follow the current stance height. F3 adds the query wireframes, the clipped cone and the last sight ray for every camera.

The Unity Starter Assets first person controller and Input System are used. A regular camera follows the controller's camera target, with no Cinemachine dependency.

## Source

- `Assets/RayOEZoning/Runtime/`: stealth state, spatial detection, stance, the per-run puzzle (`PuzzleCatalog`, `ClueDirector`, `ClueSpot`, `ClueWriter`, `Lettering`), interface (`StealthPresentation`), flashlight reveal (`HiddenClue`, `RevealInk` shader) and visuals (searchlight shafts, coverage outline, `PlayerBodyVisual` mannequin body with arm IK, held flashlight).
- `Assets/RayOEZoning/Art/`: materials, generated textures and the `LightShaft` shader.
- `ASSET_CREDITS.md`: asset sources and licenses.

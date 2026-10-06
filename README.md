<h1 align="center">Gameplay: Laser Room</h1>
<p align="center"><strong>Elkan Ainer M. La Madrid · 12413739 · GAMEDEV S01</strong><br>De La Salle University · Unity 6000.6.0f1 · Universal Render Pipeline 17.6.0</p>

A short survival game seen through a fixed CCTV camera. Dodge changing laser patterns for 90 seconds, read the floor lights to choose useful panels and avoid traps, then cross the exit after its blue grid shuts down.

## Gameplay video

[Compressed gameplay presentation and demo (MP4, 88 MiB)](Demo/LaserRoom-demo.mp4)

The compressed copy is 1080p at 30 fps and is stored directly in Git.

[Original gameplay presentation and demo (MP4)](Videos/Laser-Room-Demo.mp4)

The original 1080p recording runs for about 13 minutes and is stored with Git LFS. Download it from the linked file page. Cloning the recording requires Git LFS.

## Play

1. Clone the `laser-room` branch:
   ```bash
   git clone --branch laser-room --single-branch https://github.com/ainere/GAMEDEV.git
   ```
2. Add the cloned `GAMEDEV` folder to Unity Hub and open it with **Unity 6000.6.0f1**.
3. Open `Assets/LaserCorridor/Scenes/LaserCorridor.unity` and press **Play**.

The branch contains the editable Unity project. It does not include a standalone executable.

| Input | Action |
| --- | --- |
| WASD | Move through the corridor |
| Space / Enter | Start from Ready; Space also jumps during a run |
| Left or right Ctrl | Crouch |
| Escape | Pause or resume |
| R | Reset the run |

## How a run works

- Start with 100 health and survive the 90-second countdown. The exit grid is lethal until the timer ends; then cross the corridor to win.
- Moving lasers use 16 pattern families with four authored designs each. Their 64 designs and mirrored orientations form a pool of 128 layouts. New runs use a fresh sequence; recently used families and variants are avoided when other choices are available.
- Patterns and speeds unlock and intensify as time passes. The safety validator checks each selected encounter for a continuous dodge route before it is used.
- Floor plates have identical geometry. Steady lights mark healing, shields or slower lasers; flickering lights mark movement slowdown or shock traps.
- The centered CCTV HUD shows health, time, active effects and run feedback. Ready, pause, death and win overlays use the same camera treatment.

## Project map

| Path | Contents |
| --- | --- |
| `Assets/LaserCorridor/Scenes/LaserCorridor.unity` | Playable scene |
| `Assets/LaserCorridor/Runtime/` | Run, player, laser, floor-panel, camera, audio and HUD logic |
| `Assets/LaserCorridor/Data/` | Run settings, effects and authored laser patterns |
| `Assets/LaserCorridor/Tests/EditMode/` | Automated gameplay, geometry and schedule tests |
| `ASSET_CREDITS.md` | Asset sources, licenses and visual reference |
| `ASSET_INVENTORY.csv` | Retained source-file paths and SHA-256 hashes |
| `DESIGN_REVIEW.md` | Design rationale |
| `VERIFICATION.md` | Test results and verification limits |

## Verification

The retained Edit Mode suite has **36 passing tests**, including 10,000 seeded safety checks covering all 64 laser designs and 128 mirrored layouts. Live selection checks reached all four opening families and all 48 advanced family/variant combinations. Run the suite through Unity's Test Runner.

A muted automated HUD check passed five states at 720p, 1080p and ultrawide resolutions. The reviewed captures isolate the HUD; human difficulty, audible output and hardware performance were not measured. See [VERIFICATION.md](VERIFICATION.md) for the scope and results.

## References

| Reference | Project use |
| --- | --- |
| [Unity Character Controller manual](https://docs.unity3d.com/6000.6/Documentation/Manual/class-CharacterController.html) | Player movement and collision |
| [Unity Input System 1.19 manual](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/index.html) | Keyboard input |
| [Unity UI (uGUI) 2.6 manual](https://docs.unity3d.com/Packages/com.unity.ugui@2.6/manual/index.html) | In-game interface |
| [Universal Render Pipeline manual](https://docs.unity3d.com/6000.6/Documentation/Manual/urp/urp-introduction.html) | Scene rendering |
| [Unity Test Framework 1.8 manual](https://docs.unity3d.com/Packages/com.unity.test-framework@1.8/manual/index.html) | Edit Mode regression tests |
| [Quaternius Universal Animation Library](https://quaternius.itch.io/universal-animation-library) | Character model and animation source |
| [Kenney Sci-fi Sounds](https://kenney.nl/assets/sci-fi-sounds) | Ambient and gameplay sound source |
| [Kenney Space Station Kit](https://kenney.nl/assets/space-station-kit) | License and provenance retained; its content is not used |
| [Meristation: Resident Evil 4 Remake — Separate Ways](https://as.com/meristation/avances/analisis-de-separate-ways-los-caminos-separados-de-ada-wong-y-leon-en-resident-evil-4-remake-n/) | Visual inspiration only; no franchise assets are included |

The [asset credits](ASSET_CREDITS.md) document the included source packs and licenses. The project uses the Unity-bundled Liberation Sans font; its OFL license is included at [`Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt`](<Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt>).

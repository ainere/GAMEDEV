# Asset Credits and References

The project combines locally authored game assets with the sources listed below. Original license files are included beside the applicable assets. [`ASSET_INVENTORY.csv`](ASSET_INVENTORY.csv) records the 33 retained imported or bundled source files, their project paths, sizes and SHA-256 hashes.

| Source | License and included files | Use in the game |
| --- | --- | --- |
| [Universal Animation Library — Quaternius](https://quaternius.itch.io/universal-animation-library), Standard edition | CC0 1.0 Universal. [`License.txt`](<Assets/ThirdParty/Quaternius/Universal Animation Library[Standard]/License.txt>) and [`README.txt`](<Assets/ThirdParty/Quaternius/Universal Animation Library[Standard]/README.txt>) are included. | The Unity `UAL1_Standard.fbx` supplies the player model and animation clips. The project uses a local Animator controller, masks and additive hit-reaction clips derived from the source animations. The source FBX is unchanged. |
| [Sci-fi Sounds — Kenney](https://kenney.nl/assets/sci-fi-sounds), version 1.0 | CC0. [`License.txt`](Assets/ThirdParty/KenneySciFiSounds/License.txt) is included. The inventory lists all 14 retained sound files. | Fourteen scene-assigned files are retained. Runtime cues use room and engine ambience plus selected launch, impact, trap, exit and death sounds. |
| Liberation Sans, bundled with Unity TextMesh Pro | SIL Open Font License 1.1. The [`font license`](<Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt>) is included with the bundled font and TextMesh Pro resources. | The font's signed-distance-field asset is used by the CCTV HUD. |
| [Space Station Kit — Kenney](https://kenney.nl/assets/space-station-kit), version 1.0 | CC0. [`License.txt`](Assets/ThirdParty/KenneySpaceStationKit/License.txt) is retained with its provenance. | No kit content is used in the scene; only the license file is included. |

## Local project work

The corridor architecture, floor plates, vault door and keypad, materials, shaders, CCTV interface, laser layouts, gameplay code and scene setup were authored for this project. Quaternius animation source clips are configured through project-owned controller and mask assets. No Resident Evil imagery, audio, models or textures are included.

The visual direction was informed by [Meristation's Separate Ways article](https://as.com/meristation/avances/analisis-de-separate-ways-los-caminos-separados-de-ada-wong-y-leon-en-resident-evil-4-remake-n/). It is a reference for mood and composition only; all game content is created from the sources identified above or locally authored.

## Download archive provenance

These SHA-256 values identify the source archives used for the two included packs and the separately retained Space Station Kit license:

| Archive | SHA-256 |
| --- | --- |
| Quaternius Universal Animation Library, Standard | `cc73fc4e495b82958207316596317a3f40b9fa38065bde1027937452da537724` |
| Kenney Sci-fi Sounds | `119340f351a5098ad814f78719438c0da355a9ce8a4c8a3af6a8d48aa3d49e04` |
| Kenney Space Station Kit | `215e79bd5415cff93665183390f0343ed9acf87780306331013b78520170c6d8` |

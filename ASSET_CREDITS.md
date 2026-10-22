# Asset credits

Ray-OE Zoning uses assets already present in this project plus content authored locally for it. No new asset packs or packages were downloaded. Original license files stay beside their assets. [ASSET_INVENTORY.csv](ASSET_INVENTORY.csv) lists the retained imported source files with their sizes and SHA-256 hashes; Unity metadata files are not part of that inventory.

| Source | License and provenance | Use in Ray-OE Zoning |
| --- | --- | --- |
| Locally authored content | Written for this coursework project | All laboratory architecture, props (lockers, benches, fume hoods, freezers, dewars, compute racks, research chamber, vault), CCTV housings, laser emitter columns, the gloved hand and tactical flashlight, and the interface are built from Unity primitives by `ZoningBuilder`. Surface textures (speckled vinyl, ceiling tile, wall panel), monitor screens, pictogram signs and hazard stripes are generated as PNGs by the same code. The `LightShaft` shader for the visible CCTV beams is written for this project. |
| Original Laser Corridor project | Locally authored coursework assets and code | The level the stealth game was converted from. Its source scene, scripts and materials remain in `Assets/LaserCorridor/`. |
| Unity Starter Assets | Existing coursework reference project, `Assets/Starter Assets/Runtime` | First person controller, input helper, input-actions asset and assembly definition, copied with their original file metadata. A small controller reset hook supports retries. A regular camera follows the camera target; no Cinemachine dependency was added. |
| [Universal Animation Library — Quaternius](https://quaternius.itch.io/universal-animation-library), Standard edition | CC0 1.0; [included license](<Assets/ThirdParty/Quaternius/Universal Animation Library[Standard]/License.txt>) | The mannequin mesh and its Idle, Walk, Sprint, Crouch Idle and Crouch Forward clips drive the visible first person body (legs and boots when looking down). Materials are replaced with the investigator's dark clothing. |
| Liberation Sans, bundled with Unity TextMesh Pro | SIL Open Font License 1.1; [included license](<Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt>) | Interface key caps, keypad digits and record text. |
| [Sci-fi Sounds — Kenney](https://kenney.nl/assets/sci-fi-sounds) and [Space Station Kit — Kenney](https://kenney.nl/assets/space-station-kit) | CC0; license files kept under `Assets/ThirdParty/` | Not used by the scene. The inherited sound files and audio mixer were removed earlier. |

The game is silent: the scene has no AudioSource or AudioListener and project audio is disabled.

The retained Quaternius source archive has SHA-256 `cc73fc4e495b82958207316596317a3f40b9fa38065bde1027937452da537724`. No Resident Evil imagery, models, textures or audio are included.

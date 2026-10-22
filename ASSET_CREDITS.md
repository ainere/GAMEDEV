# Asset credits

Ray-OE Zoning uses assets already present in this project plus content authored locally for it. No new asset packs or packages were downloaded. Original license files stay beside their assets.

| Source | License and provenance | Use in Ray-OE Zoning |
| --- | --- | --- |
| Locally authored content | Written for this coursework project | All laboratory architecture, props (lockers, benches, fume hoods, freezers, dewars, compute racks, research chamber, vault, clipboards, sticky notes, whiteboard, control panel), CCTV cameras, laser emitter columns, the tactical flashlight, and the interface are built from Unity primitives by the level builder, which is not part of this branch. The single-stroke handwriting and stencil font, the 5×7 dot-matrix font and the sixteen clue symbols are authored as point data in `Lettering.cs`. Surface textures (speckled vinyl, ceiling tile, wall panel), monitor screens, pictogram signs and hazard stripes are generated as PNGs by the same level builder. The `LightShaft` shader for the visible CCTV beams and the `RevealInk` shader for flashlight-revealed writing are written for this project. |
| Unity Starter Assets | Existing coursework reference project, `Assets/Starter Assets/Runtime` | First person controller, input helper, input-actions asset and assembly definition, copied with their original file metadata. A small controller reset hook supports retries. A regular camera follows the camera target; no Cinemachine dependency was added. |
| [Universal Animation Library — Quaternius](https://quaternius.itch.io/universal-animation-library), Standard edition | CC0 1.0; [included license](<Assets/ThirdParty/Quaternius/Universal Animation Library[Standard]/License.txt>) | The mannequin mesh is the visible first person body, and its own right hand holds the flashlight. Its Idle, Walk, Sprint, Crouch Idle and Crouch Forward clips drive the body; the T-pose clip supplies straight fingers and the Idle Torch clip's grip angles are reused for the hand. Materials are replaced with a dark suit. |
| Liberation Sans, bundled with Unity TextMesh Pro | SIL Open Font License 1.1; [included license](<Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt>) | Interface text, keypad digits and the terminal's key labels. |

The game is silent: the scene has no AudioSource or AudioListener and project audio is disabled.

The retained Quaternius source archive has SHA-256 `cc73fc4e495b82958207316596317a3f40b9fa38065bde1027937452da537724`. No Resident Evil imagery, models, textures or audio are included.

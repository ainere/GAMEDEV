# Asset credits

Ray-OE Zoning uses existing local assets and locally authored game content. No new asset packs or production packages were downloaded for this adaptation. Original license files remain beside their assets. [ASSET_INVENTORY.csv](ASSET_INVENTORY.csv) lists the retained imported source files and their current sizes and SHA-256 hashes; Unity metadata files are not part of that inventory.

| Source | License and provenance | Use in Ray-OE Zoning |
| --- | --- | --- |
| Original Laser Corridor project | Locally authored coursework assets | The blast-door/keypad assembly and selected material assets are adapted from the original level. The new modular annex architecture, server cabinets, camera rails, surveillance domes, maintenance markings, interface and stealth systems are authored locally. Original source assets remain in the project. |
| Unity Starter Assets | Existing coursework reference project, `Assets/Starter Assets/Runtime` | First person controller, input helper, input-actions asset and assembly definition are copied with their original file metadata. A small controller reset hook supports retries. A regular camera follows its camera target; no Cinemachine dependency was added. |
| Liberation Sans, bundled with Unity TextMesh Pro | SIL Open Font License 1.1; [included license](<Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt>) | The SDF font renders the interface and world clues. Unity has serialized the font for the current editor version. |
| [Universal Animation Library — Quaternius](https://quaternius.itch.io/universal-animation-library), Standard edition | CC0 1.0; [included license](<Assets/ThirdParty/Quaternius/Universal Animation Library[Standard]/License.txt>) | The original character and animation source remain in the base project. They are not used by the new first person player. |
| [Sci-fi Sounds — Kenney](https://kenney.nl/assets/sci-fi-sounds) and [Space Station Kit — Kenney](https://kenney.nl/assets/space-station-kit) | CC0; license files retained under `Assets/ThirdParty/` | No content from either pack is used by the new scene. All inherited sound files and the audio mixer were removed. |

The game is silent: the new scene has no AudioSource or AudioListener, project audio is disabled, and master volume is zero.

The retained Quaternius source archive has SHA-256 `cc73fc4e495b82958207316596317a3f40b9fa38065bde1027937452da537724`. No Resident Evil imagery, models, textures or audio are included.

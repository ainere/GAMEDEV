# Particles: Super Sentai Transform!

Three particle effect showcases built with Unity's built-in Particle System (Shuriken) in URP, each staged with posable characters that move in time with the effects.

| Scene | What happens |
| :--- | :--- |
| Super Sentai Transform | Five rangers call their transformation, spiral into their suits and strike team poses in front of a five-colour explosion. |
| Elemental Bending | One bender performs water, earth, fire and air techniques in a courtyard at dusk, then bows. |
| Final Boss | An armoured figure on a throne in a dark hall wakes up and powers up a layered aura, seen from a low camera. |

## Open and play

1. Open this project in Unity **6000.6.0f1**.
2. Open any scene in `Assets/SentaiTransform/Scenes`, `Assets/ElementalBending/Scenes` or `Assets/FinalBoss/Scenes`.
3. Enter Play mode. Each sequence starts immediately and replays automatically.

| Key | Action |
| :--- | :--- |
| 1 or Space | Restart the sequence |
| L | Toggle automatic replay |
| R | Reset the rangers to civilian clothes (Super Sentai only) |
| Tab | Next scene (F1, F2, F3 jump to a scene) |
| H | Hide or show the hotkey help (for recording) |

## How it is built

The characters are stylized, posable figures: an 18-joint skeleton with shaped parts on each joint, dressed as a ranger (with a civilian outfit and a suit), a bender or an armoured boss. Poses are written in choreography terms, such as "raise the right arm 90 degrees forward and bend the elbow 40 degrees", and each scene has a timeline of poses that the effects are timed against.

Every effect is a standard Particle System in its scene, so its settings can be inspected and adjusted in the Inspector.

## Project contents

- [Characters](Assets/Characters/Scripts): skeleton, pose system, pose playback and outfit swapping.
- [Showcase](Assets/Showcase): the scene switcher and HUD toggle, plus the generated meshes and materials the scenes use.
- [Super Sentai](Assets/SentaiTransform), [Elemental Bending](Assets/ElementalBending) and [Final Boss](Assets/FinalBoss): each has its scene, its choreography and a controller that runs the sequence.

## Assets

Particle textures come from Kenney's Particle Pack and Brackeys' VFX Bundle (flipbooks), both CC0. See [ASSET_CREDITS.md](ASSET_CREDITS.md). Everything else, including the characters, sets, materials and effects, was made for this project.

Find the other course submissions in the [GAMEDEV directory](https://github.com/ainere/GAMEDEV/tree/main).

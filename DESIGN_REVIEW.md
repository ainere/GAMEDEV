# Ray-OE Zoning design

Ray-OE Zoning adapts the corridor into a short first person stealth puzzle. Its industrial architecture is extended to 28 m, with tall opaque cover and three moving scanning volumes. Existing wall, floor, ceiling, entrance and exit meshes retain their original material references.

The player reveals four physical glyph objects with a 50-degree, 8 m flashlight. Continuous illumination within 3 m records each clue after 0.8 seconds. World objects immediately hide when flashlight, cone, range or unobstructed sight fails; recorded field notes remain available.

The clue mappings are Eye=2, Moon=6, Gate=4 and Crown=9. Eye is first, Gate is not last, and Crown immediately follows Moon. These constraints uniquely determine 2469. There are no collectible-key or discovery-count prerequisites at the keypad.

Each scanner performs its own real spatial query followed by obstruction raycasts. Sphere, oriented box and horizontal capsule fields move laterally on repeating patrols. One shared alert meter fills after 1.35 seconds of confirmed exposure and drains over 1.5 seconds when sight is lost. Capture gently returns the player to the entrance, resets the physical run, and preserves field notes.

The Starter Assets first person controller uses a regular camera, 2.2 m/s movement and disabled sprint/jump. Cover, timing and observation are the intended ways through the level. Notebook, keypad and pause modes freeze patrols and alert without clearing them.

The unsettling mood comes from confined sightlines, dim cyan architecture, isolated light pools and visible surveillance machinery. There are no jump scares. The game and demo have no audio, following the final user instruction.

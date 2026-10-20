# Ray-OE Zoning design

Ray-OE Zoning is a CCTV infiltration mission through a 28 m security annex. The player enters through a maintenance access point, passes the Perimeter, Signal and Records sectors, decodes the archive terminal, and crosses the protected archive entrance. Numbered sector portals, equipment identifiers and a continuous cyan guide line establish one readable route.

The environment uses graphite service frames, navy structural steel, pale equipment enamel and a dark service floor. Server cabinets provide opaque cover and give the corridor a functional purpose. Camera domes hang from moving ceiling carriages; stems and rails connect the surveillance machinery to the architecture. Cyan identifies navigation and access, amber marks surveillance coverage, and red signals confirmed exposure. The original blast door is reused within the new archive entrance; the former room's dense ribbed architecture and blank cover slabs are replaced.

The player reveals four physical glyph objects with a 50-degree, 8 m flashlight. Continuous illumination within 3 m records each clue after 0.8 seconds. World objects immediately hide when flashlight, cone, range or unobstructed sight fails; recorded field notes remain available.

The clue mappings are Eye=2, Moon=6, Gate=4 and Crown=9. Eye is first, Gate is not last, and Crown immediately follows Moon. These constraints uniquely determine 2469. There are no collectible-key or discovery-count prerequisites at the keypad.

Each camera performs its own real spatial query followed by obstruction raycasts from its mounted optical lens. Sphere, oriented box and horizontal capsule fields move laterally on repeating patrols. Panoramic domes communicate surveillance around each zone, while amber floor footprints show its coverage. Exact 3D wire volumes and per-camera sight diagnostics appear in F3 view. One shared alert meter fills after 1.35 seconds of confirmed exposure and drains over 1.5 seconds when sight is lost. Capture gently returns the player to the entrance, resets the physical run, and preserves field notes.

The Starter Assets first person controller uses a regular camera, 2.2 m/s movement and disabled sprint/jump. Cover, timing and observation are the intended ways through the level. Notebook, keypad and pause modes freeze patrols and alert without clearing them.

Mission copy, field notes, access terminal and results use the same security-annex language. The atmosphere comes from confined sightlines, restrained lighting and visible surveillance machinery. There are no jump scares or audio.

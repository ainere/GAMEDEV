# Ray-OE Zoning design

The level is a maintained research laboratory after hours. The player is breaking in to reach a prototype sample in the archive vault; there is no item theft mechanic, but the vault, the security desk and the CCTV signage set up the heist. Surfaces stay clean and equipment stays powered: monitors show live data, freezers glow, racks blink. The space reads as a working facility with the lights switched off, not an abandoned one.

## Layout

| Zone | Extent | Notes |
| --- | --- | --- |
| Staff entry | z 0–11, 8 m wide | Lockers, a night security desk with camera-feed monitors, a supply-shelf divider. First record on the notice wall by the entrance. |
| Analytical laboratory | z 11–29, 10.5 m wide | Long bench islands run with the route, fume hoods, a tall storage island, an inspection workstation with the clipped record, an optional crouch pipe chase. |
| Specimen cold storage | z 29–45, 8.5 m wide | Two freezer rows form a serpentine. A prone-only cryo-line crawlspace cuts through the second row. Record on a cold-store controller. |
| Secure research | z 45–59.5, 9 m wide | Compute racks with live activity lights, a glass research chamber, the archive terminal and the laser-gated blast door. |
| Archive vault | z 59.5–64.5 | Drawer banks and the glowing sample cassette under a spotlight. |

Doorways alternate sides (left, right, left, centre), so there is never a straight sightline down the building. The route to the archive door is about 60 m, a modest expansion of the original 48 m hall.

## Light and darkness

Ambient light is low but never zero, so silhouettes stay readable. Each room has at most one or two dim night lights. Emissive monitors, LEDs, exit and zone pictograms, freezer interiors and the amber beacons at the archive door give orientation. The held flashlight is warm, bright and 18 m long, which makes it the main way to see detail and the only way to reveal records. It dims smoothly on close surfaces so records stay legible at reading distance.

CCTV searchlights are cold white and the brightest lights in the level. Their inner and outer spot angles are nearly equal, so each floor pool has a crisp edge. A soft volumetric shaft makes the beam visible in the air, and each radial ray is clipped where it meets cover, so the shaft stops at a cabinet just like the light does. A thin outline traces the floor pool. The shaft, outline and light turn red while the camera sees the player.

## Detection

A camera sees the player when all three hold:

1. Its real `CheckSphere`, oriented `CheckBox` or `CheckCapsule` overlaps the player's collider.
2. The player's footprint is inside the lens's view cone and range. These match the searchlight's spot angle and range, so this is the lit floor pool.
3. A raycast from the lens reaches the head or torso sample. Both samples follow the current CharacterController height.

The footprint rule fixes a mismatch the previous version had: near a ceiling camera, a standing player's head could enter the 3D cone while their feet were still outside the lit area, so they were caught without ever stepping into the light. Now the rule a player can see on screen is the rule the game uses.

| Camera | Query | View angle | Range | Pan |
| --- | --- | --- | --- | --- |
| 1 Staff entry corner | Sphere | 64° | 8.5 m | ±30°, 7.2 s |
| 2 Lab right wall | Box | 38° | 11 m | ±26°, 8 s |
| 3 Lab ceiling, cold-store door | Capsule | 50° | 10 m | ±30°, 8.5 s |
| 4 Cold storage right wall | Sphere | 68° | 8 m | ±22°, 7.8 s |
| 5 Archive wall corner | Box | 34° | 11.5 m | ±24°, 8.8 s |
| 6 Secure research ceiling | Capsule | 46° | 9.5 m | ±25°, 8 s |

Confirmed exposure fills the shared meter over 1.35 seconds and drains it over 1.5 seconds. Capture returns the player to the entrance standing and resets the physical run while keeping notes.

## Stances

Standing, crouched and prone heights are 1.8, 1.1 and 0.56 m, with speeds of 2.35, 1.35 and 0.70 m/s. Forward standing sprint reaches 4.55 m/s. Expanding to a taller stance needs clear headroom. Bench service spines (1.56 m) hide a crouched head; tall cabinets, freezers and racks hide a standing player. The crawlspace in cold storage is passable only prone.

## Player presentation

A gloved hand holds a modelled tactical flashlight, and the beam comes from its actual muzzle. Looking down shows the legs and boots of the CC0 Quaternius mannequin, blended between idle, walk, sprint, crouch-idle and crouch-walk clips from the real stance and speed; its upper body is collapsed so it never blocks the view. When prone, the body lies behind the camera.

## Puzzle and interface

The clue mappings are Eye=2, Moon=6, Gate=4 and Crown=9. The ordering statements ("The Eye is first", "The Gate is not last", "The Crown comes immediately after the Moon") have exactly one solution, 2469. The keypad does not require all clues to be found first.

The interface uses icons and key caps: flashlight, notes and four record dots at bottom left, stance at bottom right, and the detection meter at bottom centre with caution and danger bands. Directional chevrons around the crosshair show which camera sees you. A red vignette rises with the alert. The only written words in the game are the discovered record content and key labels.

The archive grid runs between two emitter columns with visible ports. Unlocking switches the beams off and leaves the hardware in place.

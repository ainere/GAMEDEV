# Ray-OE Zoning design

The level is a maintained research laboratory after hours. The player is breaking in to reach a prototype sample in the archive vault; there is no item theft mechanic, but the vault, the security desk and the CCTV signage set up the heist. Surfaces stay clean and equipment stays powered: monitors show live data, freezers glow, racks blink. The space reads as a working facility with the lights switched off, not an abandoned one.

## Layout

| Zone | Extent | Notes |
| --- | --- | --- |
| Staff entry | z 0–11, 8 m wide | Lockers, a night security desk with camera-feed monitors, a supply-shelf divider. Clue surfaces: wall plaque, sticky note on a locker, clipboard by the staff door. |
| Analytical laboratory | z 11–29, 10.5 m wide | Long bench islands run with the route, fume hoods, a tall storage island, a workstation with a pinboard, an optional crouch pipe chase. Clue surfaces: whiteboard, bench monitor, desk paper, pinboard clipboard, all readable crouched from the left lane. |
| Specimen cold storage | z 29–45, 8.5 m wide | Two freezer rows form a serpentine. A prone-only cryo-line crawlspace cuts through the second row. Clue surfaces: LCD control panel, freezer label, clipboard, wall stencil. |
| Secure research | z 45–59.5, 9 m wide | Compute racks with live activity lights, a glass research chamber with rail-mounted screens, the archive terminal and the laser-gated blast door. Clue surfaces: chamber screen, taped memo, sticky note on a rack. |
| Archive vault | z 59.5–64.5 | Drawer banks and the glowing sample cassette under a spotlight. |

Doorways alternate sides (left, right, left, centre), so there is never a straight sightline down the building. The route to the archive door is about 60 m, a modest expansion of the original 48 m hall.

## Light and darkness

Ambient light is low but never zero, so silhouettes stay readable. Each room has at most one or two dim night lights. Emissive monitors, LEDs, the exit sign, freezer interiors and the amber beacons at the archive door give orientation; there are no zone pictograms. The held flashlight is warm, bright and 18 m long, which makes it the main way to see detail and the only way to reveal writing. Its intensity follows the square of the distance to whatever its centre ray hits (full at 6 m), so a page at arm's length is lit about as brightly as a far wall and never washes out.

Each CCTV is a bullet camera: a grey housing with a sun shield, a dark lens bezel and a red recording diode, hung from a wall arm or a ceiling drop rod on a swivel above the pan axis, so the head turns under a fixed bracket.

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

The player's body is the CC0 Quaternius mannequin at full size, so its eyes sit at the camera's 1.65 m eye height, in a dark suit. It plays idle, walk, sprint and crouch clips from the real stance and speed, faces the view (the rig faces -Z, so it is turned 180°), and has its head collapsed so it never blocks the camera. Looking down shows the legs and feet.

The mannequin's own right hand holds the flashlight. `PlayerBodyVisual` solves the right arm with two-bone IK each frame after the animation, with the elbow pointing down and out, and turns the hand so the knuckles lie over the top of the barrel and the thumb side faces the tail. The fingers are reset straight (from the T-pose clip) and curled with the angles of the model's own `Idle_Torch_Loop` grip; the thumb is aimed across the curled index finger. The barrel is placed at the measured centre of the middle finger's curl, so it sits inside the fist. The flashlight is held lower while looking up and further out while looking down, and if the arm still cannot reach, the flashlight moves to the hand instead of floating. The beam always comes from the flashlight's muzzle and follows the view. A faint fill light that affects only the player layer keeps the hand readable in the dark.

## Puzzle and interface

`ClueDirector` rolls a new run when the scene starts and again on "play again". `PuzzleCatalog.Generate` picks four distinct symbols from sixteen (Eye, Moon, Gate, Crown, Star, Sun, Key, Bell, Fish, Leaf, Bolt, Anchor, Drop, Spiral, Flame, Tree) and a random digit for each. It then builds a minimal set of ordering statements (place, not-first/last, right-after, before) with exactly one solution, and splits it into two order notes so neither is enough alone. The code digits and the order are separate pieces of evidence.

Fourteen `ClueSpot` surfaces exist, three or four per room. Each run puts one code digit in every room and the two order notes in two different rooms, on surfaces with room for sentences. `ClueWriter` lays each clue out in lettering that suits the surface, all built as meshes from a locally authored single-stroke font: clean monoline for engraved plaques, label tape and typed memos; per-letter jitter for ballpoint, pencil and marker handwriting; bridged strokes for spray stencils; and a 5×7 dot-matrix font for monitors and the LCD panel. Symbols are vector outlines drawn in the same styles, or rasterised onto the dot grid for screens.

All hidden writing uses the `RevealInk` shader. `RevealInk.Push` sends the held flashlight's position, direction, range and cone to the GPU every frame, and each pixel of ink is drawn only inside that cone, fading at its edge, so the beam reveals exactly the strokes it covers. `HiddenClue` still gates the whole clue on the flashlight being on, in range, near the cone (allowing for the writing's size) and with a clear line from the lamp. The ink is unlit, so the hotspot cannot wash it out. There is no notebook and no automatic transcription; the player keeps the code in their head. The keypad does not require any clue to be found first. `--puzzle-seed N` on the command line fixes a run for testing.

The interface is deliberately minimal. A small crosshair dot is the only permanent element. The detection ring is hidden until a camera sees the player and fades out once the meter drains. It is a thin (about 4 px), faint track that fills from the bottom up both sides with a smooth white-amber-red gradient at roughly half opacity, ending in the same red as the vignette that rises with the alert. When the meter is full, a small letter-spaced "SPOTTED" label appears under the ring. The vignette starts at a third of the meter and stays soft (at most about a quarter opacity at the screen corners). Both the ring and its gradient are generated sprites drawn by radial-filled UI Images, so the circle stays smooth at any window size. Small arrows outside the ring point at the cameras that currently see the player. The only prompt is "E Use terminal" near the keypad. The keypad and end screen are plain dark overlays. There is no pause menu, start-up key legend or notebook.

While the player's eye is inside a camera's beam, that beam's volumetric shaft fades out (seen from inside, it read as flat red sheets); the lit pool, outline, ring and vignette carry the warning.

Scene objects use plain names in the hierarchy (`Lockers`, `Desk`, `Freezer`, `Server`, `Server (1)`, `Monitor`, `CCTV`). Simple shapes keep Unity's default primitive names (`Cube`, `Cylinder (2)`), and invisible blockers are `Collider`.

The archive grid runs between two emitter columns with visible ports. Unlocking switches the beams off and leaves the hardware in place.

The supply divider in the staff entry is 2.65 m tall, so the entry camera cannot see over it to a standing player near the spawn.

A local audit lists coplanar, same-facing overlapping faces of different boxes and quads (z-fighting). Jambs, chamber posts, shelf boards and uprights, worktop ends, door rails and paper-thin layers were offset until only faces hidden under the chamber roof remain.

Every prop sits on a surface or against a wall: monitors stand on desks or hang from the chamber's rail, wall items sit flush (clearing the 8 mm wayfinding stripe), pipes, the cable tray and the duct casing hang on rods, beacons sit on wall brackets, and the archive terminal faces the room.

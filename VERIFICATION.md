# Ray-OE Zoning verification

## Third pass: mannequin body and quieter alert (2026-10-22)

The Quaternius mannequin is back as the first person body, with its own right hand holding the flashlight through arm IK. The detection ring is thinner and fainter, "DETECTED" became a small "SPOTTED" label, and the red edge glow is softer.

- **EditMode: 79 passed, 0 failed. PlayMode: 11 passed, 0 failed.** The route playback was not re-run; collision, cameras and detection are unchanged.
- Still renders checked the grip from six sides around the flashlight and in first person at look angles from 60° up to 70° down, standing and crouched. In every view the hand's measured grip point lands exactly on the flashlight's grip (gap 0.000 m). The render tool now draws a baked copy of the skinned mesh, checked to within 2 mm of the finger bones, because without a player loop the skinned mesh drew an earlier pose.
- A Windows review build ran the HUD poses with no exceptions: the hand is visible holding the flashlight in normal play, and the ring and SPOTTED label show at partial and full alert.

## Second remodel pass (2026-10-22)

Removed the notebook, reading prompts, zone pictograms and the first person body; added per-pixel flashlight reveal, flashlight auto-exposure, a sprite-based detection ring and a rebuilt glove; fixed z-fighting, beam sheets seen from inside, and a sightline over the supply divider to the spawn.

- **EditMode: 79 passed, 0 failed. PlayMode: 11 passed, 0 failed.** Tests changed with the API: reveal-only `HiddenClue.Evaluate`, an edge-lit clue staying on for the shader, the terminal as the only prompt, and reveal through capture and retry. The route playback was not re-run.
- The standing coverage map shows the whole staff entry up to the divider outside camera coverage; coverage behind the divider is unchanged.
- The z-fighting audit went from 77 visible overlaps to 10, all of them faces pressed under the chamber roof.
- Review renders show partial reveal on the plaque and clipboard, close-up paper no longer blown out, the new glove from several angles, the vault jamb, and doorways without pictograms. Review-build HUD captures show the ring partly and fully filled, with no exceptions in the player log.

## Remodel revision (2026-10-22)

This revision changed the puzzle, interface, CCTV models, first person body and flashlight, prop placement and hierarchy names. Collision geometry, camera placement, sweep timing and detection rules were left as they were. At the user's direction the gameplay suites and route playback were **not** re-run for this revision.

- All runtime, editor, test and local QA assemblies compile in Unity 6000.6.0f1 with no errors.
- `ZoningBuilder.Create` rebuilt the scene without errors: six CCTV cameras and 14 clue surfaces.
- Still renders reviewed every clue surface with both a code digit and an order note, each CCTV close up, the security desk, lab benches, workstation, chamber screens, server racks and cable tray, pipes, the archive terminal and beacons, and the body and hand looking down while standing and crouched.
- A Windows review build captured the real HUD: idle crosshair, the detection ring partly and fully filled with its direction marker, DETECTED on capture, the noted toast, notebook, keypad and end screen. The build rolled a different code from the scene's preview seed, so the per-run roll works. No exceptions in the player log.
- Standing and crouched camera-coverage maps place every clue surface's reading position outside camera coverage; the four lab surfaces need a crouch, like the previous lab record.
- Tests were updated for the new API, not run: six active clues per run, every clue surface readable from its approach, and 200 rolled puzzles each with exactly one solution and neither order note sufficient alone.

The results below are from the previous revision (commit `b31acc6`).


Verified on 2026-10-22 with Unity 6000.6.0f1 on Windows 11, after the dark-laboratory redesign. These results replace the earlier three-camera and first six-camera reports; neither of those applies to this level.

## Automated tests

- **EditMode: 79 passed, 0 failed.** This includes the stealth, detection, stance and authored-scene checks plus the 36 retained Laser Corridor tests.
  - The authored scene has one first person controller and MainCamera, complete references, six cameras covering all three query shapes, a view-angle spread above 20°, four records, no missing or legacy gameplay scripts, and no AudioSource or AudioListener.
  - Every searchlight shares its camera's angle and range, casts shadows, and stays on the lens through the pan cycle.
  - Real `CheckSphere`, oriented `CheckBox` and `CheckCapsule` queries detect the player collider, reject unrelated geometry, and respect raycast occlusion.
  - New: a footprint just inside a camera's half-angle is seen and one just outside is not. Close under a mounted lens, a player whose head is inside the 3D cone but whose feet are outside the lit pool is not seen; the same player standing in the pool is.
  - Crouching drops the head and torso samples behind waist-high cover without changing the broadphase overlap.
  - Each of the four records can be lit and read from its authored approach. The clue ordering has exactly one solution (2469).
- **PlayMode: 11 passed, 0 failed.** These cover shared exposure and decay, modal freezes, capture and retry, retained notes, keypad validation and keyboard digits, clue reading distance, C/Z stance toggles, and Shift sprint through the real CharacterController. The sprint test Astra left unverified passes.

Run the suites through Unity's Test Runner.

## Route verification

A local design simulator (not part of the submitted source) evaluated the real scene's detectors along the intended route at 50 Hz and chose camera-phase timing that still holds with a 0.45 s late start and 12% slower movement. The simulator route passes in 72.1 s with all four records.

The same route then ran in a Windows player build using the real first person controller, input, stance changes, camera patrols and detection. It did not teleport, force notes or bypass detection; it only waited for camera pan phases at the marked waiting spots. Result:

- **PASS: archive reached in 72.4 s**, well under the three-minute target.
- All four records were read through the flashlight. The wrong code 1111 was rejected, then 2469 opened the grid and the door.
- Peak detection along the route was 0.39 of the meter; the player was never caught.
- Crouch (laboratory lanes), prone (cold-storage crawlspace) and standing timed crossings were all exercised.
- No exceptions in the player log.

## Builds

- The production Windows build (`Ray-OE Zoning → Build Windows`) succeeded with the local QA tooling excluded. It launched and ran for 25 seconds with no errors or exceptions in its log.
- Screenshots from a review build covered the HUD (detection meter, chevrons, vignette, key legend, pause, notebook, keypad, win screen), each room, the laser grid, record reveals, prone and crouched views, and the first person body. The rooms were also reviewed as still renders. Logs and images stay local under ignored folders.

## Silent output

The scene has no audio sources or listener and project audio is disabled.

## Limits

The route checks are automated playback, not a human difficulty study, and no performance benchmark was run beyond the development machine. A gameplay demo video is not included in this revision.

# Ray-OE Zoning verification

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

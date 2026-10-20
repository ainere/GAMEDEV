# Ray-OE Zoning verification

Verified on 2026-10-20 with Unity 6000.6.0f1 on Windows.

## Automated checks

- EditMode: 51 passed, 0 failed. This includes 15 stealth/detection/scene checks and 36 retained Laser Corridor tests.
- The scene checks require one first person controller and MainCamera, complete references, no legacy gameplay scripts or missing scripts, and no AudioSource or AudioListener.
- Both authored-scene checks passed again after the final material and lighting pass.
- Actual CheckSphere, oriented CheckBox and CheckCapsule queries detect the player collider, reject unrelated geometry, and respect opaque raycast occlusion.
- Mounted-camera checks use the actual elevated optical origin: low cover can be seen over, while tall cover blocks both player sight rays without changing query overlap. The authored scene requires a child optical origin on all three camera rigs.
- Flashlight tests cover on/off, cone, range, obstruction, shadow suppression and uninterrupted close-range reading. All four authored clue approaches are readable.
- The clue ordering has exactly one solution.
- PlayMode: 7 passed, 0 failed. Checks cover exposure aggregation and decay, modal input/state, capture/retry, retained notes, keypad validation, keyboard digits, clue-distance feedback and the exit.

Run the suites through Unity's Test Runner. XML reports, logs and capture tooling remain local under ignored folders.

## Windows runtime evidence

A Windows player completed a visible, automated input playthrough using the actual first person controller and production gameplay rules. It did not teleport, force note discovery, alter detection, or bypass the puzzle.

The final production Windows build succeeded with local inspection/capture tooling excluded and passed a separate launch smoke check without runtime errors.

The redesigned route confirmed a straight-ahead capture, a retry retaining its first clue, a successful timed route through all three moving CCTV areas, four naturally recorded clues, an elevated camera ray blocked by a server cabinet while the capsule query overlapped the player, wrong-code rejection, correct-code unlock and archive access. It reached the win state at z27.507 with four notes and zero alert.

The rendered annex architecture, camera rails, server cabinets, world clues and interface were visually reviewed at 720p and 1080p, including navigation readability with the flashlight off. Static visual inspection used separate camera placements and was not counted as a gameplay completion. Screenshots, logs and verification tools remain local.

## Silent output

The scene has no audio sources or listener, project audio is disabled, and master volume is zero. Verification runs are muted. No gameplay video is bundled with this revision.

## Limits

The successful route is automated controller playback, not a human difficulty study or a hardware performance benchmark. The game is intentionally silent. The original Laser Corridor checkout and its untracked presentation script were preserved.

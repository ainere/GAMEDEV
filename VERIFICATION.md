# Ray-OE Zoning verification

Verified on 2026-10-20 with Unity 6000.6.0f1 on Windows.

## Automated checks

- EditMode: 50 passed, 0 failed. This includes 14 stealth/detection/scene checks and 36 retained Laser Corridor tests.
- The scene checks require one first person controller and MainCamera, complete references, no legacy gameplay scripts or missing scripts, and no AudioSource or AudioListener.
- Actual CheckSphere, oriented CheckBox and CheckCapsule queries detect the player collider, reject unrelated geometry, and respect opaque raycast occlusion.
- Flashlight tests cover on/off, cone, range, obstruction, shadow suppression and uninterrupted close-range reading. All four authored clue approaches are readable.
- The clue ordering has exactly one solution.
- PlayMode: 7 passed, 0 failed. Checks cover exposure aggregation and decay, modal input/state, capture/retry, retained notes, keypad validation, keyboard digits, clue-distance feedback and the exit.

Run the suites through Unity's Test Runner. XML reports, logs and capture tooling remain local under ignored folders.

## Windows runtime evidence

The Windows build completed successfully. The final player also passed a launch smoke check with local capture tooling excluded. A visible, automated input playthrough used the actual first person controller and production gameplay rules. It did not teleport, force note discovery, alter detection, or bypass the puzzle.

The route confirmed a straight-ahead capture, a retry retaining its first clue, a successful timed route through all three moving detector areas, four naturally recorded clues, an obstruction ray blocked by cover while the capsule query overlapped the player, wrong-code rejection, correct-code unlock and escape. The earlier complete route reached the win state at z27.508 with four notes and zero alert.

World clues, notebook and keypad were visually reviewed at 720p; the final 93-second gameplay demo is 1080p/30 fps H.264. Static visual inspection used separate camera placements and was not counted as a gameplay completion. The local tool keeps those proof tiers separate.

## Silent output

Per the final user instruction, scene audio sources and its listener were removed, project audio is disabled, and master volume is zero. Verification runs are muted. The submitted MP4 contains a video stream only.

The final MP4 fully decoded without errors: 1920×1080, 30 fps, H.264, 92.93 seconds, 21,046,866 bytes (20.1 MiB). Brief captions identify the three physics queries while the recorded diagnostic view shows their volumes and rays.

## Limits

The successful route is automated controller playback, not a human difficulty study or a hardware performance benchmark. No audio quality claim applies: the game and demo are intentionally silent. The original Laser Corridor checkout and its untracked presentation script were preserved.

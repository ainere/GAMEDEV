# Verification

Verified with Unity **6000.6.0f1** on **2026-10-06**.

| Check | Result |
| --- | --- |
| Edit Mode regression suite | **36 passed, 0 failed, 0 skipped.** Covers run rules, damage and shields, floor contacts and lighting, audio limits, environment assemblies and laser schedules. |
| Laser schedule sampling | **10,000 seeded schedules:** 7,297 accepted and 2,703 safely rejected across 64 designs, 128 mirrored layouts, 10 challenge bands and 16 ordered speed pairs. |
| Pattern variety | Across 64 deterministic opening seeds, all four starter families appeared (15, 16, 16 and 17 selections). Full-unlock sampling covered 16 families, all 48 advanced family/variant pairs and 512 selected wave signatures. Both samples retained certified clearance and used no fallback encounter. |
| HUD layout | A muted automated audit passed Ready, effects, Pause, Death and Win states at 1280 × 720, 1920 × 1080 and 2560 × 1080. Active text fit; HUD-only captures were reviewed for alignment. |
| Earlier cleanup smoke check | Before the HUD and selection fixes, a muted automated run passed scene initialization, start, pause, exit opening, victory, damage, death and reset. |
| Project import and compile | Completed successfully after temporary verification helpers were removed. |

The HUD captures show the interface layout only; they do not verify full-scene appearance or replace human playtesting. No human gameplay session, audible review or hardware performance measurement is claimed. The project is intended to be opened and played in the Unity Editor; this branch does not include a standalone player build.

# Graph, quiz and turbine audio revision

- Removed only the added Voltage/Current graph switching tabs and current-curve code. RPM vs live DC voltage remains the normal graph; synchronized RPM, voltage and current table fields remain assigned on all five wind controllers. Math and export remain unchanged.
- Quiz answer highlights are discovered among inactive descendants of each clicked button (Correct/Incorrect and Glow Outline Right/Wrong). Manual per-answer Inspector highlight assignments are removed; cached references are not serialized. Existing 1.5-second delay and retry/advance behavior remain unchanged.
- Guided turbine AudioSources increased from 0.3 to 0.4. Free Mode uses 0.4 and now routes its created AudioSource to the existing SFX_3D mixer group controlled by Devices Volume. Loop and speed-dependent pitch unchanged.

## Verification

Both runtime and editor assemblies compiled successfully. Unity checked both correct/incorrect child activation on cloned copies of all 21 actual scene answer buttons, retained current-list references on five wind graph controllers, absence of switching tabs, and guided sound volume/routing. No interactive full-experiment replay or listening-level comparison was performed in this revision.

# V12 requested fixes

Each fix has its own local commit: 5, 20, 30, 32, 33, 40, 53, 57, 58, 59 and 60. Issue 35 has an investigation-only commit; no optimization was applied.

Validation:
- Actual transparency button: all 110 spline material slots toggle in Guided and Free Mode.
- Monitor selection: System default, four Graph markers, EXP1 initial lock and Free Mode round trip.
- Stacked plugs: 28 two-slot pools; actual endpoint clicks and removal for all four experiment profiles.
- EXP4 power: grid leads optional, both connect/remove live, other live-wire removal still blocked.
- Quiz: isolated runtime wrong/correct answer checks; wrong stays on the question and only the selected answer highlights.
- System capture: all four physical mouse callbacks start capture; generated monitor PNGs were visually checked. The automated check stops before the native save dialog.
- Current graph: live model current, aligned third column, synchronized three-column scrolling, preserved voltage/current points, and Clear.
- Final Main Menu -> Guided -> Free regression for EXP1–4: zero runtime errors, cable activation/rebuild/raycast/removal checks, and current/voltage switching.
- All 176 numerical samples exactly match the prior baseline. No CSV equations, cameras, storyboard sequence or existing connection endpoints changed.
- Runtime and Editor assemblies compile successfully after temporary test sources are removed from Assets. Existing compiler warnings remain.

Temporary test sources are archived under Tools, outside Assets. Do not copy them back into Assets unless intentionally running their one-shot checks. Generated mesh caches and unrelated old compiler outputs were restored. Previously saved scene state and the existing untracked V12FixRunner helper are preserved outside the fix commits.

Issue 35 remains open: repeated CSV parsing, text updates and connection checks are candidates; a profiler capture is needed to confirm the stutter's cause. See 35-investigation.md.

# Step 7 — UI scaling, resolution and graph validation

## Changes

- Standardized seven **screen-space** CanvasScaler components across Main Menu (three) and PV Lab (four): Scale With Screen Size, reference **1600x900**, Match **0.5**.
- World-space equipment/monitor canvases, their colliders, text alignment, knob/slider geometry, experiment steps and wiring were excluded.
- Main Menu camera fallback background is black so the standard scaling does not reveal the camera's old blue fill outside the menu artwork. Camera pose and clear mode are unchanged.
- Kept only the intended scaler fields/prefab overrides and camera background edit in the saved scene diff. Unrelated data/default-field serialization introduced by Unity's scene save was filtered out against the pre-step copies.

## Evidence

`Runtime.txt` records actual Game View dimensions and runtime scaler values. `Screenshots` captures Main Menu and EXP1 Guided startup at 1280x720,1600x900,1920x1080,2560x1440,3840x2160. PNG dimensions are checked separately in `ImageDimensions.txt`; filenames alone are not treated as proof. `Before` contains the pre-change 1280x720 views and scaler audit.

The two-column graph calculation check uses the active Guided controller:

- RPM/DC voltage plotted values equal its live source reading.
- Repeating an identical measurement does not duplicate a row.
- Export contains exactly the Speed (RPM) and DC voltage (V) columns and one data row (`GraphSample.csv`).
- Clear empties the plotted arrays and exported records.

These are API-level functional checks, not an actual click/drag test of every collider and scroll panel. Graph/table synchronization code remains unchanged. No alternate graph or controls were introduced.

## Review limits / remaining gate work

- Current screenshots are diagnostic evidence, not BEDO-approved visual baselines.
- The initial menu has a horizontal list: a partially visible fourth card is not a claim that four whole cards fit at once. The existing scroll behavior and document/objective flows still need full interactive review.
- The captures cover the initial EXP1 view, not every EXP2–4 subtitle, popup, document, graph and question state. All-screen clipping/overlap and control-state review remain pending.
- Browser zoom 90–125% cannot be certified without the WebGL build/module. Editor resolution changes are not browser zoom tests.
- Hover styling remains user-owned as previously requested; this step does not claim D-02 fully passes.

The temporary Editor tools are archived under Tools outside Assets. Import the resolution tool into an Editor folder, wait for compilation, then write `run` to `Temp/ui-validation.flag` to repeat the captures with saved scenes. The harness restores the previous Game View selection and opens PV Lab on exit. It never saves Play Mode scene state.

Follow-up: raised the 75 top-level EXP1–4 subtitle panels to Y=110 reference pixels to clear the bottom navigation. Text, size, children, notifications and experiment bindings remain unchanged. The final resolution run repeats the captures after this correction. Full dynamic subtitle/popup and all-screen review still remains required before D-03 acceptance.

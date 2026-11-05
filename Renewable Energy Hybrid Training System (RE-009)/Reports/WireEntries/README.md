# Compact wire entry correction

Scope: the wide banana-plug entry hooks illustrated in the V12 wire screenshots. The existing camera views are retained.

## Change

- Corrected **102 unique cable splines / 204 plug entries**, used by **153 Guided/Free connection bindings** across EXP1–4 and Free Mode. Shared cable objects remain shared; no wire, plug, banana or pointer was duplicated.
- Each cable keeps a short **0.12-world-unit straight outlet** along the plug's own cable axis. The previous broad end hook is replaced by a direct blend into the retained cable trunk.
- Local end loops are trimmed over at most 2.5 arc units per end, limited to 20% of cable length/chord for shorter cables. The central Bezier curves are split analytically and retained, with sampled preservation tolerance 0.0002 world units. This retains the established routing/clearance across the equipment.
- FreeLabWire's existing allocated-plug route uses the same short exit and small tangent, so its dynamic fitting does not restore the old hook on connection.
- Ethernet and mains power-cord geometry was excluded from the banana-plug correction. This is not a wholesale reroute of existing central cable crossings.

## Preservation

`Preservation.txt` compares all **40,369** original serialized scene objects/components. Only the selected SplineContainers' main spline data is retained from the editor save. Every other component and serialized value matches the pre-change scene, including:

- Camera poses/settings and movement targets.
- Plug/banana positions, rotations, colliders and assignments.
- Interaction events, storyboard order, markers, power and wiring requirements.
- Materials, UI, graph, mathematical values and other experiments' data.
- Extra legacy splines; the 102 modified containers have no knot-index links that trimming could invalidate.

Generated mesh cache files touched by diagnostics are restored. Fresh cable activation is tested against an explicit rebuild to verify that current splines drive actual rendered geometry. Existing collider generation/removal code and physics logic are unchanged.

## Verification

- `Geometry.txt`: all 204 outlets align with their plug axes and the retained central curves match the originals.
- `Views`: before/after renders use a temporary camera copied from the scene camera at the **existing authored poses**. All four experiments were checked at poses 0,1,8; representative full images are retained, with SHA-256 inventory for all 24 captures in `ViewManifest.json`. Extra diagnostic captures remain in ignored Temp, not runtime content.
- `Clearance.json`: dense curve sampling checked 672 Guided cable pairs, excluding common-pin stacking regions. No new close approaches were found at the stated thresholds. This distinguishes new regressions from pre-existing crossings; it is not an exhaustive triangle-level collision proof.
- `after.smoke.txt`: final actual runtime outcome, covering Main Menu -> Guided -> Free for EXP1–4, model/lamp/wire references, all three rheostat controls, regenerated cable render/click meshes, collider raycasts and removal through the wire's existing removal handler.
- Rendered meshes immediately after activation must match an explicit rebuild, checking that old cached geometry is not being displayed.
- `after.samples.txt`: 176 numerical cases compared with the prior baseline; numerical outputs must be unchanged.

The runtime checks use controlled API inputs and raycasts, not a complete human traversal of every guided step. Existing unrelated checklist issues are not marked resolved by this cable-entry pass.

## Maintenance

Temporary editor correction/probe/test sources are archived in Tools and removed from imported Assets after verification. They are one-time diagnostics/migration sources, not runtime dependencies. `Baseline.json` identifies the original revision; before/after geometry fixtures record the measured inputs. Reapply only against a verified matching baseline, not blindly over subsequently edited splines.

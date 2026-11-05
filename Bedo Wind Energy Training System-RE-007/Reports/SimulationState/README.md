# Simulation state / SOAP migration — phase 4

Scope: preserve the existing simulation and procedure, give shared wind values explicit SOAP storage and reset ownership, and check numerical/structural parity. Unity remains **6000.3.21f1**.

Implementation and writer/reader ownership are documented in [WindSimulationState.md](../../Documentation/WindSimulationState.md).

## Changed

- Added five `WindRuntimeState` contexts: one per Guided experiment and a separate Free Mode context.
- Added 57 numeric SOAP variables, eight independent switch variables, one authoritative Free Mode connection list and the shared speed settings asset (72 assets total).
- Bound existing controllers in place. Public interaction methods, event signatures, CSV references, step assignments and graph/export source APIs remain intact.
- Added one scene-local `WindSimulationSession` under Systems to reset values before initialization and release wire references on destruction.
- Retained legacy local/default fallbacks for unconfigured old content. The production binding audit requires SOAP assignments, so those fallbacks are inactive in the configured wind simulation.
- Did not change vendor SOAP code, Unity version, packages, mathematical equations, CSV contents or Main Menu.

## Numerical and behavior evidence

`before.samples.txt` and `after.samples.txt` contain **176 corresponding cases** and match exactly. The cases cover:

- All four Guided controllers: six RPM values from 0 through 1000, six rheostat/decrement positions (where assigned), and turbine-off readings.
- Existing PWM/MPPT load/output/system flags and off-grid powered/battery/load/grid calculations with the authored wire/plug visuals connected.
- All four Free Mode circuit profiles with actual authored wires created, power controls enabled, six RPM positions, power-off readings, and wire removal after power-off.

The `.smoke.txt` logs also check Main Menu → Guided → Free for EXP1–4, the 100/150/800-ohm ranges, each canvas's plus/minus bindings, the 86 wire definitions and model/lamp references. These are regression scenarios, not full scientific acceptance or a complete human traversal of every guided step.

After cleanup, both runtime and Editor assemblies passed Unity's bundled C# compiler checks (exit 0 for each; `RuntimeCompiler.log` / `EditorCompiler.log`). The one-time migration/test scripts are absent from the final compilation inputs. Existing obsolete-API warnings remain; no player/WebGL build was performed in this phase.

## Pre-existing issues observed

The expanded connection/teardown tests log **three exceptions in both the before and after runs**. `KnownSplineIssue.txt` captures the Unity Splines `SplineExtrude.Rebuild` deferred Editor callback calling `AssetDatabase.SaveAssetIfDirty` during teardown, after a mesh has gone away. This was observed before any state migration and is not presented as a new migration failure or as fixed. It remains flagged for a separate spline/editor lifecycle repair. Existing missing-script/reference warnings also remain outside this phase.

The raw smoke logs label a run containing any exception as `FAIL`; numerical parity and the unchanged baseline exception count are reported separately. No new runtime exception appeared in these scenarios. Do not interpret this report as a zero-defect acceptance certificate.

## Preservation and rollback

- `Before/SourceAndScenes.zip` contains the exact source and both delivery scenes from before this phase.
- `StructuralAudit.txt` records production SOAP bindings, nonpersistent state, explicit reset ownership and comparison against all 65,787 original serialized objects/components. Only the new SOAP reference fields and the new session child are allowed changes in that comparison; existing values, event bindings, visuals, hierarchy and wiring remain covered.
- Main Menu bytes, CSV content (normalizing Git line endings), solver source and the 16 equipment prefab files are checked independently in `StaticValidation.txt`.
- Temporary binding/regression tools are archived under `Tools` outside Assets after verification. The read-only ownership audit remains available in Unity.

Stateful player/WebGL delivery, all reset permutations (including the later 20-cycle acceptance requirement), the scientific approval matrix, performance budgets and LMS integration remain later work.

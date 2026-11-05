# Step 6 — interaction and restart validation

The production change in this step is limited to an Editor-only deferred mesh-save guard in the existing **Unity Splines 2.9.0** package. The package is embedded so the correction is reproducible; no package version upgrade or project experiment logic change was made. `SplinePatch.txt` records the exact scope. The other package files remain identical to the installed original.

## Tested scenarios

- EXP1–4 Main Menu -> Guided -> Free startup with the correct selected storyboard.
- All source model, 86 wire-definition and eight lamp-reference assignments resolve.
- Three Free Mode rheostats initialize to 100/150/800 ohms; each own Minus/Plus button changes the correct controller and restores its maximum.
- Main/wind power cannot be enabled in the disconnected lab; an unauthored endpoint pair is rejected.
- Connecting an authored pair uses the exact existing cable object. Collider clicks are handled by the logical parent or a child forwarding component.
- Operating power blocks removal; emergency shutdown cuts main power; the unpowered cable can be removed without leaving a stale connection.
- The actual models/controllers reproduce all **176 phase-4 numerical samples exactly** (`NumericalParity.txt`).
- Twenty consecutive reloads alternate Free and Guided. The harness deliberately dirties each context's requested speed/turbine flag and switch assets before successive reloads, and verifies zero/off inputs, off switches and an empty connection list afterwards. Free Mode also checks its configured rheostat maxima; inactive Free Mode controls are not required to be initialized in Guided Mode.

`runtime.smoke.txt` is the final run's actual outcome, including any runtime errors. A passing subset is not a full manual learner traversal of every wire, quiz, camera, graph and UI panel. The test constructs profile wires and drives calculation controls through their APIs; it does not claim every action was performed by a physical mouse raycast. Reflection is used by this temporary Editor harness only, not production runtime code.

## Fixed regression

SplineExtrude.Rebuild scheduled an Editor save using the mutable `m_Mesh` field. OnDisable could clear that field before the delayed callback ran, causing NullReferenceException during cable activation/deactivation/teardown. The guard captures the mesh that was rebuilt and checks it still exists before saving. Runtime extrusion calculations, knot positions, cable materials, equipment, colliders and graph math remain unchanged.

The earlier unpatched runs reproduced this exception. Final runtime results establish whether it is resolved in these scenarios. An independent source comparison checks that only this package file differs from the original; existing Assets content is not intentionally edited.

## Remaining evidence

- Full human traversal of all guided instruction/quiz/camera sequences and negative wiring permutations.
- Identical-physical-circuit cross-mode comparison for all lamp combinations; the historical profile samples intentionally use different EXP4 loads/grid states.
- Resource and listener profiling across a representative complete-flow 20-cycle run. This automated run verifies reset state, not a leak-free performance certificate.
- Existing missing source components and serialized reference warnings remain separately disclosed in the dependency report.

Reproduction source is archived in Tools outside Assets. Temporarily import it into an Editor folder, wait for compilation to finish, then write `runtime` to `Temp/acceptance-regression.flag`. Do not modify/recompile test sources during Play Mode. It exits Play Mode and restores PV Lab without saving runtime scene changes. Remove the temporary imported tool afterwards.

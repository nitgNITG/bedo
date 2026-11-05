# Wind simulation state ownership

Phase 4 migrates the existing simulation owners to SOAP; it does not replace the scientific model or the experiment procedure. Production assignments are in `PV Lab`, and assets are in `Assets/_Project/Data/Wind/Simulation`.

## Authoritative values

| Value | Writer/owner | SOAP storage | Readers |
| --- | --- | --- | --- |
| Guided requested RPM and turbine on/off | The experiment's existing `WindTurbineSpeedController` | Its `WindRuntimeState` context | Slider/rotor, existing binder and graph calculation APIs |
| Guided generator measurements | That turbine controller's existing `TryGetReading` calculation | Context `source` | Meter binder, graph, rows/export through their existing source API |
| Guided PWM/MPPT output/load/system flags and derived battery/dump values | Existing `PVGuidedDataBinder` | The **same** context obtained from its turbine controller | Existing display methods |
| Guided off-grid main/inverter flags and bus measurements | Existing off-grid binder | Its experiment context | Existing off-grid displays |
| Each physical rheostat's resistance | Its `RheostatController` | A separate `WindFloatVariable`, through `FloatReference` | Guided model, Free Mode model and existing graph APIs |
| Each Free Mode physical switch | Its `FreeLabControl` | A separate `WindBoolVariable`, through `BoolReference` | Wiring interlocks, circuit evaluation, Free Mode controller |
| Free Mode connected wires | `FreeLabManager` | One `WindConnectionState` SOAP list | `Connections`, `ConnectionCount`, `HasConnection`, device/circuit requirements and wire removal |
| Free Mode requested RPM, detected circuit profile, effective resistance and measurements | Existing `FreeLabWindController` | Separate Free Mode `WindRuntimeState` | Its existing monitor, graph, rotor and export consumers |
| Shared speed limits, slider increment and visual multiplier | `WindSimulationSettings` | SOAP float references | Both Guided and Free controllers |
| Authored rheostat ranges/rotation increments and Guided charger parameters | Their explicit SOAP float references | Separate numeric configuration assets initialized from the existing serialized values | The owning component |

Five contexts prevent inactive experiments and Free Mode from overwriting one another. Individual switches and rheostats also use distinct assets. A new reusable prefab instance must receive its own device state references; do not copy one device's mutable state reference to another physical device.

Requested RPM and generated RPM are different quantities: the slider may retain a requested speed while turbine power is off, while `source.rpm` and electrical outputs are zero. Likewise, the selected menu experiment belongs to `InteractionManager`; the Free Mode circuit profile belongs to circuit detection. These are intentionally distinct meanings, not competing copies of one value.

## Reset and lifetime

`Systems/Wind Simulation Session` runs at execution order **-10000**, before physical-control and experiment initialization. It resets the five contexts, numeric values and switches, then clears the physical connection list. Every scene reload, mode switch and fresh attempt therefore starts from authored defaults. Hiding an interaction step does not reset shared simulation state.

The connection list is cleared on session destruction so it cannot retain destroyed scene wires. No simulation assets save to PlayerPrefs. SOAP's automatic scene-loaded reset is disabled for these assets: resetting after components' `Awake` would erase initialized inputs. The session is the explicit reset owner.

The small SOAP variable subclasses initialize assets loaded **after** entering Editor Play Mode as well as in a player. This is necessary for Main Menu → PV Lab loading; it avoids an Editor-only zero configuration or stale runtime value on late-loaded assets. Vendor SOAP code was not modified.

## Procedure, physical objects and presentation

- Guided `WireInteraction` still owns its actual cable and plug state. Advancing a storyboard row clears completion/progression state, but does not disconnect a physical cable. Guided off-grid calculations continue checking cable/plug state; no second logical connected flag was added.
- Guided completion latches, current storyboard index and one-time maximum/minimum events remain in the interaction/procedure layer. They are not simulation input values and were not migrated into a shared progress flag.
- Free Mode's SOAP connection list stores the same existing live `FreeLabWire` objects. No connections are mirrored into a second active list and no wire visuals are copied or redrawn.
- Graph history, row history, export records, UI visibility and charger-screen smoothing remain presentation/history state. They consume the same calculation APIs as before.
- CSV assets and the static `PVCSVReader` equations remain the scientific authority. The only reader-source change is adding serialization attributes to its two result structs so SOAP can represent them.

Existing serialized scalar fields and legacy local fallbacks are retained for unconfigured old/reference content and portable prefab templates. **Every production wind assignment is bound to SOAP**, so these fallback values do not compete with the active assets. When SOAP is assigned, edit its configuration asset/reference rather than the retained legacy scalar default. The read-only ownership audit checks those bindings explicitly.

The simulation does not call LMS/reporting services. No controller depends on a successful network response or grade/progress update. Broader scientific approval, exhaustive flow/reset testing, WebGL/performance tests and LMS reporting remain later phases.

## Validation

See [phase 4 evidence](../Reports/SimulationState/README.md). `Tools → Wind Simulation → Audit State Ownership` audits explicit bindings and preservation of the existing scene's serialized content. The one-time binding and Play Mode regression tools are archived outside Assets after use.

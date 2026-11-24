# Wind readings and graph audit

## Changes
- Reconnected Experiment 2's WindTurbineSpeedController.graphController to its own PVGraphController.
- Enabled the existing graph controllers for Experiments 2, 3 and 4. Their existing active-experiment gate remains in place; runtime inspection confirmed one chart and one active graph controller.
- Set Experiment 2's System-tab instruction (storyboard index 28, displayed instruction 16) to a two-second elapsed-real-time delay. Other instructions retain their previous timing behavior.
- Corrected title-localization retry ownership when selecting an inactive storyboard. Previously this logged "Coroutine couldn't be started ... inactive" during experiment changes and Free Mode restoration.

## Guided functional runs
All four actual experiment cards were used. Automated input called the normal Interaction.Click routes for both cable terminals and single-click controls, the authored +/- button for the Exp 1 rheostat, the existing speed slider events, and the correct quiz answer buttons. The runner waited for normal storyboard progression and reached the final completion entry in all four experiments.

Each run recorded 200, 400, 600, 800, and 1000 RPM, with five chart points and five entries in each of the RPM, DC voltage and DC current table columns.

At the loaded 4-ohm speed sweep, the recorded DC voltage was 9.17, 18.33, 27.50, 36.67, 45.83 V and DC current was 2.29, 4.58, 6.88, 9.17, 11.46 A.

The final Exp 2 run measured its System instruction at 2.000749 seconds. No runtime errors were recorded during that run and its subsequent normal Free Mode transition after the localization fix.

## Free Mode functional configurations
28 wiring configurations, five speed samples each (140 samples):
- Profile 1: 100, 150, and 800-ohm rheostats (3).
- PWM: all four DC lamp alternatives (4).
- MPPT: all four DC lamp alternatives (4).
- Off-grid: all sixteen DC/AC lamp combinations, plus a grid-connected case (17).

For every configuration:
- Connections were made through endpoint Interaction.Click.
- Power controls were enabled through their regular interactions and enable checks.
- Live voltage/current matched the existing model equations and selected resistance.
- Voltage and current plots retained all five points.
- All three table columns and the CSV builder contained five rows.

## Additional checks
- All 86 Free Mode wire definitions had both endpoints and a wire template.
- All Guided wind meter, model and graph links resolved.
- No missing persistent onStepStart/onStepEnd targets in the four storyboards.
- All Free Mode page readouts, lamp references, controls, and required/alternative groups resolved.
- Clear cleared the plot, three table columns and CSV history; another speed change recorded fresh data.
- Turning the Free Mode turbine off returned the source DC readouts on all four monitor pages to zero.
- Restart restored zero RPM, zero connected wires, power off, and the 100/150/800-ohm defaults.
- Actual mouse checks in Experiment 2 opened Graphs, selected the current-versus-speed plot, and cleared the chart/tables.
- Actual runtime chart/table screenshots were inspected for Guided Exp 4 and Free Mode off-grid.

## Limits
This was an Editor Play Mode reading/graph audit, not a standalone build or exhaustive visual/localization/platform certification. Most full-flow input was automated through existing event routes; representative graph UI controls were additionally tested with physical mouse input. Existing editor authoring warnings were not treated as proof of a reading failure.

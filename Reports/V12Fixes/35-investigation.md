# V12 issue 35 — investigation only

No performance or simulation behavior was changed for this issue.

Static findings:
- Guided EXP2/3 call PVGuidedDataBinder.RefreshDisplay every Update; EXP4 does likewise. This formats and assigns meter text even with unchanged input.
- FreeLabWindController.Update calls Refresh every frame. It checks connection groups, calculates readings, formats text and writes shared state. Refresh invokes ApplyPages every frame.
- PVCSVReader.TryCalculateWind reparses the calibration TextAsset into a SortedDictionary for each request. TryCalculateOffGrid similarly splits the CSV and builds a constants dictionary for each request. Repeated requests can allocate memory and create garbage-collection pressure.
- PVGraphController suppresses duplicate RPM/voltage points and instantiates only the active graph. This source inspection alone does not establish that XCharts causes the reported stutter.
- Spline mesh and click-surface generation occurs on cable activation; it could cause a connection-time spike, but it is not evidence of continuous stutter.

These are candidates, not a confirmed diagnosis. A frame-time/GC allocation capture during the reported stutter is still required to distinguish script allocations, canvas rebuilds, rendering and activation spikes. No profiler measurements are claimed here. Investigation status: open; no optimization applied, as requested.

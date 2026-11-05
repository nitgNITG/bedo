# Issue 35 follow-up investigation

Status: investigated, not fixed. No production performance, model, rendering or simulation behavior changed for this issue.

## Measured Editor capture

Unity 6000.3.21f1, existing project settings, Main Menu -> Guided -> Free for all four experiments. ProfilerRecorder counters Main Thread and GC Allocated In Frame sampled once per rendered frame, during the final 2.5 seconds of each settled mode wait. Loading, mesh rebuilds and synchronous regression calls are excluded from these windows. This is an Editor capture, not a standalone-build or deep call-stack profile.

1,199 frames: median main-thread duration 16.63..16.68 ms; per-mode 95th percentile 17.01..17.12 ms; maximum 26.70 ms; no frame above 33.33 ms. These wall times can include frame pacing/waits. The reported hitch was not reproduced in these short settled windows. GC allocation counter medians were 278.55..331.31 KiB/frame; these include the Editor and diagnostic overhead and must not be attributed wholly to the simulation.

See 35-editor-frames.csv and 35-summary.json for raw and grouped measurements.

## Scientific calculation microbenchmark

After 32 warmups, five batches of 500 actual calls with assigned source assets: EXP1 TryCalculateWind median approximately 24.59 ms per batch (0.049 ms/call); controller calibration median 4.82 ms (0.0096 ms/call); TryCalculateOffGrid median 5.21 ms (0.0104 ms/call). The per-thread allocation API returned zero even for known-allocating code in this Unity runtime, so the bytes_per_call column in 35-csv-benchmark.csv is unavailable evidence, not proof of zero allocations. The frame recorder counter above was nonzero.

## Supported candidates and limits

- TryCalculateWind creates a sorted dictionary and parses rows on each call; TryCalculateOffGrid splits CSV and builds a constants dictionary on each call. Timings do not show that an individual parse alone explains a visible hitch.
- Guided EXP2-4 refresh display each Update. FreeLabWindController.Refresh runs each frame, reevaluating connection groups/model, allocating formatted strings and applying page visibility. These are plausible avoidable sources of repeated work and allocation.
- The chart is already lazy-spawned for the active experiment and suppresses duplicate readings. This investigation does not establish that the graph is responsible.
- Wire mesh/collider cooking and first activation can produce transient spikes, which these settled windows intentionally do not quantify.

Next evidence needed to close this issue: capture a longer Development Build CPU/GC timeline while reproducing the actual visible hitch (including the triggering click/tab/connection if applicable), inspect allocation call stacks and correlate spikes with CSV/display refresh, canvas rebuilds, graph refresh or mesh cooking. No optimization has been applied under this investigation-only request.

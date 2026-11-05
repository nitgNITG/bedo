# Issue 28 graph correction

Restored physical/UI tabs for DC Voltage versus RPM and AC current versus RPM (AC-Speed), bound to all five wind graph controllers. AC points use WindReading.acCurrent from the same active binder or Free Mode model as the meters. DC current remains in the synchronized scrolling table. CSV export remains RPM/DC voltage. Switching graphs retains the recorded measurements; Clear clears both plotted datasets and the table.

Added 7 percent internal right margin to wind charts, retaining the 0..1200 RPM range. Visually checked both tabs after asynchronous chart redraw: the full 1200 label fits inside the chart without moving the monitor or camera.

Validation: Main Menu -> Guided -> Free for EXP1-4 passed with zero runtime errors. AC points checked against active model readings at sampled RPM/load settings. Physical tab event and data/selection state checked. All 176 numerical regression samples exactly match Reports/V12Fixes/Integration/after.samples.txt. Existing scene objects retained; no scientific constants, CSV files, storyboard steps or camera poses changed.

# V12 issue 52: zoom in monitor camera view 12

The shared camera mover now tracks the selected authored view index. Mouse wheel zoom is available in Guided Mode only at index 12 (Camera Pos 12, system/graph monitor). Guided panning stays disabled. Free Mode retains the pan/zoom behavior from issue 42. Selecting another saved view restores the original lens and framing. All lens/pose writes remain in the LateUpdate call path; no camera positions or storyboard steps were edited.

Guided runtime tests pass for EXP1-4: no zoom in view 11 or overview; zoom in view 12; no Guided panning; 20-80 degree bounds; actual UI button detection; UI, viewport, focus and transition guards; resetting saved view framing. Zero runtime errors. Free Mode tests are recorded in ../V12Issue42/runtime.txt. Synthetic mouse samples are injected from a LateUpdate probe through the same private input-processing method; UI hit checks use the actual EventSystem.

Controls: right- or middle-mouse drag pans in Free Mode; wheel zooms over the scene. Wheel/drag over UI graphics is reserved for those controls. Temporary test runners are archived here outside Assets. The saved user scene stayed byte-identical throughout both test runs and is not part of either commit.

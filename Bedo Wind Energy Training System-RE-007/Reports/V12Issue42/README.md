# V12 issue 42: Free Mode mouse navigation

Right- or middle-mouse drag pans in the current camera plane. Mouse wheel adjusts perspective field of view from 20 to 80 degrees. Pan is limited to 25 world units from the saved view. Left-click remains reserved for wiring and device interactions. Mouse navigation is blocked over UI graphics, outside the camera viewport, while a saved-view transition is running, or while the application lacks focus. Releasing/crossing UI/losing focus cancels the drag. Selecting a saved camera view restores its authored position, rotation and original lens.

Implemented in the existing SmoothCameraMovement. All pose and lens writes are reached through LateUpdate. No scene, camera target, storyboard, wire or scientific model changes are required. At this commit mouse navigation is enabled only in Free Mode; Guided monitor zoom is the separate issue 52 change.

Runtime tests pass for Free Mode launched from Main Menu for EXP1-4: pan/zoom and bounds, preserved rotation, actual UI button hit detection, UI/focus/transition guards, saved-view reset and no immediate pose writes from view requests. Zero runtime errors. Tests inject mouse samples through the same private navigation path from a LateUpdate probe and use actual UI raycasts.

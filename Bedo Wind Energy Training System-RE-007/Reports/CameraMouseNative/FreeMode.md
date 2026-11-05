# Follow-up: Free Mode mouse input

The earlier injected-input checks missed the real input path. A native Windows drag reproduced the failure: a complete fast gesture could arrive before one rendered frame and lose its starting position. The existing installed Input System now buffers gesture start/end coordinates; only LateUpdate applies camera movement. Its normalized wheel units are used directly (platform-specific Windows wheel deltas are divided by 120 only when that setting is selected).

Decorative UI graphics no longer block navigation. Selectable controls and scrolling lists still own their input. Right/middle dragging remains supported; left dragging can pan when it starts on empty space. Starts on wires, bananas, physical controls and UI controls are protected. Missed initial auxiliary-button frames recover without a jump. Existing bounds, saved-view resets and focus cancellation remain.

Actual Windows drag visibly moved the camera approximately 5.7 world units. Native wheel changed FOV 60 to 54; wheel over the Wind UI button left FOV unchanged at 54. Details are in FreeNativeChecks.json. Tests used normal player input, not calls to ProcessMouseNavigation. No scene or scientific model changes.

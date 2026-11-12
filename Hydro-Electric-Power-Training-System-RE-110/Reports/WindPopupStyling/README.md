# Wind speed UI styling and guided highlight

Wind Speed Button is now a sibling immediately after Wire Transparency btn in the existing Right Panel, with the same 60x60 dimensions and vertical spacing. Reuses the right-side button visual states and animator, keeping the wind graphic and caption. The popup reuses the existing Main Settings background, frame, font and grayscale palette. Its View turbine control has a rectangular project-style frame. The empty standalone overlay canvas was removed.

A shared Step Highlight child replaces the old Slider Button pointer for the authored speed step in EXP1-4 (elements 22, 29, 26, 31). Existing collider-unlock/camera events are retained. The step enables the glow/outline and clears it on exit. No runtime scripts, maths, wires or other step behavior changed.

Unity runtime tests passed for all four experiments in Guided and Free Mode: hierarchy/style, highlight before/during/after the step, old pointer hidden, slider gate/max completion, navigation/re-entry, popup open/close and no highlight in Free Mode. Zero runtime errors. Final font/frame preview passed separately; the outline is a child of the tested highlight. Temporary editor tools are archived outside Assets.

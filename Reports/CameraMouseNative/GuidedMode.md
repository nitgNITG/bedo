# Follow-up: Guided camera zoom

Issue 52 used the zero-based list index 12, although the on-screen number is index + 1. The gate now includes displayed camera 12 (list index 11) and retains zoom for the authored monitor view by Transform identity. Free Mode navigation remains available. Guided panning remains disabled.

Native Windows wheel input through the normal player input path changed FOV from 60 to 54 on both displayed camera 12 and the monitor. A repeat wheel input on displayed camera 12 changed FOV from 54 to 48. Position and rotation were unchanged. No synthetic calls to the navigation method were used. Evidence is in GuidedNativeChecks.json.

The temporary observer was removed. The existing user scene stayed byte-for-byte unchanged.

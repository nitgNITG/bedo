# Issue 35 camera update-order correction

SmoothCameraMovement keeps its existing serialized camera positions, camera buttons, number label, moveDuration and public MoveToPosition(int) entry point. Requests now capture transition state only; LateUpdate applies SmoothStep position/rotation interpolation and the exact final target pose. A new request replaces the current transition. Disabled components cancel movement; a missing/destroyed target cancels safely.

No scenes, camera poses, storyboard calls, maths or packages are changed by this fix. Existing unsaved scene edits were saved before testing as previously authorized, and retained outside the commit. The saved scene SHA256 is 3BE47C11D48261B1B31080183A5B6BD308B59CA045F5B5834F01E13CF4A73BD5. The authored duration is 1.5 seconds. Cinemachine migration is unnecessary for this correction and no package was added.

Validation uses actual rendered frames in all four Guided and Free experiments: every saved view, no immediate camera-pose writes from requests, motion observed between Update and the end of LateUpdate, labels, interrupted/latest requests, configured-duration behavior, invalid indexes and disposable-target edge cases (zero/negative duration, disable/re-enable and destroyed target). Tests do not save scenes. Temporary runners are archived outside Assets.

This addresses the camera-timing concern raised for issue 35. It does not establish that update order caused all previously reported stuttering; the earlier performance investigation remains valid until the original hitch is reproduced and retested.

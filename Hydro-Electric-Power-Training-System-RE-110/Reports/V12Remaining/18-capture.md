# Issue 18

Verified the existing monitor-surface capture used by the four System Save Screen handlers and the shared Guided/Free Graph exporter. The graph button calls PVExperimentExporter.SaveGraphPNG, not its unused legacy MapCameraScreenshotButton_RT component. No capture-camera or collider changes were necessary.

Rendered each System monitor and the graph surface to PNG and visually checked the results: only the monitor content is present, without bezel, room, subtitles or navigation overlays. Surface fitting determines the image aspect ratio. The physical graph mouse handler also reached the native Save Wind PNG dialog; that dialog was dismissed without writing a user file. The graph evidence was rendered immediately after activation, before chart tick labels received their first update; the normal save coroutine waits until end of frame.

The existing collider/OnMouseDown save mechanism and scientific/graph data are unchanged.

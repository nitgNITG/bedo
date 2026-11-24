mergeInto(LibraryManager.library, {
  StudentTracking_Emit: function (jsonPtr) {
    var json = UTF8ToString(jsonPtr);
    var payload;

    try {
      payload = JSON.parse(json);
    } catch (error) {
      console.error("[student-tracking] Invalid JSON from Unity", error);
      return;
    }

    if (!Array.isArray(window.studentTrackingEvents)) {
      window.studentTrackingEvents = [];
    }
    window.studentTrackingEvents.push(payload);

    if (typeof window.onStudentTrackingEvent === "function") {
      try {
        window.onStudentTrackingEvent(payload);
        return;
      } catch (error) {
        console.error("[student-tracking] Host callback failed", error);
      }
    }

    if (typeof window.dispatchEvent === "function" &&
        typeof window.CustomEvent === "function") {
      window.dispatchEvent(new CustomEvent("student-tracking", {
        detail: payload
      }));
    }
  }
});
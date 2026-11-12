# Guided wiring permission fix

Guided Mode previously allowed every wire interaction, regardless of the current
storyboard step. Finished wires also re-enabled their endpoints for removal, and
advancing cleared completion without disabling the previous endpoints.

The existing input, wire and storyboard scripts now validate the owning active
experiment, current step and expected endpoint before invoking click events.
Guided completion disables endpoints; advancing retains connected plugs and wire
visuals. Removal remains available in Free Mode. Single controls retain their
existing authored permissions.

## Validation

Unity 6000.3.21f1 compiled the changes. Play Mode regression launched EXP1–4 from
Main Menu and tested all 72 guided wire entries (10/18/18/26):

- Correct first and second endpoints complete the connection.
- Forced stale permissions cannot authorize unrelated or out-of-order clicks.
- Repeated clicks cannot remove completed wires during the next-step delay.
- Advancing retains plugs and wires and rejects previous endpoints.
- Switching each experiment to Free Mode permits normal endpoint connection and
  removal through the existing Free Mode wire handler.

See `runtime.txt`. The temporary runner is archived in `Tools/` outside Assets.
No scene assets were saved by testing. These are interaction-level Play Mode
checks, not a visual or full experiment acceptance test.

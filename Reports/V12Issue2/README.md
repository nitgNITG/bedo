# V12 issue 2 — wind speed popup

The shared Wind Speed Button opens an overlay containing the existing speed slider and readout. The panel is hidden with a CanvasGroup so opening/closing it does not reset the guided collider gate, listeners or speed. View turbine moves to a newly appended optional camera; positions 0–12 are unchanged.

Runtime checks cover all four Guided and Free experiments, 200-RPM quantization, authored slider unlock, maximum event once, existing graph/model bindings, lamp/rheostat references, cable activation/removal and 176 scientific samples. The analyzer check is separately recorded under V12Issue7. Existing authored steps, maths and connection definitions are unchanged.

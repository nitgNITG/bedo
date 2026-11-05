# V12 issue 7 — check only

Checked the existing physical Power Analyzer display without changing its styling, text or experiment wiring.

- EXP1, EXP2 and EXP3: assigned to the shared Power Analyzer canvas and TMP text. The Mode selection interaction calls ShowPowerAnalyzer. Existing background RawImage has nonzero opacity. Text uses three lines: W, VAr and VA. Runtime samples verify the text equals the model AC power, zero reactive power (as currently modelled), and absolute AC voltage × AC current.
- Free Mode: all four starting experiments use the assigned shared analyzer; screen activation and the same live units/values passed the runtime test.
- **EXP4 Guided: both powerAnalyzerCanvas and powerAnalyzerText are unassigned. This is not complete. Left unchanged because this item was requested as a check only.**

Evidence: ../V12Issue2/runtime.txt. Test-only null checks avoid treating the known missing EXP4 binding as a new runtime regression. No production file changed for issue 7.

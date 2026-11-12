# Scientific validation — phase 5

Status: **automated numerical evidence prepared; scientific acceptance remains pending**. No production script, scene, prefab, CSV or equipment configuration was changed in this phase. The existing unsaved/working-tree PV Lab edits were left untouched. Unity remains 6000.3.21f1.

## Results

- **711/711 source values match** the supplied workbook and the project's wind data assets. `WorkbookTrace.csv` identifies each workbook cell, asset column and compared value.
- **126 solver cases / 656 numerical assertions pass**, with zero numerical failures. `Results.csv` contains expected/actual values and tolerances. `Summary.json` records the solver hash and test scope.
- The actual production wind calculation methods are compiled verbatim from `PVCSVReader.cs`, with minimal standalone replacements for TextAsset, Vector2 and Mathf. This is a numerical unit harness, not a Unity execution or visual test. Runtime compilation succeeded (`Compiler.txt`, empty on success).
- Source rows cover EXP1 resistance/speed calibration, EXP2/3 shared generator calibration, EXP4 generator and grid-current columns, and the separate reference-curve API. Additional cases cover zero/max RPM, all slider increments, 150/800-ohm AC equations, multiple load counts, discharge/charge/dump balance, and rejection of zero/negative resistance and nonfinite speed.
- Provisional numerical tolerance: max(0.00001 absolute, 0.000002 × absolute expected value). This accommodates single-precision arithmetic. It is **not an approved BEDO scientific tolerance**. The source-to-CSV trace uses a much tighter double-precision comparison.

## Model trace

| Experiment | Source | Production calculation |
| --- | --- | --- |
| EXP1 Speed Curve | Sheet1 A3:K38, formulas A2:K2 | WindExp1Data; V_AC=0.055×RPM, I_AC=V_AC/R, P_AC=V_AC×I_AC, V_DC=V_AC/1.2×C1, I_DC=V_DC/(R×C2), P_DC=V_DC×I_DC |
| EXP2 PWM | Sheet1 A70:O76, N77 charge-limit note | WindExp2Data generator; binder subtracts connected load, limits battery charge to 9 A, routes surplus to dump |
| EXP3 MPPT | Same supplied controller calibration as EXP2 | Same source equations; no separate MPPT efficiency/IV/MPPT tracking curve was supplied |
| EXP4 off-grid | Sheet1 A70:Z76 and limit/sign notes below | WindExp4Data; battery bus 24 V, AC 220 V; physical load counts and wiring determine battery/grid/load outputs |

The workbook contains one worksheet and multiple tables. `WorkbookCells.json` preserves cell values/formulas, original path and SHA-256 for repeatability. This reads existing Excel cached values and formulas; it does not recalculate Excel. Formula checks independently exercise the explicit source equations.

## Approval / unresolved cases — no scientific substitutions made

1. **Extended rheostats:** calibration exists at 4,20,40,60,80,100 ohms. The current solver interpolates C1/C2 inside that range and retains end calibration outside it. AC equations at 150/800 ohms are tested, but DC calibration above 100 ohms has no reference table. BEDO must approve the existing extension or supply calibration.
2. **PWM versus MPPT:** the supplied data does not distinguish their conversion efficiency or MPPT dynamics. They currently share numerical calibration. A scientifically distinct MPPT algorithm cannot be certified from this source.
3. **EXP4 charge/discharge interpretation:** S70:S71 are negative in raw cells, while S78 says floor at zero; runtime implements the floor. T70:T76 contain separate illustrative charging inputs; U/Y/Z depend on those inputs. The runtime does not expose T as an independent control and therefore these columns are not asserted as live-equivalent outputs. Confirm the intended charge-input and sign conventions before changing them.
4. **Load variants:** source provides 2 A DC halogen, 0.2 A DC LED, 0.2 A AC display current and 2 A AC DC-equivalent current. Multiple lamps are currently additive; Free Mode treats AC LED equivalent current as 0.2 A. This latter extension has no explicit reference case. Two AC lamps plus one DC LED in Guided EXP4 differs intentionally from the workbook's one AC halogen plus one DC halogen example; compare only identical physical loads.
5. **Energy interpretation:** the supplied calibration itself produces DC power above AC power in several rows. Tests match it; no efficiency correction was invented. AC lamp display power (220×0.2=44 W) differs from its DC-equivalent (24×2=48 W). Confirm these teaching-model conventions for formal approval.
6. **Power analyzer:** reactive power is currently displayed as 0 VAr and apparent power as |V×I|. There is no power-factor/phase-angle model in the source; certification beyond the present resistive assumption needs approval.
7. **Graph:** the live graph intentionally uses live DC voltage so resistance affects it (user instruction). The separate O/P measured curve is preserved and tested through its reference API, but is not asserted to equal the live graph for arbitrary resistance.

## Acceptance matrix

| Standard | Current evidence | Status |
| --- | --- | --- |
| B-01, G-01 equations and references | 711 source comparisons; 656 solver checks; formula/unit trace above | Automated checks PASS; scientific approval/tolerance pending |
| B-02 cross-view consistency | Source review: binder and graph use live DC readings; phase 4 preserved source APIs | Visual meter/graph/rows/export comparison still required |
| B-03 Guided/Free parity | Phase 4 covered both modes, but its EXP4 sample profiles use different loads/grid states | Identical-circuit runtime parity still required; do not infer parity from unchanged results |
| B-04 wiring/power | Phase 4 authored circuit/power smoke evidence | Negative wiring/interlock matrix deferred to phase 6 |
| B-05 boundaries | Zero/max/increments, resistance endpoints and extensions, current balance and invalid input rejection | Numerical subset PASS; physical/mode boundaries pending |
| B-06 reset | Explicit state reset owner from phase 4 | Fresh-run comparison and 20-cycle reset exercise deferred to phase 6 |

No gate is marked fully accepted solely on these unit checks. The pre-existing Splines deferred teardown exceptions and missing source components remain flagged in earlier reports.

## Repeat

From project root, using Python 3:

1. `python Reports/ScientificValidation/Tools/extract_workbook.py` (requires the original workbook path recorded in that script).
2. `python Reports/ScientificValidation/Tools/trace_workbook.py`.
3. `python Reports/ScientificValidation/Tools/validate_science.py` (uses installed Unity 6000.3.21f1 bundled compiler/runtime).

These commands write reports and ignored Temp files only. No test utility is imported into Assets, no scene is loaded/saved, and no experiment is modified. Source hashes identify the tested revision; rerun after model changes. Runtime/visual acceptance must be performed in Unity and delivery builds separately.

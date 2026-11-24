# Wind student-tracking mapping

Project-specific mapping for the Wind Energy Training System, generated from Assets/_Project/Scenes/Wind Lab.unity.

- schemaVersion: 2.1
- projectId: com.BEDO.Wind-Energy-Training-System
- projectVersion: 0.1 (increment on each build release)
- assessmentVersion: 1.0.0 (increment when learning requirements or grading change)
- Manifest: Wind-BUILD-0.1.json

## Host transport

The WebGL .jslib converts Unity JSON to an object, queues it in window.studentTrackingEvents, and calls window.onStudentTrackingEvent(payload) when present. Otherwise it dispatches CustomEvent('student-tracking', { detail: payload }). Each payload carries device-id: student-tracking directly; the bridge does not add another envelope.

The receiving website is responsible for authenticated learner association, validation against an approved manifest, deduplication by eventId, retries and durable storage. Browser-side events alone do not persist learner results.

## Lifecycle mapping

- Session: InteractionManager.Start calls StartSession; it is idempotent, and the first session event includes the project manifest.
- Guided attempt: StoryboardSteps.StartStoryboard starts the matching EXPnn_GUIDED attempt.
- Guided step start: entering a step with an assigned interaction emits StepStarted. Delay-only steps and screens without interactions are omitted.
- Guided step completion: the previous interaction's onCompleted listener advances the storyboard; GoToStep completes the prior tracked interaction step. Duplicate starts/completions are ignored.
- Guided experiment completion: emitted when the storyboard reaches its final completion screen/chime. Exiting/restarting or replacing an unfinished attempt emits ExperimentAborted.
- Free mode: selecting a Wind experiment starts its matching EXPnn_FREE attempt and step 1. Step 1 and the experiment complete when that experiment's evaluator reaches B - Modules Connected.
- Question: MCQ/True-False panels emit QuestionShown when they receive a question, and one QuestionAnswered plus one correct/incorrect result on each button submission. MCQ answers are zero-based choice indexes as strings; True/False answers are true/false strings. Attempts reset per experiment attempt.
- Session completion: the existing Exit Game action emits SessionCompleted; app/tab termination without that action cannot guarantee an event flush.

## Experiment IDs

| Mode | Experiment ID | Wind title |
| --- | --- | --- |
| Free | EXP01_FREE | Wind Experiment 1 |
| Guided | EXP01_GUIDED | Speed Curve Characteristics |
| Free | EXP02_FREE | Wind Experiment 2 |
| Guided | EXP02_GUIDED | Regulating DC Output from Wind Turbine using PWM Charger |
| Free | EXP03_FREE | Wind Experiment 3 |
| Guided | EXP03_GUIDED | Regulating DC Output from Wind Turbine using MPPT Charger |
| Free | EXP04_FREE | Wind Experiment 4 |
| Guided | EXP04_GUIDED | OFF Grid System Using Wind Turbine |

## Guided step catalog and emission points

### EXP01_GUIDED - Speed Curve Characteristics

| Step | Tracking | Authored boundary |
| ---: | --- | --- |
| 1 | tracked, required | WireInteraction (Main Board 1 to Full bringe rectifier Red input); starts on entry, completes on its onCompleted callback |
| 2 | tracked, required | WireInteraction (Main Board 1 to Full bringe rectifier Black input); starts on entry, completes on its onCompleted callback |
| 3 | tracked, required | WireInteraction (Full bringe rectifier output Red to rheostat 100 ohm red); starts on entry, completes on its onCompleted callback |
| 4 | tracked, required | WireInteraction (Full bringe rectifier output Black to rheostat 100 ohm Black); starts on entry, completes on its onCompleted callback |
| 5 | tracked, required | WireInteraction (Main frame  to the wind mill module  ethernet); starts on entry, completes on its onCompleted callback |
| 6 | tracked, required | WireInteraction (Full bringe rectifier L1 to wind mill module L1); starts on entry, completes on its onCompleted callback |
| 7 | tracked, required | WireInteraction (Full bringe rectifier L2 to wind mill module L2); starts on entry, completes on its onCompleted callback |
| 8 | tracked, required | WireInteraction (Full bringe rectifier L3 to wind mill module L3); starts on entry, completes on its onCompleted callback |
| 9 | tracked, required | WireInteraction (Full bringe rectifier +ve to rheostat 100 ohm terminal 1); starts on entry, completes on its onCompleted callback |
| 10 | tracked, required | WireInteraction (Full bringe rectifier -ve to rheostat 100 ohm terminal 3); starts on entry, completes on its onCompleted callback |
| 11 | tracked, required | SingleClickInteraction (Emergeny_button002 (1)); starts on entry, completes on its onCompleted callback |
| 12 | tracked, required | SingleClickInteraction (single_432 (1)); starts on entry, completes on its onCompleted callback |
| 13 | tracked, required | SingleClickInteraction (on_button004 (1)); starts on entry, completes on its onCompleted callback |
| 14 | tracked, required | SingleClickInteraction (Emergeny_button wind); starts on entry, completes on its onCompleted callback |
| 15 | tracked, required | SingleClickInteraction (Breaker wind); starts on entry, completes on its onCompleted callback |
| 16 | tracked, required | SingleClickInteraction (Power_Switch wind); starts on entry, completes on its onCompleted callback |
| 17 | tracked, required | SingleClickInteraction (Mode selection wind001); starts on entry, completes on its onCompleted callback |
| 18 | tracked, required | SingleClickInteraction (air speed power adjustment001  rheostat 100); starts on entry, completes on its onCompleted callback |
| 19 | omitted, optional | Delay / screen only; omitted |
| 20 | tracked, required | SingleClickInteraction (Graph Button); starts on entry, completes on its onCompleted callback |
| 21 | omitted, optional | Delay / screen only; omitted |
| 22 | omitted, optional | Delay / screen only; omitted |
| 23 | omitted, optional | Interaction / screen only; omitted |
| 24 | omitted, optional | Interaction / screen only; omitted |
| 25 | omitted, optional | Interaction / screen only; omitted |

### EXP03_GUIDED - Regulating DC Output from Wind Turbine using MPPT Charger

| Step | Tracking | Authored boundary |
| ---: | --- | --- |
| 1 | tracked, required | WireInteraction (Main Board 1 to Full bringe rectifier Red input); starts on entry, completes on its onCompleted callback |
| 2 | tracked, required | WireInteraction (Main Board 1 to Full bringe rectifier Black input); starts on entry, completes on its onCompleted callback |
| 3 | tracked, required | WireInteraction (Full bringe rectifier output Red to MPPT Solar Charge Controller red); starts on entry, completes on its onCompleted callback |
| 4 | tracked, required | WireInteraction (Full bringe rectifier output Black to MPPT Solar Charge Controller black); starts on entry, completes on its onCompleted callback |
| 5 | tracked, required | WireInteraction (MPPT Solar Charge Controller red to Battery Red); starts on entry, completes on its onCompleted callback |
| 6 | tracked, required | WireInteraction (MPPT Solar Charge Controller red to Battery Black); starts on entry, completes on its onCompleted callback |
| 7 | tracked, required | WireInteraction (Battery Output Red to DC Halogen Input Red); starts on entry, completes on its onCompleted callback |
| 8 | tracked, required | WireInteraction (DC Halogen Input Black to Battery Output Black); starts on entry, completes on its onCompleted callback |
| 9 | tracked, required | WireInteraction (Main frame  to the wind mill module  ethernet); starts on entry, completes on its onCompleted callback |
| 10 | tracked, required | WireInteraction (Full bringe rectifier L1 to wind mill module L1); starts on entry, completes on its onCompleted callback |
| 11 | tracked, required | WireInteraction (Full bringe rectifier L2 to wind mill module L2); starts on entry, completes on its onCompleted callback |
| 12 | tracked, required | WireInteraction (Full bringe rectifier L3 to wind mill module L3); starts on entry, completes on its onCompleted callback |
| 13 | tracked, required | WireInteraction (Full bringe rectifier +ve toPWM Solar Charge Controller DC Load +ve); starts on entry, completes on its onCompleted callback |
| 14 | tracked, required | WireInteraction (Full bringe rectifier -ve PWM Solar Charge Controller DC Load -ve); starts on entry, completes on its onCompleted callback |
| 15 | tracked, required | WireInteraction (MPPT Solar Charge Controller Battery -ve to Battery -ve); starts on entry, completes on its onCompleted callback |
| 16 | tracked, required | WireInteraction (Battery +ve to MPPT Solar Charge Controller Battery +ve); starts on entry, completes on its onCompleted callback |
| 17 | tracked, required | WireInteraction (MPPT Solar Charge Controller DC Load +ve to DC Halogen +ve); starts on entry, completes on its onCompleted callback |
| 18 | tracked, required | WireInteraction (DC Halogen -ve to MPPT Solar Charge Controller DC Load -ve); starts on entry, completes on its onCompleted callback |
| 19 | tracked, required | SingleClickInteraction (Emergeny_button002 (1)); starts on entry, completes on its onCompleted callback |
| 20 | tracked, required | SingleClickInteraction (single_432 (1)); starts on entry, completes on its onCompleted callback |
| 21 | tracked, required | SingleClickInteraction (on_button004 (1)); starts on entry, completes on its onCompleted callback |
| 22 | tracked, required | SingleClickInteraction (Object1554); starts on entry, completes on its onCompleted callback |
| 23 | tracked, required | SingleClickInteraction (Emergeny_button wind); starts on entry, completes on its onCompleted callback |
| 24 | tracked, required | SingleClickInteraction (Breaker wind); starts on entry, completes on its onCompleted callback |
| 25 | tracked, required | SingleClickInteraction (Power_Switch wind); starts on entry, completes on its onCompleted callback |
| 26 | tracked, required | SingleClickInteraction (Mode selection wind001); starts on entry, completes on its onCompleted callback |
| 27 | omitted, optional | Interaction / screen only; omitted |
| 28 | omitted, optional | Delay / screen only; omitted |
| 29 | omitted, optional | Interaction / screen only; omitted |
| 30 | omitted, optional | Delay / screen only; omitted |
| 31 | omitted, optional | Interaction / screen only; omitted |

### EXP04_GUIDED - OFF Grid System Using Wind Turbine

| Step | Tracking | Authored boundary |
| ---: | --- | --- |
| 1 | tracked, required | WireInteraction (Main Board 1 to Full bringe rectifier Red input); starts on entry, completes on its onCompleted callback |
| 2 | tracked, required | WireInteraction (Main Board 1 to Full bringe rectifier Black input); starts on entry, completes on its onCompleted callback |
| 3 | tracked, required | WireInteraction (Full bringe rectifier output Red to AC Halogen Red input); starts on entry, completes on its onCompleted callback |
| 4 | tracked, required | WireInteraction (Full bringe rectifier output Black to AC Halogen Black input); starts on entry, completes on its onCompleted callback |
| 5 | tracked, required | WireInteraction (AC Halogen Red to Off-grid Inverter Red); starts on entry, completes on its onCompleted callback |
| 6 | tracked, required | WireInteraction (AC Halogen Black to Offgrid Inverter Black); starts on entry, completes on its onCompleted callback |
| 7 | tracked, required | WireInteraction (Off grid inverter output red to Battery input Red); starts on entry, completes on its onCompleted callback |
| 8 | tracked, required | WireInteraction (Off grid inverter output red to Battery Black); starts on entry, completes on its onCompleted callback |
| 9 | tracked, required | WireInteraction (Battery Output Red to DC LED Input Red); starts on entry, completes on its onCompleted callback |
| 10 | tracked, required | WireInteraction (DC LED Input Black to Battery Output Black); starts on entry, completes on its onCompleted callback |
| 11 | tracked, required | WireInteraction (Main frame  to the wind mill module  ethernet); starts on entry, completes on its onCompleted callback |
| 12 | tracked, required | WireInteraction (Off-Grid Inverter Battery -ve to Battery -ve); starts on entry, completes on its onCompleted callback |
| 13 | tracked, required | WireInteraction (Battery +ve to MPPT Solar Charge Controller Battery +ve); starts on entry, completes on its onCompleted callback |
| 14 | tracked, required | WireInteraction (DC LED -ve to Off-Grid Inverter Battery -ve); starts on entry, completes on its onCompleted callback |
| 15 | tracked, required | WireInteraction (Off-Grid Inverter Battery +ve to DC LED +ve); starts on entry, completes on its onCompleted callback |
| 16 | tracked, required | SingleClickInteraction (Emergeny_button002 (1)); starts on entry, completes on its onCompleted callback |
| 17 | tracked, required | SingleClickInteraction (single_432 (1)); starts on entry, completes on its onCompleted callback |
| 18 | tracked, required | SingleClickInteraction (on_button004 (1)); starts on entry, completes on its onCompleted callback |
| 19 | tracked, required | SingleClickInteraction (Power connector button); starts on entry, completes on its onCompleted callback |
| 20 | tracked, required | WireInteraction (AC Halogen L 1 to Off-grid Inverter AC Output L); starts on entry, completes on its onCompleted callback |
| 21 | tracked, required | WireInteraction (AC Halogen N 1 to Off-grid Inverter AC Output N); starts on entry, completes on its onCompleted callback |
| 22 | tracked, required | WireInteraction (AC Halogen L 2 to Off-grid Inverter AC Output L); starts on entry, completes on its onCompleted callback |
| 23 | tracked, required | WireInteraction (AC Halogen N 2 to Off-grid Inverter AC Output N); starts on entry, completes on its onCompleted callback |
| 24 | tracked, required | WireInteraction (Full bringe rectifier L1 to wind mill module L1); starts on entry, completes on its onCompleted callback |
| 25 | tracked, required | WireInteraction (Full bringe rectifier L2 to wind mill module L2); starts on entry, completes on its onCompleted callback |
| 26 | tracked, required | WireInteraction (Full bringe rectifier L3 to wind mill module L3); starts on entry, completes on its onCompleted callback |
| 27 | tracked, required | WireInteraction (Full bringe rectifier -ve to Off-grid Inverter DC Input -ve); starts on entry, completes on its onCompleted callback |
| 28 | tracked, required | WireInteraction (Full bringe rectifier +ve to Off-grid Inverter DC Input +ve); starts on entry, completes on its onCompleted callback |
| 29 | tracked, required | SingleClickInteraction (Emergeny_button wind); starts on entry, completes on its onCompleted callback |
| 30 | tracked, required | SingleClickInteraction (Breaker wind); starts on entry, completes on its onCompleted callback |
| 31 | tracked, required | SingleClickInteraction (Power_Switch wind); starts on entry, completes on its onCompleted callback |
| 32 | omitted, optional | Interaction / screen only; omitted |
| 33 | omitted, optional | Delay / screen only; omitted |
| 34 | omitted, optional | Interaction / screen only; omitted |
| 35 | tracked, required | WireInteraction (Main Board Black 2 to Off-grid Inverter AC Input N); starts on entry, completes on its onCompleted callback |
| 36 | tracked, required | WireInteraction (Main Board Red 2 to Off-grid Inverter AC Input L); starts on entry, completes on its onCompleted callback |
| 37 | omitted, optional | Interaction / screen only; omitted |
| 38 | omitted, optional | Interaction / screen only; omitted |

### EXP02_GUIDED - Regulating DC Output from Wind Turbine using PWM Charger

| Step | Tracking | Authored boundary |
| ---: | --- | --- |
| 1 | tracked, required | WireInteraction (Main Board 1 to Full bringe rectifier Red input); starts on entry, completes on its onCompleted callback |
| 2 | tracked, required | WireInteraction (Main Board 1 to Full bringe rectifier Black input); starts on entry, completes on its onCompleted callback |
| 3 | tracked, required | WireInteraction (Full bringe rectifier output Red to PWM Solar Charge Controller red); starts on entry, completes on its onCompleted callback |
| 4 | tracked, required | WireInteraction (Full bringe rectifier output Black to PWM Solar Charge Controller black); starts on entry, completes on its onCompleted callback |
| 5 | tracked, required | WireInteraction (PWM Solar Charge Controller red to Battery Red); starts on entry, completes on its onCompleted callback |
| 6 | tracked, required | WireInteraction (PWM Solar Charge Controller red to Battery Black); starts on entry, completes on its onCompleted callback |
| 7 | tracked, required | WireInteraction (Battery Output Red to DC Halogen Input Red); starts on entry, completes on its onCompleted callback |
| 8 | tracked, required | WireInteraction (DC Halogen Input Black to Battery Output Black); starts on entry, completes on its onCompleted callback |
| 9 | tracked, required | WireInteraction (Main frame  to the wind mill module  ethernet); starts on entry, completes on its onCompleted callback |
| 10 | tracked, required | WireInteraction (Full bringe rectifier L1 to wind mill module L1); starts on entry, completes on its onCompleted callback |
| 11 | tracked, required | WireInteraction (Full bringe rectifier L2 to wind mill module L2); starts on entry, completes on its onCompleted callback |
| 12 | tracked, required | WireInteraction (Full bringe rectifier L3 to wind mill module L3); starts on entry, completes on its onCompleted callback |
| 13 | tracked, required | WireInteraction (Full bringe rectifier +ve toPWM Solar Charge Controller DC Load +ve); starts on entry, completes on its onCompleted callback |
| 14 | tracked, required | WireInteraction (Full bringe rectifier -ve PWM Solar Charge Controller DC Load -ve); starts on entry, completes on its onCompleted callback |
| 15 | tracked, required | WireInteraction (PWM Solar Charge Controller Battery -ve to Battery -ve); starts on entry, completes on its onCompleted callback |
| 16 | tracked, required | WireInteraction (Battery +ve to PWM Solar Charge Controller Battery +ve); starts on entry, completes on its onCompleted callback |
| 17 | tracked, required | WireInteraction (PWM Solar Charge Controller DC Load +ve to DC Halogen +ve); starts on entry, completes on its onCompleted callback |
| 18 | tracked, required | WireInteraction (DC Halogen -ve to PWM Solar Charge Controller DC Load -ve); starts on entry, completes on its onCompleted callback |
| 19 | tracked, required | SingleClickInteraction (Emergeny_button002 (1)); starts on entry, completes on its onCompleted callback |
| 20 | tracked, required | SingleClickInteraction (single_432 (1)); starts on entry, completes on its onCompleted callback |
| 21 | tracked, required | SingleClickInteraction (on_button004 (1)); starts on entry, completes on its onCompleted callback |
| 22 | tracked, required | SingleClickInteraction (Enlèv. mat.-Extru.009); starts on entry, completes on its onCompleted callback |
| 23 | tracked, required | SingleClickInteraction (Emergeny_button wind); starts on entry, completes on its onCompleted callback |
| 24 | tracked, required | SingleClickInteraction (Breaker wind); starts on entry, completes on its onCompleted callback |
| 25 | tracked, required | SingleClickInteraction (Power_Switch wind); starts on entry, completes on its onCompleted callback |
| 26 | tracked, required | SingleClickInteraction (Mode selection wind001); starts on entry, completes on its onCompleted callback |
| 27 | omitted, optional | Delay / screen only; omitted |
| 28 | omitted, optional | Interaction / screen only; omitted |
| 29 | omitted, optional | Delay / screen only; omitted |
| 30 | omitted, optional | Interaction / screen only; omitted |
| 31 | omitted, optional | Interaction / screen only; omitted |

## Question catalog

The build manifest includes question ScriptableObjects under Assets/_Project only. Question IDs are normalized asset names with a Q_ prefix. Do not copy IDs from Assets/_Reference/OtherLabs into the Wind release. The WebGL runtime uses the same explicit allowlist (currently Q_TRUEANDFALSEQUESTION) because editor asset paths are unavailable in the browser; add any new Wind question to the allowlist and manifest mapping together.

| Question ID | Type | Required |
| --- | --- | --- |
| Q_TRUEANDFALSEQUESTION | TrueFalse | False |

All catalog questions are optional for completion. Events are emitted only during an active tracking attempt.

## Verification status

- Unity editor compilation completed without tracking-related errors.
- The in-project Editor contract-check runner passed lifecycle, unique event IDs, stable session, replay attempt IDs, duplicate steps, retry, answer pairing/attempt numbers, abort, and progress rejection after end. Run it from Tools > Student Tracking > Run Contract Checks.
- The existing console error about missing authored Lamp 1 L/N wires is from the unrelated AC Lamp 2 setup.
- WebGL callback delivery still needs a build hosted by the target page to confirm platform receipt; no platform account/backend is connected in this project.

# Step 9 — progress/questions/LMS integration audit

Status: **blocked for production integration**. No experiment, quiz, simulation or network behavior was changed.

The acceptance standard requires BEDO's approved schema 2.1 reporter, stable published project/assessment identities, a catalog of tracked/required steps, per-attempt lifecycle and question events, and a reporting transport. No approved reporter package/schema contract, LMS endpoint/test environment, stable release identities or tracked-step catalog is present in this checkout.

## Existing behavior and gaps

- StoryboardSteps owns the actual guided sequence. Repeated subtitle numbers can represent multiple physical interactions; UI numbering alone must not define reporting identity.
- MultipleChoiceQuiz currently shows correct/incorrect feedback and retries incorrect submissions. The standard's default is hidden correctness, unless project-specific requirements approve feedback. Do not remove existing teaching behavior without checking that exception.
- There is no experiment-attempt event ledger or schema 2.1 adapter in production wind code. The old template's UI progress assets are not an LMS implementation.
- Scientific state remains independent of reporting/network availability.

## Required integration inputs

1. Approved BEDO reporter package and schema 2.1 envelope/transport contract, including offline queue, identity, retry and duplicate-delivery rules.
2. Stable projectId, build version, assessmentVersion, experiment/step/question IDs and the approved tracked/required step catalog.
3. Confirmation of whether the wind storyboard intentionally reveals quiz correctness; otherwise H-01 requires changing that feedback.
4. LMS sandbox endpoint/host bridge and a test account/session supplied through the approved process. No credentials should be committed.

## Repeatable acceptance cases to run after integration

| Case | Expected |
| --- | --- |
| Complete a required tracked step twice | One progress contribution within the same attempt |
| Complete navigation/untracked step | No progress contribution |
| Retry a completed required step | Remove contribution until it completes again |
| Start a new experiment attempt | Separate attempt ID; no merged progress |
| Wrong answer then correct answer | Two submissions/results, attempts 1 and 2; selected answers and correctness retained internally |
| Submit once / duplicate transport retry | Exactly one logical recorded result |
| Complete experiment with missing evidence | Lifecycle completion does not invent 100% progress or pass grade |
| Drop reporting transport | Solver and learner interactions continue with identical physical/numerical results |
| Exit/switch unfinished experiment | ExperimentAborted reports the correct attempt |

Required event names are recorded in the supplied standard: SessionStarted, SessionCompleted, ExperimentStarted, ExperimentCompleted, ExperimentAborted, StepStarted, StepCompleted, StepFailed, StepRetried, QuestionShown, QuestionAnswered, QuestionCorrect, QuestionIncorrect.

This report is an integration handoff, not a claim that Gate H passes. Inventing a local replacement for the approved reporter would not demonstrate compatibility.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace TutorialFramework
{
    /// <summary>
    /// Central schema-2.1 student tracking state machine for the Hybrid WebGL lab.
    /// UI and simulation code report confirmed boundaries here; only this class
    /// creates event payloads and sends them to the host page.
    /// </summary>
    public static class HybridStudentTracking
    {
        public const string ProjectId = "com.BEDO.Renewable-Energy-Hybrid-Training-System";
        public const string ProjectVersion = "0.1";
        public const string AssessmentVersion = "1.0.0";
        public const string SchemaVersion = "2.1";
        public const string DeviceId = "student-tracking";

        [Serializable]
        private sealed class TrackingEvent
        {
            public string deviceId;
            public string schemaVersion;
            public string eventId;
            public string sessionId;
            public string experimentAttemptId;
            public string projectId;
            public string projectVersion;
            public string assessmentVersion;
            public string experimentId;
            public string eventType;
            public string timestamp;
            public int stepNumber;
            public string questionId;
            public string questionType;
            public string answer;
            public int attempt;
            public string mode;
            public ProjectManifest projectManifest;
        }

        [Serializable]
        private sealed class ProjectManifest
        {
            public string schemaVersion;
            public string projectId;
            public string projectVersion;
            public string assessmentVersion;
            public ExperimentDefinition[] experiments;
        }

        [Serializable]
        private sealed class ExperimentDefinition
        {
            public string experimentId;
            public string title;
            public StepDefinition[] steps;
            public QuestionDefinition[] questions;
        }

        [Serializable]
        private sealed class StepDefinition
        {
            public int stepNumber;
            public bool tracked;
            public bool requiredForCompletion;
        }

        [Serializable]
        private sealed class QuestionDefinition
        {
            public string questionId;
            public string questionType;
            public bool requiredForCompletion;
        }

        private static string sessionId;
        private static string experimentId;
        private static string experimentAttemptId;
        private static string mode;
        private static bool experimentEnded;
        private static bool sessionEnded;
        private static readonly HashSet<int> startedSteps = new HashSet<int>();
        private static readonly HashSet<int> completedSteps = new HashSet<int>();
        private static readonly Dictionary<string, int> questionAttempts =
            new Dictionary<string, int>(StringComparer.Ordinal);

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void StudentTracking_Emit(string json);
#endif

        public static int CurrentStepNumber { get; private set; }
        public static bool HasActiveExperiment =>
            !string.IsNullOrEmpty(experimentId) && !experimentEnded;

        public static string ExperimentId(string storyboardId, bool guided)
        {
            string number;
            switch (storyboardId)
            {
                case "RE009 Off Grid Inverter": number = "EXP01"; break;
                case "Speed Curve Characteristics": number = "EXP02"; break;
                default: return null;
            }

            return number + (guided ? "_GUIDED" : "_FREE");
        }

        public static string QuizQuestionId(int stepNumber, int questionNumber)
        {
            return $"Step_{stepNumber}_Question_{questionNumber}";
        }
        public static string StableQuestionId(string questionAssetName)
        {
            if (string.IsNullOrWhiteSpace(questionAssetName))
                return "Q_UNNAMED";

            var chars = questionAssetName.ToUpperInvariant().ToCharArray();
            var result = new System.Text.StringBuilder("Q_");
            bool previousUnderscore = false;
            foreach (char c in chars)
            {
                if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
                {
                    result.Append(c);
                    previousUnderscore = false;
                }
                else if (!previousUnderscore)
                {
                    result.Append('_');
                    previousUnderscore = true;
                }
            }

            return result.ToString().TrimEnd('_');
        }

        public static void StartExperiment(string id, string experimentMode)
        {
            if (sessionEnded || string.IsNullOrWhiteSpace(id))
                return;

            EnsureSessionStarted();
            AbortActiveExperiment();

            experimentId = id;
            mode = experimentMode;
            experimentAttemptId = Guid.NewGuid().ToString("D");
            experimentEnded = false;
            CurrentStepNumber = 0;
            startedSteps.Clear();
            completedSteps.Clear();
            questionAttempts.Clear();

            Emit(NewEvent("ExperimentStarted"));
        }

        public static void AbortActiveExperiment()
        {
            if (!HasActiveExperiment)
                return;

            Emit(NewEvent("ExperimentAborted"));
            experimentEnded = true;
        }

        public static void CompleteExperiment()
        {
            if (!HasActiveExperiment)
                return;

            Emit(NewEvent("ExperimentCompleted"));
            experimentEnded = true;
        }

        public static void StartStep(int stepNumber)
        {
            if (!HasActiveExperiment || stepNumber <= 0 ||
                completedSteps.Contains(stepNumber) ||
                !startedSteps.Add(stepNumber))
                return;

            CurrentStepNumber = stepNumber;
            var payload = NewEvent("StepStarted");
            payload.stepNumber = stepNumber;
            Emit(payload);
        }

        public static void CompleteStep(int stepNumber)
        {
            if (!HasActiveExperiment || stepNumber <= 0 ||
                !startedSteps.Contains(stepNumber) ||
                !completedSteps.Add(stepNumber))
                return;

            var payload = NewEvent("StepCompleted");
            payload.stepNumber = stepNumber;
            Emit(payload);
        }

        public static void FailStep(int stepNumber)
        {
            if (!HasActiveExperiment || stepNumber <= 0 ||
                !startedSteps.Contains(stepNumber) ||
                completedSteps.Contains(stepNumber))
                return;

            var payload = NewEvent("StepFailed");
            payload.stepNumber = stepNumber;
            Emit(payload);
        }

        public static void RetryStep(int stepNumber)
        {
            if (!HasActiveExperiment || stepNumber <= 0)
                return;

            completedSteps.Remove(stepNumber);
            startedSteps.Remove(stepNumber);
            var payload = NewEvent("StepRetried");
            payload.stepNumber = stepNumber;
            Emit(payload);
            StartStep(stepNumber);
        }

        public static void ShowQuestion(string questionAssetName, string type)
        {
            if (!HasActiveExperiment || string.IsNullOrWhiteSpace(questionAssetName))
                return;

            string id = StableQuestionId(questionAssetName);
            int nextAttempt;
            if (!questionAttempts.TryGetValue(id, out nextAttempt))
                nextAttempt = 1;
            else
                nextAttempt++;

            var payload = NewEvent("QuestionShown");
            payload.questionId = id;
            payload.questionType = type;
            payload.attempt = nextAttempt;
            Emit(payload);
        }

        public static void AnswerQuestion(
            string questionAssetName,
            string type,
            string answer,
            bool correct)
        {
            if (!HasActiveExperiment || string.IsNullOrWhiteSpace(questionAssetName))
                return;

            string id = StableQuestionId(questionAssetName);
            int nextAttempt;
            if (!questionAttempts.TryGetValue(id, out nextAttempt))
                nextAttempt = 1;
            else
                nextAttempt++;

            questionAttempts[id] = nextAttempt;

            var answered = NewEvent("QuestionAnswered");
            answered.questionId = id;
            answered.questionType = type;
            answered.answer = answer ?? string.Empty;
            answered.attempt = nextAttempt;
            Emit(answered);

            var result = NewEvent(correct ? "QuestionCorrect" : "QuestionIncorrect");
            result.questionId = id;
            result.questionType = type;
            result.attempt = nextAttempt;
            Emit(result);
        }

        public static void StartSession()
        {
            if (!sessionEnded)
                EnsureSessionStarted();
        }

        public static void CompleteSession()
        {
            if (sessionEnded)
                return;

            AbortActiveExperiment();
            EnsureSessionStarted();
            Emit(NewEvent("SessionCompleted"));
            sessionEnded = true;
        }

        private static void EnsureSessionStarted()
        {
            if (!string.IsNullOrEmpty(sessionId))
                return;

            sessionId = Guid.NewGuid().ToString("D");
            var payload = NewEvent("SessionStarted");
            payload.projectManifest = BuildManifestFromLoadedScene();
            Emit(payload);
        }

        private static TrackingEvent NewEvent(string type)
        {
            return new TrackingEvent
            {
                deviceId = DeviceId,
                schemaVersion = SchemaVersion,
                eventId = Guid.NewGuid().ToString("D"),
                sessionId = sessionId ?? string.Empty,
                experimentAttemptId = type.StartsWith("Session", StringComparison.Ordinal)
                    ? string.Empty
                    : experimentAttemptId ?? string.Empty,
                projectId = ProjectId,
                projectVersion = ProjectVersion,
                assessmentVersion = AssessmentVersion,
                experimentId = type.StartsWith("Session", StringComparison.Ordinal)
                    ? string.Empty
                    : experimentId ?? string.Empty,
                eventType = type,
                timestamp = DateTime.UtcNow.ToString("o"),
                stepNumber = 0,
                questionId = string.Empty,
                questionType = string.Empty,
                answer = string.Empty,
                attempt = 0,
                mode = mode ?? string.Empty
            };
        }

        private static ProjectManifest BuildManifestFromLoadedScene()
        {
            var definitions = new SortedDictionary<string, ExperimentDefinition>(StringComparer.Ordinal);
            var storyboards = UnityEngine.Object.FindObjectsByType<StoryboardSteps>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var storyboard in storyboards)
            {
                string guidedId = ExperimentId(storyboard.StoryboardID, true);
                if (guidedId == null || definitions.ContainsKey(guidedId))
                    continue;

                var steps = new List<StepDefinition>();
                var questions = new List<QuestionDefinition>();
                for (int index = 0; index < storyboard.steps.Count; index++)
                {
                    var step = storyboard.steps[index];
                    if (step == null)
                        continue;

                    steps.Add(new StepDefinition
                    {
                        stepNumber = index + 1,
                        tracked = true,
                        requiredForCompletion = true
                    });

                    var quiz = step.uiObject ? step.uiObject.GetComponentInChildren<MultipleChoiceQuiz>(true) : null;
                    if (quiz == null)
                        continue;

                    for (int question = 0; question < quiz.questions.Count; question++)
                        questions.Add(new QuestionDefinition
                        {
                            questionId = StableQuestionId(QuizQuestionId(index + 1, question + 1)),
                            questionType = "MultipleChoice",
                            requiredForCompletion = false
                        });
                }

                definitions.Add(guidedId, new ExperimentDefinition
                {
                    experimentId = guidedId,
                    title = storyboard.StoryboardID,
                    steps = steps.ToArray(),
                    questions = questions.ToArray()
                });
                string freeId = ExperimentId(storyboard.StoryboardID, false);
                definitions.Add(freeId, new ExperimentDefinition
                {
                    experimentId = freeId,
                    title = storyboard.StoryboardID,
                    steps = Array.Empty<StepDefinition>(),
                    questions = Array.Empty<QuestionDefinition>()
                });
            }

            return new ProjectManifest
            {
                schemaVersion = SchemaVersion,
                projectId = ProjectId,
                projectVersion = ProjectVersion,
                assessmentVersion = AssessmentVersion,
                experiments = new List<ExperimentDefinition>(definitions.Values).ToArray()
            };
        }
        private static void Emit(TrackingEvent payload)
        {
            string json = JsonUtility.ToJson(payload);
            json = json.Replace("\"deviceId\"", "\"device-id\"");

#if UNITY_WEBGL && !UNITY_EDITOR
            StudentTracking_Emit(json);
#else
            Debug.Log("[STUDENT_TRACKING] " + json);
#endif
        }
    }
}

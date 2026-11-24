using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace TutorialFramework
{
    /// <summary>
    /// Central schema-2.1 student tracking state machine for the Wind WebGL lab.
    /// UI and simulation code report confirmed boundaries here; only this class
    /// creates event payloads and sends them to the host page.
    /// </summary>
    public static class WindStudentTracking
    {
        public const string ProjectId = "com.BEDO.Wind-Energy-Training-System";
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

        public static string ActiveExperimentId => experimentId;
        public static bool HasActiveExperiment =>
            !string.IsNullOrEmpty(experimentId) && !experimentEnded;

        public static string GuidedExperimentId(string storyboardId)
        {
            switch (storyboardId)
            {
                case "Speed Curve Characteristics": return "EXP01_GUIDED";
                case "Regulating DC Output from Wind Turbine using PWM Charger": return "EXP02_GUIDED";
                case "Regulating DC Output from Wind Turbine using MPPT Charger": return "EXP03_GUIDED";
                case "OFF Grid System Using Wind Turbine": return "EXP04_GUIDED";
                default: return null;
            }
        }

                public static string FreeExperimentIdFromStoryboard(string storyboardId)
        {
            switch (storyboardId)
            {
                case "Speed Curve Characteristics": return "EXP01_FREE";
                case "Regulating DC Output from Wind Turbine using PWM Charger": return "EXP02_FREE";
                case "Regulating DC Output from Wind Turbine using MPPT Charger": return "EXP03_FREE";
                case "OFF Grid System Using Wind Turbine": return "EXP04_FREE";
                default: return null;
            }
        }

public static string FreeExperimentId(string evaluatorId)
        {
            switch (evaluatorId)
            {
                case "Wind Experiment 1": return "EXP01_FREE";
                case "Wind Experiment 2": return "EXP02_FREE";
                case "Wind Experiment 3": return "EXP03_FREE";
                case "Wind Experiment 4": return "EXP04_FREE";
                default: return null;
            }
        }

        public static bool IsActiveExperiment(string candidateId)
        {
            return HasActiveExperiment &&
                   string.Equals(experimentId, candidateId, StringComparison.Ordinal);
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
            if (!HasActiveExperiment || !IsWindQuestionId(questionAssetName))
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
            if (!HasActiveExperiment || !IsWindQuestionId(questionAssetName))
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
            var definitions = new List<ExperimentDefinition>();
            var storyboards = UnityEngine.Object.FindObjectsByType<StoryboardSteps>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (StoryboardSteps storyboard in storyboards)
            {
                if (storyboard == null)
                    continue;

                string id = GuidedExperimentId(storyboard.StoryboardID);
                if (string.IsNullOrEmpty(id) || storyboard.steps == null)
                    continue;

                var stepDefinitions = new List<StepDefinition>();
                for (int i = 0; i < storyboard.steps.Count; i++)
                {
                    StoryboardSteps.Step step = storyboard.steps[i];
                    bool tracked = step != null && step.interaction != null;
                    stepDefinitions.Add(new StepDefinition
                    {
                        stepNumber = i + 1,
                        tracked = tracked,
                        requiredForCompletion = tracked
                    });
                }

                definitions.Add(new ExperimentDefinition
                {
                    experimentId = id,
                    title = storyboard.StoryboardID,
                    steps = stepDefinitions.ToArray(),
                    questions = BuildQuestionCatalog()
                });
            }

            // The first WebGL scene is the menu, so the Wind Lab storyboard
            // objects are not loaded when SessionStarted is emitted. Include
            // the authored guided catalog here so the session manifest is
            // complete before the learner opens the lab.
            AddGuidedFallback(definitions, "EXP01_GUIDED", "Speed Curve Characteristics", 25, new[] { 1, 18, 20, 20 });
            AddGuidedFallback(definitions, "EXP02_GUIDED", "Regulating DC Output from Wind Turbine using PWM Charger", 31, new[] { 1, 26 });
            AddGuidedFallback(definitions, "EXP03_GUIDED", "Regulating DC Output from Wind Turbine using MPPT Charger", 31, new[] { 1, 26 });
            AddGuidedFallback(definitions, "EXP04_GUIDED", "OFF Grid System Using Wind Turbine", 38, new[] { 1, 31, 35, 36 });

            AddFreeDefinition(definitions, "EXP01_FREE", "Wind Experiment 1");
            AddFreeDefinition(definitions, "EXP02_FREE", "Wind Experiment 2");
            AddFreeDefinition(definitions, "EXP03_FREE", "Wind Experiment 3");
            AddFreeDefinition(definitions, "EXP04_FREE", "Wind Experiment 4");

            definitions.Sort((a, b) =>
                string.CompareOrdinal(a.experimentId, b.experimentId));

            return new ProjectManifest
            {
                schemaVersion = SchemaVersion,
                projectId = ProjectId,
                projectVersion = ProjectVersion,
                assessmentVersion = AssessmentVersion,
                experiments = definitions.ToArray()
            };
        }

        private static bool IsWindQuestionId(string questionAssetName)
        {
            return string.Equals(StableQuestionId(questionAssetName),
                "Q_TRUEANDFALSEQUESTION", StringComparison.Ordinal);
        }

        private static bool IsWindQuestionAsset(UnityEngine.Object question)
        {
#if UNITY_EDITOR
            string path = UnityEditor.AssetDatabase.GetAssetPath(question);
            return path.StartsWith("Assets/_Project/", StringComparison.Ordinal);
#else
            return IsWindQuestionId(question.name);
#endif
        }

        
private static QuestionDefinition[] BuildQuestionCatalog()
        {
            var catalog = new Dictionary<string, QuestionDefinition>(StringComparer.Ordinal);

            MultipleChoiceQuestionScriptableObject[] multipleChoice =
                Resources.FindObjectsOfTypeAll<MultipleChoiceQuestionScriptableObject>();
            foreach (MultipleChoiceQuestionScriptableObject question in multipleChoice)
            {
                if (question == null || !IsWindQuestionAsset(question))
                    continue;

                string id = StableQuestionId(question.name);
                if (!catalog.ContainsKey(id))
                {
                    catalog.Add(id, new QuestionDefinition
                    {
                        questionId = id,
                        questionType = "MultipleChoice",
                        requiredForCompletion = false
                    });
                }
            }

            TrueAndFalseQuestionScriptableObject[] trueFalse =
                Resources.FindObjectsOfTypeAll<TrueAndFalseQuestionScriptableObject>();
            foreach (TrueAndFalseQuestionScriptableObject question in trueFalse)
            {
                if (question == null || !IsWindQuestionAsset(question))
                    continue;

                string id = StableQuestionId(question.name);
                if (!catalog.ContainsKey(id))
                {
                    catalog.Add(id, new QuestionDefinition
                    {
                        questionId = id,
                        questionType = "TrueFalse",
                        requiredForCompletion = false
                    });
                }
            }

            var result = new List<QuestionDefinition>(catalog.Values);
            result.Sort((a, b) => string.CompareOrdinal(a.questionId, b.questionId));
            return result.ToArray();
        }

        
private static void AddFreeDefinition(
            List<ExperimentDefinition> definitions,
            string id,
            string title)
        {
            definitions.Add(new ExperimentDefinition
            {
                experimentId = id,
                title = title,
                steps = new[]
                {
                    new StepDefinition
                    {
                        stepNumber = 1,
                        tracked = true,
                        requiredForCompletion = true
                    }
                },
                questions = BuildQuestionCatalog()
            });
        }

        private static void AddGuidedFallback(
            List<ExperimentDefinition> definitions,
            string id,
            string title,
            int stepCount,
            int[] trackedRanges)
        {
            foreach (ExperimentDefinition definition in definitions)
            {
                if (definition != null && string.Equals(definition.experimentId, id, StringComparison.Ordinal))
                    return;
            }

            var steps = new StepDefinition[stepCount];
            for (int i = 0; i < stepCount; i++)
            {
                bool tracked = false;
                for (int range = 0; range + 1 < trackedRanges.Length; range += 2)
                {
                    if (i + 1 >= trackedRanges[range] && i + 1 <= trackedRanges[range + 1])
                    {
                        tracked = true;
                        break;
                    }
                }

                steps[i] = new StepDefinition
                {
                    stepNumber = i + 1,
                    tracked = tracked,
                    requiredForCompletion = tracked
                };
            }

            definitions.Add(new ExperimentDefinition
            {
                experimentId = id,
                title = title,
                steps = steps,
                questions = BuildQuestionCatalog()
            });
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

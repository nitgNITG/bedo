using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using TutorialFramework;

internal static class WindStudentTrackingContractChecks
{
    private static readonly Type TrackerType = typeof(WindStudentTracking);

    [MenuItem("Tools/Student Tracking/Run Contract Checks")]
    private static void RunFromMenu()
    {
        Run();
    }

    public static string Run()
    {
        var saved = new Dictionary<string, object>();
        SaveValue(saved, "sessionId");
        SaveValue(saved, "experimentId");
        SaveValue(saved, "experimentAttemptId");
        SaveValue(saved, "mode");
        SaveValue(saved, "experimentEnded");
        SaveValue(saved, "sessionEnded");

        var started = GetField("startedSteps").GetValue(null) as HashSet<int>;
        var completed = GetField("completedSteps").GetValue(null) as HashSet<int>;
        var attempts = GetField("questionAttempts").GetValue(null) as Dictionary<string, int>;
        var savedStarted = new HashSet<int>(started);
        var savedCompleted = new HashSet<int>(completed);
        var savedAttempts = new Dictionary<string, int>(attempts);

        var events = new List<string>();
        Application.LogCallback handler = (condition, stackTrace, type) =>
        {
            const string prefix = "[STUDENT_TRACKING] ";
            int index = condition.IndexOf(prefix, StringComparison.Ordinal);
            if (index >= 0)
                events.Add(condition.Substring(index + prefix.Length));
        };

        Application.logMessageReceived += handler;
        try
        {
            WindStudentTracking.StartSession();
            WindStudentTracking.StartExperiment("EXP01_GUIDED", "guided");
            WindStudentTracking.StartStep(5);
            WindStudentTracking.StartStep(5);
            WindStudentTracking.CompleteStep(5);
            WindStudentTracking.CompleteStep(5);
            WindStudentTracking.RetryStep(5);
            WindStudentTracking.CompleteStep(5);
            WindStudentTracking.ShowQuestion("TrueAndFalseQuestion", "TrueFalse");
            WindStudentTracking.AnswerQuestion("TrueAndFalseQuestion", "TrueFalse", "false", false);
            WindStudentTracking.ShowQuestion("TrueAndFalseQuestion", "TrueFalse");
            WindStudentTracking.AnswerQuestion("TrueAndFalseQuestion", "TrueFalse", "true", true);
            WindStudentTracking.CompleteExperiment();
            WindStudentTracking.CompleteStep(6);

            WindStudentTracking.StartExperiment("EXP01_GUIDED", "guided");
            WindStudentTracking.StartExperiment("EXP02_FREE", "free");
            WindStudentTracking.AbortActiveExperiment();
            WindStudentTracking.StartStep(9);
            WindStudentTracking.CompleteSession();

            Validate(events);
            string result = "Passed: lifecycle, unique event IDs, stable session, replay attempt IDs, duplicate steps, retry, answer pairing/attempts, abort, and progress rejection after end.";
            Debug.Log("[STUDENT_TRACKING_TEST] " + result);
            return result;
        }
        finally
        {
            Application.logMessageReceived -= handler;
            RestoreValue("sessionId", saved["sessionId"]);
            RestoreValue("experimentId", saved["experimentId"]);
            RestoreValue("experimentAttemptId", saved["experimentAttemptId"]);
            RestoreValue("mode", saved["mode"]);
            RestoreValue("experimentEnded", saved["experimentEnded"]);
            RestoreValue("sessionEnded", saved["sessionEnded"]);
            started.Clear(); foreach (int value in savedStarted) started.Add(value);
            completed.Clear(); foreach (int value in savedCompleted) completed.Add(value);
            attempts.Clear(); foreach (var pair in savedAttempts) attempts[pair.Key] = pair.Value;
        }
    }

    private static void Validate(List<string> events)
    {
        Require(Count(events, "SessionStarted") == 1, "SessionStarted count");
        Require(Count(events, "ExperimentStarted") == 3, "ExperimentStarted count");
        Require(Count(events, "ExperimentAborted") == 2, "ExperimentAborted count");
        Require(Count(events, "ExperimentCompleted") == 1, "ExperimentCompleted count");
        Require(Count(events, "StepStarted") == 2, "duplicate StepStarted protection");
        Require(Count(events, "StepCompleted") == 2, "duplicate StepCompleted protection");
        Require(Count(events, "StepRetried") == 1, "StepRetried count");
        Require(Count(events, "QuestionAnswered") == 2, "answer event count");
        Require(Count(events, "QuestionIncorrect") == 1, "incorrect result count");
        Require(Count(events, "QuestionCorrect") == 1, "correct result count");
        Require(Count(events, "SessionCompleted") == 1, "SessionCompleted count");

        var eventIds = new HashSet<string>();
        string sessionId = null;
        var attemptIds = new List<string>();
        var answerAttempts = new List<int>();
        foreach (string json in events)
        {
            string eventId = Field(json, "eventId");
            Require(!string.IsNullOrEmpty(eventId) && eventIds.Add(eventId), "unique eventId");
            Require(Field(json, "device-id") == "student-tracking", "device-id field");
            Require(Field(json, "schemaVersion") == "2.1", "schemaVersion field");

            if (Field(json, "eventType") == "SessionStarted")
                sessionId = Field(json, "sessionId");
            else
                Require(Field(json, "sessionId") == sessionId, "stable sessionId");

            if (Field(json, "eventType") == "ExperimentStarted")
                attemptIds.Add(Field(json, "experimentAttemptId"));
            if (Field(json, "eventType") == "QuestionAnswered")
                answerAttempts.Add(IntField(json, "attempt"));
        }
        Require(attemptIds.Count == 3 && attemptIds[0] != attemptIds[1] && attemptIds[1] != attemptIds[2], "fresh replay/replacement attempt IDs");
        Require(answerAttempts.Count == 2 && answerAttempts[0] == 1 && answerAttempts[1] == 2, "question answer attempt numbering");
        Require(events.Count == 19, "unexpected extra or missing event");
    }

    private static int Count(List<string> events, string eventType)
    {
        int count = 0;
        foreach (string json in events)
            if (Field(json, "eventType") == eventType) count++;
        return count;
    }

    private static string Field(string json, string key)
    {
        Match match = Regex.Match(json, "\\\"" + Regex.Escape(key) + "\\\"\\s*:\\s*\\\"([^\\\"]*)\\\"");
        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    private static int IntField(string json, string key)
    {
        Match match = Regex.Match(json, "\\\"" + Regex.Escape(key) + "\\\"\\s*:\\s*(\\d+)");
        return match.Success ? int.Parse(match.Groups[1].Value) : -1;
    }

    private static void Require(bool condition, string check)
    {
        if (!condition) throw new InvalidOperationException("Wind student-tracking contract check failed: " + check);
    }

    private static FieldInfo GetField(string name)
    {
        return TrackerType.GetField(name, BindingFlags.NonPublic | BindingFlags.Static);
    }

    private static void SaveValue(Dictionary<string, object> saved, string name)
    {
        saved[name] = GetField(name).GetValue(null);
    }

    private static void RestoreValue(string name, object value)
    {
        GetField(name).SetValue(null, value);
    }
}
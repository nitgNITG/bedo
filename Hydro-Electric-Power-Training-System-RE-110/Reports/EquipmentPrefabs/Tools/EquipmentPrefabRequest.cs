using System;
using System.IO;
using UnityEditor;

[InitializeOnLoad]
public static class EquipmentPrefabRequest
{
    static string pendingFamily;
    static double verifyAt;
    public static void BeginSettledVerification(string family)
    {
        pendingFamily = family; verifyAt = EditorApplication.timeSinceStartup + 2;
        File.WriteAllText("Temp/equipment-prefab-result.txt", "Verifying reopened scene: " + family);
    }
    static EquipmentPrefabRequest() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if (pendingFamily != null)
        {
            if (EditorApplication.timeSinceStartup < verifyAt) return;
            var label = pendingFamily; pendingFamily = null;
            try
            {
                EquipmentPrefabConversion.VerifyBaseline(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                File.AppendAllText(EquipmentPrefabWorkflow.ReportFolder + "/Conversion.txt", "PASS " + label + ": full scene serialization matches baseline after save/reopen and UI layout rebuild.\n");
                File.WriteAllText("Temp/equipment-prefab-result.txt", "PASS " + label);
                if (File.Exists(EquipmentPrefabWorkflow.ReportFolder + "/Mismatch.txt")) File.Delete(EquipmentPrefabWorkflow.ReportFolder + "/Mismatch.txt");
            }
            catch (Exception ex) { File.WriteAllText("Temp/equipment-prefab-result.txt", ex.ToString()); UnityEngine.Debug.LogException(ex); }
            return;
        }
        const string request = "Temp/equipment-prefab-request.txt";
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(request)) return;
        var action = File.ReadAllText(request).Trim();
        File.Delete(request);
        try
        {
            if (action == "audit") EquipmentPrefabWorkflow.Audit();
            else if (action == "audit-both") { EquipmentPrefabWorkflow.AuditBoth(); BeginSettledVerification("Final delivery-scene audit"); }
            else if (action == "baseline") EquipmentPrefabConversion.Baseline();
            else if (action == "rheostats") EquipmentPrefabConversion.Rheostats();
            else if (action == "lamps") EquipmentPrefabConversion.Lamps();
            else if (action == "connectors") EquipmentPrefabConversion.Connectors();
            else if (action == "discard-trial") EquipmentPrefabConversion.DiscardUnsavedTrial();
            else if (action == "outputs") EquipmentPrefabConversion.OutputModelVariant();
            else if (action == "verify") EquipmentPrefabConversion.VerifyBaseline(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            else throw new InvalidOperationException("Unknown prefab request: " + action);
            if (pendingFamily == null) File.WriteAllText("Temp/equipment-prefab-result.txt", "PASS " + action);
        }
        catch (Exception ex) { File.WriteAllText("Temp/equipment-prefab-result.txt", ex.ToString()); UnityEngine.Debug.LogException(ex); }
    }
}

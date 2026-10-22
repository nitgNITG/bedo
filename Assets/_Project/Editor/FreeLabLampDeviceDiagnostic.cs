using System.Text;
using TutorialFramework;
using UnityEditor;
using UnityEngine;

/// <summary>Prints the authored Free Mode lamp device setup without editing it.</summary>
public static class FreeLabLampDeviceDiagnostic
{
    [MenuItem("Free Lab/Print lamp device setup %&k")]
    private static void Print()
    {
        StringBuilder report = new StringBuilder("FREE LAB LAMP DEVICE DIAGNOSTIC\n");
        foreach (FreeLabDevice device in Object.FindObjectsByType<FreeLabDevice>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (device == null || (device.name.IndexOf("LED", System.StringComparison.OrdinalIgnoreCase) < 0 &&
                device.name.IndexOf("Halogen", System.StringComparison.OrdinalIgnoreCase) < 0))
                continue;

            SerializedObject serialized = new SerializedObject(device);
            report.AppendLine(device.name + " | connected=" + device.IsConnected + " | running=" + device.IsRunning);
            AppendObjects(report, "  On visuals", serialized.FindProperty("objectsToTurnOn"));
            AppendObjects(report, "  Off visuals", serialized.FindProperty("objectsToTurnOff"));
            AppendGroups(report, "  Valid paths", serialized.FindProperty("anyConnectionGroups"));
        }
        Debug.Log(report.ToString());
    }

    private static void AppendObjects(StringBuilder report, string label, SerializedProperty items)
    {
        report.Append(label + ":");
        for (int i = 0; items != null && i < items.arraySize; i++)
        {
            Object target = items.GetArrayElementAtIndex(i).objectReferenceValue;
            report.Append(" " + (target != null ? target.name : "<missing>"));
        }
        report.AppendLine();
    }

    private static void AppendGroups(StringBuilder report, string label, SerializedProperty items)
    {
        report.Append(label + ":");
        for (int i = 0; items != null && i < items.arraySize; i++)
        {
            Object target = items.GetArrayElementAtIndex(i).objectReferenceValue;
            report.Append(" " + (target != null ? target.name : "<missing>"));
        }
        report.AppendLine();
    }
}

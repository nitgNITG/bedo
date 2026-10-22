using TutorialFramework;
using UnityEditor;
using UnityEngine;

public static class FreeLabMainBoardDiagnostics
{
    [MenuItem("Free Lab/Diagnostics/Log Main Board Element 2")]
    private static void Element2() => Log(2);
    [MenuItem("Free Lab/Diagnostics/Log Main Board Element 3")]
    private static void Element3() => Log(3);
    [MenuItem("Free Lab/Diagnostics/Log Main Board Element 5")]
    private static void Element5() => Log(5);

    [MenuItem("Free Lab/Diagnostics/Log Active Free Lab Wire Names")]
    private static void LogActiveWireNames()
    {
        var lab = Object.FindFirstObjectByType<FreeLabManager>();
        if (!Application.isPlaying || lab == null)
        {
            Debug.LogWarning("Enter Free Mode Play Mode first, then run this diagnostic.");
            return;
        }

        if (lab.Connections.Count == 0)
        {
            Debug.Log("Active Free Lab wires: none.");
            return;
        }

        Debug.Log($"Active Free Lab wires ({lab.Connections.Count}):");
        foreach (FreeLabWire wire in lab.Connections)
        {
            if (wire != null)
                Debug.Log($"ACTIVE WIRE: {wire.gameObject.name}", wire.gameObject);
        }
    }

    private static void Log(int index)
    {
        var lab = Object.FindFirstObjectByType<FreeLabManager>();
        var board = lab != null ? lab.FindControlContaining("Main Board On/Off") : null;
        if (!Application.isPlaying || lab == null || board == null)
        {
            Debug.LogWarning("Enter Free Mode Play Mode first, then run this diagnostic.");
            return;
        }

        Debug.Log(board.DescribeEnableConfiguration(lab, index));
        foreach (FreeLabWire wire in lab.Connections)
        {
            if (wire != null)
                Debug.Log($"CONNECTED: {wire.EndpointA?.EndpointId} <-> {wire.EndpointB?.EndpointId}");
        }
    }
}

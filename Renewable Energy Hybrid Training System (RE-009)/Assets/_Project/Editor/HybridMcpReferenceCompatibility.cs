using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

// Editor-only workaround for duplicate Forms references in MCP CodeDom compilation.
[InitializeOnLoad]
internal static class HybridMcpReferenceCompatibility
{
    static HybridMcpReferenceCompatibility()
    {
        var type = typeof(MCPForUnity.Editor.Tools.ExecuteCode);
        var field = type.GetField("_codedomDuplicateAssemblies", BindingFlags.Static | BindingFlags.NonPublic);
        var excluded = field == null ? null : field.GetValue(null) as HashSet<string>;
        if (excluded != null) excluded.Add("System.Windows.Forms");
    }
}

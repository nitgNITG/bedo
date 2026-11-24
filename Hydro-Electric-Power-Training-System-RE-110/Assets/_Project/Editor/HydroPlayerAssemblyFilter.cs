using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;

// Retain the editor MCP connection; omit unused development assemblies from players.
public sealed class HydroPlayerAssemblyFilter : IFilterBuildAssemblies
{
    public int callbackOrder => 0;
    public string[] OnFilterAssemblies(BuildOptions options, string[] assemblies)
    {
        string[] editorOnly = { "MCPForUnity.Runtime.dll", "LeanLocalizationEditor.dll",
            "XCharts.Examples.dll", "Novell.Directory.Ldap.dll" };
        return assemblies.Where(path => !editorOnly.Contains(Path.GetFileName(path))).ToArray();
    }
}

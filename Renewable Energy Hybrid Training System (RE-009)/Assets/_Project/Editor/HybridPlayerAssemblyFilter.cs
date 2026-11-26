using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;

// Editor integration and unused sample dependencies do not belong in the RE-009 player.
public sealed class HybridPlayerAssemblyFilter : IFilterBuildAssemblies
{
    public int callbackOrder { get { return 0; } }
    public string[] OnFilterAssemblies(BuildOptions options, string[] assemblies)
    {
        string[] excluded = { "MCPForUnity.Runtime", "Novell.Directory.Ldap", "XCharts.Examples", "LeanLocalizationEditor" };
        return assemblies.Where(path => !excluded.Contains(Path.GetFileNameWithoutExtension(path), StringComparer.OrdinalIgnoreCase)).ToArray();
    }
}

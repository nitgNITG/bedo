using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

/// <summary>
/// Selection-based Editor copy/paste tool for MonoBehaviour components.
/// It never copies renderers, mesh filters, or materials.
/// </summary>
public static class ScriptComponentCopier
{
    private const string SourceObjectKey = "Bedo.ScriptComponentCopier.SourceObject";

    [MenuItem("GameObject/Copy Script Components", false, 49)]
    private static void CopyFromSelectedGameObject()
    {
        var source = Selection.activeGameObject;
        if (source == null)
        {
            return;
        }

        SessionState.SetString(SourceObjectKey, GlobalObjectId.GetGlobalObjectIdSlow(source).ToString());
        Notify($"Script source copied: {source.name}", source);
    }

    [MenuItem("GameObject/Copy Script Components", true)]
    private static bool CanCopyFromSelectedGameObject()
    {
        return Selection.activeGameObject != null;
    }

    [MenuItem("GameObject/Paste Script Components", false, 50)]
    private static void PasteOntoSelectedGameObject()
    {
        var target = Selection.activeGameObject;
        var source = GetSavedSource();

        if (target == null || source == null)
        {
            EditorUtility.DisplayDialog(
                "Paste Script Components",
                "Select a source GameObject and use GameObject > Copy Script Components first.",
                "OK");
            return;
        }

        if (source == target)
        {
            EditorUtility.DisplayDialog(
                "Paste Script Components",
                "Select a different GameObject as the target.",
                "OK");
            return;
        }

        var addedCount = 0;
        var updatedCount = 0;

        foreach (var sourceScript in source.GetComponents<MonoBehaviour>())
        {
            if (sourceScript == null)
            {
                Debug.LogWarning($"Missing script on '{source.name}' was skipped.", source);
                continue;
            }

            ComponentUtility.CopyComponent(sourceScript);
            var scriptType = sourceScript.GetType();
            var onlyOneAllowed = Attribute.IsDefined(scriptType, typeof(DisallowMultipleComponent), true);
            var existingScript = onlyOneAllowed ? target.GetComponent(scriptType) : null;

            if (existingScript != null)
            {
                Undo.RecordObject(existingScript, "Paste Script Component Values");
                ComponentUtility.PasteComponentValues(existingScript);
                EditorUtility.SetDirty(existingScript);
                updatedCount++;
            }
            else
            {
                ComponentUtility.PasteComponentAsNew(target);
                addedCount++;
            }
        }

        EditorUtility.SetDirty(target);
        Notify($"Pasted {addedCount} script(s); updated {updatedCount} existing script(s).", target);
    }

    [MenuItem("GameObject/Paste Script Components", true)]
    private static bool CanPasteOntoSelectedGameObject()
    {
        return Selection.activeGameObject != null && GetSavedSource() != null;
    }

    private static GameObject GetSavedSource()
    {
        var savedId = SessionState.GetString(SourceObjectKey, string.Empty);
        if (string.IsNullOrEmpty(savedId) || !GlobalObjectId.TryParse(savedId, out var objectId))
        {
            return null;
        }

        return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(objectId) as GameObject;
    }

    private static void Notify(string message, GameObject context)
    {
        Debug.Log(message, context);
        EditorWindow.focusedWindow?.ShowNotification(new GUIContent(message));
    }
}

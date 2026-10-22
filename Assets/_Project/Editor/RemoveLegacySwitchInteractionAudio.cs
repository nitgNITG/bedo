#if UNITY_EDITOR
using System.Collections.Generic;
using TutorialFramework;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;

internal static class RemoveLegacySwitchInteractionAudio
{
    private const string SwitchClipName = "Switch";

    [InitializeOnLoadMethod]
    private static void RunAfterScriptReload()
    {
        EditorApplication.delayCall += RemoveFromOpenScene;
    }

    [MenuItem("Tools/Bedo/Remove Legacy Switch Interaction Audio")]
    private static void RemoveFromOpenScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        var sourcesToRemove = new HashSet<AudioSource>();
        int removedListeners = 0;

        foreach (Interaction interaction in
            Object.FindObjectsByType<Interaction>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None))
        {
            if (interaction == null)
                continue;

            for (int index = interaction.onClicked.GetPersistentEventCount() - 1;
                 index >= 0;
                 index--)
            {
                if (interaction.onClicked.GetPersistentTarget(index)
                    is not AudioSource source ||
                    source.clip == null ||
                    source.clip.name != SwitchClipName ||
                    interaction.onClicked.GetPersistentMethodName(index) != "Play")
                {
                    continue;
                }

                sourcesToRemove.Add(source);
                UnityEventTools.RemovePersistentListener(
                    interaction.onClicked,
                    index);
                removedListeners++;
            }
        }

        foreach (AudioSource source in sourcesToRemove)
        {
            if (source != null)
                Object.DestroyImmediate(source);
        }

        if (removedListeners == 0)
            return;

        EditorSceneManager.MarkSceneDirty(
            EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log(
            $"Removed {removedListeners} legacy Switch click listeners and " +
            $"{sourcesToRemove.Count} legacy AudioSource components.");
    }
}
#endif

using UnityEngine;
using Obvious.Soap;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif
[CreateAssetMenu(fileName = "scriptable_list_string", menuName = "Soap/ScriptableLists/string")]
public class ScriptableListString : ScriptableList<string>
{

    protected override void OnEnable()
    {
        if(_resetOn != ResetType.None)
        {
            Clear();
        }
     

        if (_resetOn == ResetType.SceneLoaded)
            SceneManager.sceneLoaded += OnSceneLoaded;
#if UNITY_EDITOR
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
    }

    protected override void OnDisable()
    {
        if (_resetOn == ResetType.SceneLoaded)
            SceneManager.sceneLoaded -= OnSceneLoaded;
#if UNITY_EDITOR
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
#endif
    }

#if UNITY_EDITOR
    public override void OnPlayModeStateChanged(PlayModeStateChange playModeStateChange)
    {
        if (playModeStateChange == PlayModeStateChange.EnteredEditMode && _resetOn != ResetType.None)
            Clear();
    }
#endif
}


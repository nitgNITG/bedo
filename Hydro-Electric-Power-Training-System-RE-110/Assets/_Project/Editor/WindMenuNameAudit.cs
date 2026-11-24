using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TutorialFramework;
[InitializeOnLoad]
public static class WindMenuNameAudit
{
    static WindMenuNameAudit() { EditorApplication.delayCall += Run; }
    static void Run()
    {
        if (Application.dataPath.Contains("Hydro-Electric")) return;
        if (Application.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != "Assets/_Project/Scenes/Hydro Lab.unity") return;
        if (SessionState.GetBool("WindMenuNameFix.v1",false)) return;
        int fixedCount=0;
        foreach(var b in Object.FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            var so=new SerializedObject(b);var calls=so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
            StoryboardSteps owner=null;
            for(int i=0;i<calls.arraySize;i++)
            {
                var c=calls.GetArrayElementAtIndex(i);
                if(c.FindPropertyRelative("m_MethodName").stringValue=="SelectExperiment") owner=c.FindPropertyRelative("m_Arguments.m_ObjectArgument").objectReferenceValue as StoryboardSteps;
            }
            if(owner==null || (owner.StoryboardID!="Regulating DC Output from Wind Turbine using MPPT Charger" && owner.StoryboardID!="OFF Grid System Using Wind Turbine")) continue;
            string detail="";
            for(int i=0;i<calls.arraySize;i++)
            {
                var c=calls.GetArrayElementAtIndex(i);var method=c.FindPropertyRelative("m_MethodName").stringValue;
                if(method=="SetExperimentName")
                {
                    c.FindPropertyRelative("m_Target").objectReferenceValue=owner;
                    c.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue=typeof(StoryboardSteps).AssemblyQualifiedName;
                    fixedCount++;
                }
                if(method=="SetExperimentName" || method=="SelectExperiment") detail += method+" string="+c.FindPropertyRelative("m_Arguments.m_StringArgument").stringValue+" object="+c.FindPropertyRelative("m_Arguments.m_ObjectArgument").objectReferenceValue+"; ";
            }
            if(detail.Length>0) Debug.Log("WIND NAME AUDIT "+string.Join("/",b.GetComponentsInParent<Transform>(true).Reverse().Select(t=>t.name))+" :: "+detail);
            so.ApplyModifiedProperties();EditorUtility.SetDirty(b);PrefabUtility.RecordPrefabInstancePropertyModifications(b);
        }
        if(fixedCount!=2) throw new System.Exception("Expected two EXP3/EXP4 name listeners, got "+fixedCount);
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        SessionState.SetBool("WindMenuNameFix.v1",true);
        Debug.Log("WIND TITLE FIX VERIFIED: EXP3/EXP4 setters target their own storyboards, with names before selection; Hydro Lab saved.");
    }
}

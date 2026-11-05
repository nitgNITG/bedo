using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class WindAcceptanceBuild
{
 const string Output="Reports/BuildValidation/Build.txt";
 static WindAcceptanceBuild(){EditorApplication.update+=Tick;}
 static void Tick(){if(!File.Exists("Temp/acceptance-build.flag")||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
  File.Delete("Temp/acceptance-build.flag");
  File.WriteAllText(Output,"Unity "+Application.unityVersion+"\nOS "+SystemInfo.operatingSystem+"\nCPU "+SystemInfo.processorType+"\nRAM "+SystemInfo.systemMemorySize+" MB\nGPU "+SystemInfo.graphicsDeviceName+"\nGraphics memory "+SystemInfo.graphicsMemorySize+" MB\n");
  try{
   if(SceneManager.GetActiveScene().isDirty)throw new Exception("Unsaved scene; refusing build");
   var paths=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();
   if(!paths.SequenceEqual(new[]{"Assets/_Project/Scenes/Main Menu.unity","Assets/_Project/Scenes/PV Lab.unity"}))throw new Exception("Unexpected delivery scene list");
   File.AppendAllText(Output,"Enabled scenes: "+string.Join(", ",paths)+"\n");
   File.AppendAllText(Output,"WebGL installed: "+BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL,BuildTarget.WebGL)+"\n");
   if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL,BuildTarget.WebGL))File.AppendAllText(Output,"BLOCKED WebGL: target module is not installed; browser zoom/performance cannot be certified.\n");
   File.AppendAllText(Output,"Windows installed: "+BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone,BuildTarget.StandaloneWindows64)+"\n");
   var target=NamedBuildTarget.Standalone;var previous=PlayerSettings.GetScriptingBackend(target);
   try{
    PlayerSettings.SetScriptingBackend(target,ScriptingImplementation.IL2CPP);
    var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=paths,locationPathName="Builds/Acceptance/Windows/WindLab.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
    File.AppendAllText(Output,"Windows IL2CPP result: "+result.summary.result+"; errors="+result.summary.totalErrors+"; warnings="+result.summary.totalWarnings+"; duration="+result.summary.totalTime+"; bytes="+result.summary.totalSize+"\n");
    foreach(var step in result.steps)foreach(var message in step.messages)if(message.type==LogType.Error||message.type==LogType.Exception)File.AppendAllText(Output,message.content+"\n");
   }finally{PlayerSettings.SetScriptingBackend(target,previous);}
  }catch(Exception e){File.AppendAllText(Output,"BLOCKED/FAIL "+e+"\n");}
  File.AppendAllText(Output,"FINISHED. Build output is ignored by Git; runtime/performance approval is a separate check.\n");
 }
}

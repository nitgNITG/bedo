using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TutorialFramework;
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.SceneManagement;

public static partial class HybridGuidedCompletion
{
 struct HardwarePin {public string name;public Transform model;public int camera; public HardwarePin(string n,Transform t,int c){name=n;model=t;camera=c;}}
 static HardwarePin FindPin(string name,Vector3 approximate,int camera){
 var candidates=At("Equipment/Wind Lab").GetComponentsInChildren<Renderer>(true).Where(r=>!Path(r.transform).Contains("/UI/")&&r.bounds.size.magnitude<1.5f).OrderBy(r=>Vector3.Distance(r.transform.position,approximate)).ToArray();
 var r=candidates.First();if(Vector3.Distance(r.transform.position,approximate)>.2f){
 var authored=All<FreeLabWindController>().Single().sockets.Select(s=>s.endpoint.transform).OrderBy(t=>Vector3.Distance(t.position,approximate)).First();
 if(Vector3.Distance(authored.position,approximate)<.2f)return new HardwarePin(name,authored,camera);
 throw new Exception("No physical terminal at "+name+" "+approximate+" nearest="+Path(r.transform)+" "+r.transform.position);}
 return new HardwarePin(name,r.transform,camera);
 }
 static void PlaceEndpoint(Interaction point,GameObject marker,HardwarePin pin){
 point.name=pin.name;point.transform.SetPositionAndRotation(pin.model.position,pin.model.rotation);
 marker.transform.position=pin.model.position;marker.SetActive(false);point.Allow=false;Dirty(point);
 }
 static void Reconnect(StoryboardSteps.Step row,HardwarePin a,HardwarePin b){
 var w=(WireInteraction)row.interaction;w.name=a.name+" to "+b.name;PlaceEndpoint(w.pointA,w.markerA,a);PlaceEndpoint(w.pointB,w.markerB,b);
 w.wireObject.name=w.name;row.onStepStart=new UnityEvent();Route(row.onStepStart,a.camera);w.onFirstPointClicked=new UnityEvent();Route(w.onFirstPointClicked,b.camera);Dirty(w);
 }
 [MenuItem("RE009/Correct Mains And AC Load Storyboard Order")]
 public static void CorrectStoryboardWires(){
 if(Application.isPlaying)throw new Exception("Stop Play Mode first.");
 var board=All<StoryboardSteps>().Single(b=>b.StoryboardID==HybridExperimentSetup.Id);
 var mains=board.steps.Where(s=>s.uiObject&&s.uiObject.name=="Subtitle Step 1").ToArray();
 if(mains.Length!=12||mains.Any(s=>!(s.interaction is WireInteraction)))throw new Exception("Expected exactly twelve mains rows.");
 var module=At("Equipment/Wind Lab/Wind-Solar-Hybrid sys");
 var inputs=new HardwarePin[7,2];var outputs=new HardwarePin[6,2];
 // Explicit physical left-input/right-output mapping. Do not reuse an incoming terminal for an outgoing wire.
 string[] labels={"Main Board","Hybrid","DC LED","Off-grid Inverter","Battery","AC Halogen","Dump Load"};
 Vector3[] left={new Vector3(-147.74f,167.57f,-52.69f),Vector3.zero,new Vector3(-122.27f,185.88f,-52.38f),new Vector3(-122.17f,170.34f,-52.37f),new Vector3(-142.51f,170.38f,-52.38f),new Vector3(-137.55f,185.88f,-52.38f),new Vector3(-122.14f,154.84f,-52.37f)};
 Vector3[] right={left[0],Vector3.zero,new Vector3(-115.76f,185.88f,-52.38f),new Vector3(-115.84f,170.34f,-52.37f),new Vector3(-131.76f,170.38f,-52.38f),new Vector3(-131.04f,185.88f,-52.38f)};
 float[] yblack={166.31f,0,185.03f,169.48f,169.50f,185.03f,154.00f};int[] cams={1,17,3,5,4,3,8};
 for(int i=0;i<7;i++)for(int color=0;color<2;color++){
 string c=color==0?"Red":"Black";var pos=left[i];if(color==1)pos.y=yblack[i];
 inputs[i,color]=i==1?new HardwarePin("Hybrid Mains "+c+" Input",module.Find(color==0?"Cylinder439 (15)":"Cylinder440 (15)"),17):FindPin(labels[i]+" Mains "+c+" Input",pos,cams[i]);
 if(i<6){pos=right[i];if(color==1)pos.y=yblack[i];outputs[i,color]=i==1?new HardwarePin("Hybrid Mains "+c+" Output",module.Find(color==0?"Cylinder439 (14)":"Cylinder440 (14)"),17):FindPin(labels[i]+" Mains "+c+" Output",pos,cams[i]);}
 }
 // Preflight all physical pins before modifying any row.
 for(int link=0;link<6;link++)for(int color=0;color<2;color++)Reconnect(mains[link*2+color],outputs[link,color],inputs[link+1,color]);
 var loads=board.steps.Where(s=>s.uiObject&&s.uiObject.name=="Subtitle Step 8"&&s.interaction is WireInteraction).ToArray();if(loads.Length!=8)throw new Exception("Expected eight AC lamp links.");
 var pins=loads.Select(s=>(WireInteraction)s.interaction).SelectMany(w=>new[]{w.pointA,w.pointB}).GroupBy(p=>p.name).ToDictionary(g=>g.Key,g=>new HardwarePin(g.Key,g.First().transform,CameraFor(g.Key)));
 // Snapshot transforms because endpoints themselves are about to move.
 var snapshots=new List<GameObject>();foreach(string key in pins.Keys.ToArray()){var p=pins[key];var g=new GameObject("Pin snapshot");g.transform.SetPositionAndRotation(p.model.position,p.model.rotation);snapshots.Add(g);pins[key]=new HardwarePin(key,g.transform,p.camera);}
 for(int link=0;link<4;link++)for(int color=0;color<2;color++){
 string c=color==0?"L":"N";string start=link==0?"Off grid inverter Ac Output "+c:link==1?"AC Halogen "+c+" 1":link==2?"AC Halogen "+c+" 2":"AC LED "+c+" 2";
 string end=link==0?"AC Halogen "+c+" 1":link==1?"AC Halogen "+c+" 2":link==2?"AC LED "+c+" 2":"AC LED "+c+" 1";
 // Existing terminal names number the bottom halogen as 1, as in the authored wind scene.
 Reconnect(loads[link*2+color],pins[start],pins[end]);
 }
 foreach(var g in snapshots)UnityEngine.Object.DestroyImmediate(g);
 var sys=board.GetComponentInChildren<HybridTrainingController>(true);sys.mainWiring.guided=mains.Select(s=>(WireInteraction)s.interaction).ToArray();
 foreach(bool led in new[]{false,true}){
 var lamps=sys.lamps.Where(l=>l.led==led).OrderBy(l=>l.light.transform.position.y).ToArray();
 if(lamps.Length!=2)throw new Exception("Expected two lamps in each AC module.");
 for(int i=0;i<2;i++)lamps[i].connection.guided=loads.Take((led?6:2)+i*2).Select(s=>(WireInteraction)s.interaction).ToArray();
 }
 Dirty(sys);Dirty(board);EditorSceneManager.MarkSceneDirty(board.gameObject.scene);EditorSceneManager.SaveScene(board.gameObject.scene);
 File.WriteAllLines("Temp/RE009Planning/corrected-topology.txt",mains.Concat(loads).Select(s=>s.interaction.name));
 }
}

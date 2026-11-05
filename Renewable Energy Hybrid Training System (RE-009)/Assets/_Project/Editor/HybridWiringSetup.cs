using System;
using System.Linq;
using System.Collections.Generic;
using TutorialFramework;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Splines;
using Unity.Mathematics;
using Object=UnityEngine.Object;

public static class HybridWiringSetup
{
    static T[] All<T>() where T:Component=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(c=>c.gameObject.scene==UnityEngine.SceneManagement.SceneManager.GetActiveScene()).ToArray();
    class Terminal{public string name;public Vector3 position;public Quaternion rotation;public string color;public int camera;}
    static Dictionary<string,Terminal> terminals;
    static WireInteraction[] templates;
    static StoryboardSteps board;
    static Transform root,wires,pointers,ui;
    static Terminal Existing(string name,string color,int camera)
    {
        var free=All<FreeLabWindController>().Single();
        var p=free.sockets.Select(s=>s.endpoint.transform).FirstOrDefault(t=>t.name.Trim().Equals(name.Trim(),StringComparison.OrdinalIgnoreCase));
        if(!p)p=templates.SelectMany(w=>new[]{w.pointA.transform,w.pointB.transform}).First(t=>t.name.Trim().Equals(name.Trim(),StringComparison.OrdinalIgnoreCase));
        return terminals[name]=new Terminal{name=name,position=p.position,rotation=p.rotation,color=color,camera=camera};
    }
    static Terminal Physical(string name,Transform model,string color,int camera)
    {return terminals[name]=new Terminal{name=name,position=model.position,rotation=model.rotation,color=color,camera=camera};}
    static void Camera(UnityEvent ev,int number)
    {
        var trigger=All<DelayedEventTrigger>().Single(t=>t.transform.parent&&t.transform.parent.name=="Camera move"&&t.name=="Move camera to pos "+number);
        UnityEventTools.AddVoidPersistentListener(ev,trigger.TriggerDelay);
    }
    static Interaction Endpoint(Terminal t,Transform parent,out GameObject plug,out GameObject marker,int slot)
    {
        var source=templates.First(w=>w.plugA&&w.plugA.name.IndexOf(t.color,StringComparison.OrdinalIgnoreCase)>=0);
        var go=new GameObject(t.name);go.transform.SetParent(parent,false);go.transform.SetPositionAndRotation(t.position,t.rotation);
        var point=go.AddComponent<Interaction>();EditorUtility.CopySerialized(source.pointA,point);point.Allow=false;point.onClicked=new UnityEvent();
        var collider=go.AddComponent<BoxCollider>();EditorUtility.CopySerialized(source.pointA.GetComponent<BoxCollider>(),collider);
        go.AddComponent<OnMouseDownEvent>();
        plug=Object.Instantiate(source.plugA,go.transform,false);plug.name=t.color+" Plug";
        plug.transform.localPosition=source.plugA.transform.localPosition+Vector3.forward*slot*1.1f;
        plug.SetActive(false);
        // Reuse the existing banana highlight rather than a single-click arrow.
        marker=Object.Instantiate(source.markerA,go.transform,false);marker.name="Possible Connection";
        marker.transform.position=t.position;marker.SetActive(false);
        return point;
    }
    static readonly Dictionary<string,int> slots=new Dictionary<string,int>();
    static WireInteraction Connect(int instruction,string a,string b)
    {
        var ta=terminals[a];var tb=terminals[b];var folder=root.Find("Step "+instruction);
        if(!folder){folder=new GameObject("Step "+instruction).transform;folder.SetParent(root,false);}
        string name=a+" to "+b;var go=new GameObject(name);go.transform.SetParent(folder,false);
        int Slot(string key){slots.TryGetValue(key,out int n);slots[key]=n+1;return n;}
        var w=go.AddComponent<WireInteraction>();w.pointA=Endpoint(ta,go.transform,out w.plugA,out w.markerA,Slot(a));w.pointB=Endpoint(tb,go.transform,out w.plugB,out w.markerB,Slot(b));
        w.onCompleted=new UnityEvent();w.onUndone=new UnityEvent();w.onFirstPointClicked=new UnityEvent();Camera(w.onFirstPointClicked,tb.camera);
        w.wireObject=WindExp4Step1Setup.Wire(wires,name,w.pointA,w.pointB,w.plugA.transform,w.plugB.transform,ta.color=="Black",templates,ta.color,"Assets/_Project/Models/Generated/Hybrid Wires");
        // Straight leads at each plug; make bends after the plug body, not at its entrance.
        var container=w.wireObject.GetComponentInChildren<SplineContainer>(true);var spline=container.Spline;
        Vector3 start=w.plugA.transform.position,end=w.plugB.transform.position;
        Vector3 outA=-w.plugA.transform.forward,outB=-w.plugB.transform.forward;
        float lead=1.5f;float z=Mathf.Min(start.z,end.z)-2.5f-(board.steps.Count%5)*.35f;
        var points=new[]{start,start+outA*lead,new Vector3(start.x,start.y,z),new Vector3(end.x,end.y,z),end+outB*lead,end};
        container.Splines=new[]{new Spline(points.Select(p=>(float3)container.transform.InverseTransformPoint(p)),TangentMode.Linear,false)};
        container.GetComponent<SplineExtrude>().Rebuild();
        foreach(var definition in w.wireObject.GetComponentsInChildren<FreeLabWireDefinition>(true))Object.DestroyImmediate(definition);
        foreach(var legacy in w.wireObject.GetComponentsInChildren<FreeLabWire>(true))Object.DestroyImmediate(legacy);
        foreach(var collider in w.wireObject.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);
        var row=new StoryboardSteps.Step{interaction=w,uiObject=ui.Find("Subtitle Step "+instruction).gameObject,onStepStart=new UnityEvent(),onStepEnd=new UnityEvent()};Camera(row.onStepStart,ta.camera);board.steps.Add(row);
        return w;
    }
    [MenuItem("RE009/Prepare Hybrid Guided Wiring")]
    public static void Prepare()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
        if(All<StoryboardSteps>().Any(b=>b.StoryboardID==HybridExperimentSetup.Id))throw new InvalidOperationException("Hybrid board already exists.");
        templates=All<WireInteraction>().Where(w=>w.pointA&&w.pointB&&w.plugA&&w.plugB&&w.markerA&&w.markerB&&w.wireObject&&w.wireObject.GetComponentInChildren<SplineContainer>(true)).ToArray();
        terminals=new Dictionary<string,Terminal>();slots.Clear();
        var owner=new GameObject("Hybrid Experiment 1");owner.transform.SetParent(All<Transform>().Single(t=>t.name=="Experiments"&&t.parent==null),false);owner.SetActive(false);
        board=owner.AddComponent<StoryboardSteps>();var so=new SerializedObject(board);so.FindProperty("storyboardID").stringValue=HybridExperimentSetup.Id;so.ApplyModifiedPropertiesWithoutUndo();
        root=new GameObject("Steps").transform;root.SetParent(owner.transform,false);wires=new GameObject("Wires").transform;wires.SetParent(root,false);pointers=new GameObject("Pointers").transform;pointers.SetParent(root,false);
        ui=All<Transform>().Single(t=>t.name=="Hybrid Experiment UI");
        var module=All<Transform>().Single(t=>HybridExperimentSetup.PathOf(t)=="Equipment/Wind Lab/Wind-Solar-Hybrid sys");
        var names=new[]{("Hybrid Battery +ve","Cylinder390 (9)","Yellow"),("Hybrid Battery -ve","Cylinder389 (9)","Blue"),("Hybrid Wind U","Cylinder435 (29)","Red"),("Hybrid Wind V","Cylinder390 (10)","Yellow"),("Hybrid Wind W","Cylinder389 (8)","Blue"),("Hybrid Solar +ve","Cylinder390 (11)","Yellow"),("Hybrid Solar -ve","Cylinder389 (7)","Blue"),("Hybrid Dump 1","Cylinder390 (6)","Yellow"),("Hybrid Dump 2","Cylinder390 (7)","Yellow"),("Hybrid Mains Red","Cylinder439 (15)","Red"),("Hybrid Mains Black","Cylinder440 (15)","Black")};
        foreach(var n in names)Physical(n.Item1,module.Find(n.Item2),n.Item3,8);
        foreach(var n in new[]{("Main Board Red 1","Red",1),("Main Board Black 1","Black",1),("Battery Input Red","Red",4),("Battery Input Black","Black",4),("Off grid inverter red input","Red",5),("Off-grid inverter black input","Black",5),("AC Halogen Red input","Red",3),("AC Halogen Lamp Module Input Black","Black",3),("AC LED Red input","Red",3),("AC LED Lamp Module Input Black","Black",3),("Battery +ve","Yellow",4),("Battery -ve","Blue",4),("Off-Grid Inverter Battery +ve","Yellow",5),("Off-Grid Inverter Battery -ve","Blue",5),("Off grid inverter Ac Output L","Red",5),("Off grid inverter Ac Output N","Black",5),("wind mill module L1","Red",11),("wind mill module L2","Yellow",11),("wind mill module L3","Blue",11)})Existing(n.Item1,n.Item2,n.Item3);
        string[] red={"Main Board Red 1","Hybrid Mains Red","Battery Input Red","Off grid inverter red input","AC Halogen Red input","AC LED Red input"};
        string[] black={"Main Board Black 1","Hybrid Mains Black","Battery Input Black","Off-grid inverter black input","AC Halogen Lamp Module Input Black","AC LED Lamp Module Input Black"};
        for(int i=1;i<red.Length;i++){Connect(1,red[i-1],red[i]);Connect(1,black[i-1],black[i]);}
        var dump=All<Transform>().Single(t=>HybridExperimentSetup.PathOf(t)=="Equipment/Wind Lab/UNUSED (MDOULE SYSTEM)/dump load").GetComponent<Renderer>().bounds;
        var dumpPins=All<Renderer>().Where(r=>HybridExperimentSetup.PathOf(r.transform).StartsWith("Equipment/Wind Lab/Bananas (Modules)/")&&Mathf.Abs(r.transform.position.x-dump.center.x)<1&&r.transform.position.y>dump.min.y&&r.transform.position.y<dump.max.y&&r.sharedMaterials.Any(m=>m&&m.name.Contains("YELLOW BANANA"))).OrderByDescending(r=>r.transform.position.y).ToArray();
        if(dumpPins.Length!=2)throw new Exception("Cannot identify both dump-load terminals: "+dumpPins.Length);
        Physical("Dump Load 400W 1",dumpPins[0].transform,"Yellow",8);Physical("Dump Load 400W 2",dumpPins[1].transform,"Yellow",8);
        Connect(4,"Hybrid Dump 1","Dump Load 400W 1");Connect(4,"Hybrid Dump 2","Dump Load 400W 2");
        Connect(5,"Hybrid Battery +ve","Battery +ve");Connect(5,"Hybrid Battery -ve","Battery -ve");
        Connect(7,"Off-Grid Inverter Battery +ve","Battery +ve");Connect(7,"Off-Grid Inverter Battery -ve","Battery -ve");
        Connect(8,"wind mill module L1","Hybrid Wind U");Connect(8,"wind mill module L2","Hybrid Wind V");Connect(8,"wind mill module L3","Hybrid Wind W");
        foreach(string type in new[]{"AC Halogen","AC LED"})for(int number=1;number<=2;number++){
            string a=type+" L "+number,b=type+" N "+number;Existing(a,"Red",3);Existing(b,"Black",3);
            // Daisy-chain within each lamp module; the two modules share the inverter outputs.
            Connect(9,number==1?"Off grid inverter Ac Output L":type+" L 1",a);
            Connect(9,number==1?"Off grid inverter Ac Output N":type+" N 1",b);
        }
        var equipment=All<Transform>().Single(t=>HybridExperimentSetup.PathOf(t)=="Equipment/Wind Lab");
        var solarRoots=new[]{equipment.Find("UNUSED (Solar)"),equipment.Find("Solar panel 2/UNUSED (Solar) (1)"),equipment.Find("Solar panel 3/UNUSED (Solar) (1)")};
        for(int i=0;i<3;i++){Physical("PV "+(i+1)+" +ve",solarRoots[i].Find("Cylinder482"),"Yellow",0);Physical("PV "+(i+1)+" -ve",solarRoots[i].Find("Cylinder483"),"Blue",0);}
        Connect(10,"PV 1 +ve","PV 2 +ve");Connect(10,"PV 1 -ve","PV 2 -ve");Connect(10,"PV 2 -ve","PV 3 +ve");Connect(10,"PV 2 +ve","Hybrid Solar +ve");Connect(10,"PV 3 -ve","Hybrid Solar -ve");
        EditorUtility.SetDirty(board);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(owner.scene);EditorSceneManager.SaveScene(owner.scene);
        Debug.Log("RE009 guided wiring staged inactive: "+board.steps.Count+" independent wires. Communications and control steps are added separately before enabling the menu.");
    }
}

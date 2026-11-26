using System;
using System.Collections.Generic;
using System.Linq;
using TutorialFramework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using Object=UnityEngine.Object;

public static class WindExp4Step1Setup
{
    public static bool OnlyNewBlack;
    static Transform Steps()=>Object.FindObjectsByType<StoryboardSteps>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(o=>o.name=="Exp 4").SelectMany(o=>o.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="Steps");
    static Transform Banana(Transform group,string name)=>group.Cast<Transform>().Single(t=>t.name.Trim()==name);
    static Transform Plug(Transform banana)=>banana.Cast<Transform>().SingleOrDefault(t=>t.name.Contains("Plug"));
    static void Dirty(Object o){EditorUtility.SetDirty(o);PrefabUtility.RecordPrefabInstancePropertyModifications(o);}
    static string ColorName(bool black)=>black?"Black":"Red";
    static void EnsureFolder(string folder)
    {
        if(AssetDatabase.IsValidFolder(folder))return;
        int i=folder.LastIndexOf('/');EnsureFolder(folder.Substring(0,i));AssetDatabase.CreateFolder(folder.Substring(0,i),folder.Substring(i+1));
    }
    static GameObject Pointer(Transform banana,Transform plug,GameObject current,bool newBanana,Transform folder,GameObject template,float height,string name)
    {
        // Copied MPPT markers no longer describe the replacement terminal.
        bool usable=current!=null && !current.name.Contains("MPPT") && !current.name.Contains("PWM");
        if(usable)return current;
        var existing=folder.Find(name);
        var marker=existing?existing.gameObject:Object.Instantiate(template,folder,false);
        marker.name=name;
        marker.transform.position=plug.position+Vector3.up*height;
        marker.SetActive(false);Dirty(marker.transform);
        return marker;
    }
    public static GameObject Wire(Transform folder,string name,Interaction a,Interaction b,Transform plugA,Transform plugB,bool black,WireInteraction[] templates,string plugColor=null,string outputFolder="Assets/_Project/Models/Generated/EXP4 Step1 Wires")
    {
        var existing=folder.Find(name);if(existing)return existing.gameObject;
        var source=templates.Where(w=>w.pointA&&w.pointB&&w.wireObject&&w.wireObject.GetComponentInChildren<SplineContainer>(true)&&w.plugA&&w.plugA.name.Contains(plugColor??ColorName(black)))
            .OrderBy(w=>Mathf.Min(Vector3.Distance(w.pointA.transform.position,a.transform.position)+Vector3.Distance(w.pointB.transform.position,b.transform.position),Vector3.Distance(w.pointB.transform.position,a.transform.position)+Vector3.Distance(w.pointA.transform.position,b.transform.position))).First();
        var go=Object.Instantiate(source.wireObject,folder,false);go.name=name;
        var container=go.GetComponentInChildren<SplineContainer>(true);
        var sourceContainer=source.wireObject.GetComponentInChildren<SplineContainer>(true);
        var spline=sourceContainer.Splines.OrderByDescending(s=>s.Count).First();
        var original=spline.Select(k=>sourceContainer.transform.TransformPoint((Vector3)k.Position)).ToArray();
        if(original.Length<2)throw new InvalidOperationException("Wire template has no usable curve.");
        var start=plugA.position;var end=plugB.position;
        bool reverse=Vector3.Distance(original[0],end)+Vector3.Distance(original[original.Length-1],start)<Vector3.Distance(original[0],start)+Vector3.Distance(original[original.Length-1],end);
        if(reverse)Array.Reverse(original);
        var oldStart=original[0];var oldEnd=original[original.Length-1];
        var lengths=new float[original.Length];for(int i=1;i<original.Length;i++)lengths[i]=lengths[i-1]+Vector3.Distance(original[i],original[i-1]);
        var local=new List<float3>();
        for(int i=0;i<original.Length;i++)
        {
            float t=lengths[i]/Mathf.Max(.001f,lengths[lengths.Length-1]);
            Vector3 world=original[i]+Vector3.Lerp(start-oldStart,end-oldEnd,t);
            local.Add((float3)container.transform.InverseTransformPoint(world));
        }
        container.Splines=new[]{new Spline(local,TangentMode.AutoSmooth,false)};
        // Rebuild into an independent mesh, never the template's mesh asset.
        var extrude=container.GetComponent<SplineExtrude>();
        EnsureFolder(outputFolder);
        var mesh=new Mesh{name=name};AssetDatabase.CreateAsset(mesh,AssetDatabase.GenerateUniqueAssetPath(outputFolder+"/"+name+".asset"));
        extrude.Container=container;extrude.targetMesh=mesh;extrude.Rebuild();
        foreach(var renderer in go.GetComponentsInChildren<Renderer>(true))renderer.enabled=renderer.gameObject==container.gameObject;
        container.gameObject.SetActive(true);go.SetActive(false);Dirty(container);Dirty(extrude);Dirty(go);
        Debug.Log("EXP4 CREATED WIRE: "+name+" (closest template: "+source.wireObject.name+")");
        return go;
    }
    [MenuItem("PV System/Complete NEW EXP4 Step1 Bananas")]
    public static void Configure()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(Application.isPlaying||scene.isDirty||scene.path!="Assets/_Project/Scenes/Hybrid Lab.unity")throw new InvalidOperationException("Open saved Hybrid Lab in Edit Mode.");
        var steps=Steps();var step=steps.Find("Step 1");var wires=steps.Find("Wires");var pointers=steps.Find("Pointers");
        if(step==null||wires==null||pointers==null)throw new InvalidOperationException("Missing new EXP4 folders.");
        var templates=steps.GetComponentsInChildren<WireInteraction>(true).Where(w=>w.pointA&&w.pointB&&w.wireObject&&w.plugA&&w.plugB).ToArray();
        var redTemplate=templates.First(w=>w.plugA.name.Contains("Red"));
        var blackTemplate=templates.First(w=>w.plugA.name.Contains("Black"));
        var pointerTemplate=redTemplate.markerA;
        float pointerHeight=Mathf.Abs(pointerTemplate.transform.position.y-redTemplate.plugA.transform.position.y);
        if(pointerHeight<.2f)pointerHeight=1.5f;
        var specs=new[]{
            (group:"Full bringe rectifier output Black to AC Halogen Black input",a:"Full Brindge Rectifier Black output",b:"AC Halogen Lamp Module Input Black",black:true,wire:"Full bringe rectifier output Black to AC Halogen Black input",ma:"Full Bridge Rectifier black output",mb:"AC Halogen Black input"),
            (group:"Full bringe rectifier output Red to AC Halogen Red input",a:"AC Halogen Red input",b:"Full bringe rectifier Red output",black:false,wire:"Full bringe rectifier output Red to AC Halogen Red input",ma:"AC Halogen Red input",mb:"Full Bridge Rectifier red output"),
            (group:"AC Halogen Red to Off-grid Inverter Red",a:"AC Halogen Red output",b:"Off grid inverter red input",black:false,wire:"AC Halogen Red output to Off-grid Inverter Red input",ma:"AC Halogen Red output",mb:"Off-grid Inverter Red input"),
            (group:"AC Halogen Black to Offgrid Inverter Black",a:"Off-grid inverter black input",b:"AC Halogen output black",black:true,wire:"AC Halogen Black output to Off-grid Inverter Black input",ma:"Off-grid Inverter Black input",mb:"AC Halogen Black output"),
            (group:"Off grid inverter output red to Battery input Red",a:"Off grid inverter output red",b:"Battery Input Red",black:false,wire:"Off-grid Inverter Red output to Battery input Red",ma:"Off-grid Inverter Red output",mb:"Battery Input Red"),
            (group:"Off grid inverter output red to Battery Black",a:"Battery Input Black",b:"Off grid inverter output red",black:true,wire:"Off-grid Inverter Black output to Battery input Black",ma:"Battery Input Black",mb:"Off-grid Inverter Black output")
        };
        int newBananas=0,plugs=0;
        foreach(var spec in specs.Where(s=>!OnlyNewBlack||s.group=="Full bringe rectifier output Black to AC Halogen Black input"))
        {
            var group=step.Find(spec.group);if(group==null)throw new InvalidOperationException("Missing connection: "+spec.group);
            var ba=Banana(group,spec.a);var bb=Banana(group,spec.b);
            var template=spec.black?blackTemplate:redTemplate;
            foreach(var banana in new[]{ba,bb})
            {
                bool missing=banana.GetComponent<Interaction>()==null;
                if(!banana.GetComponent<BoxCollider>()){var collider=banana.gameObject.AddComponent<BoxCollider>();EditorUtility.CopySerialized(template.pointA.GetComponent<BoxCollider>(),collider);Dirty(collider);}
                if(!banana.GetComponent<OnMouseDownEvent>())banana.gameObject.AddComponent<OnMouseDownEvent>();
                if(missing){var interaction=banana.gameObject.AddComponent<Interaction>();EditorUtility.CopySerialized(template.pointA,interaction);interaction.Allow=false;Dirty(interaction);newBananas++;}
                var plug=Plug(banana);
                if(plug==null){var copy=Object.Instantiate(template.plugA,banana,false);copy.name=ColorName(spec.black)+" Plug";plug=copy.transform;plugs++;}
                plug.localPosition=template.plugA.transform.localPosition;
                plug.localRotation=template.plugA.transform.localRotation;plug.localScale=template.plugA.transform.localScale;
                plug.gameObject.SetActive(false);Dirty(plug);
                Dirty(banana.gameObject);
            }
            var w=group.GetComponent<WireInteraction>();if(w==null)w=group.gameObject.AddComponent<WireInteraction>();
            w.pointA=ba.GetComponent<Interaction>();w.pointB=bb.GetComponent<Interaction>();w.plugA=Plug(ba).gameObject;w.plugB=Plug(bb).gameObject;
            w.markerA=Pointer(ba,Plug(ba),w.markerA,false,pointers,pointerTemplate,pointerHeight,spec.ma);
            w.markerB=Pointer(bb,Plug(bb),w.markerB,false,pointers,pointerTemplate,pointerHeight,spec.mb);
            w.wireObject=Wire(wires,spec.wire,w.pointA,w.pointB,Plug(ba),Plug(bb),spec.black,templates);
            Dirty(w);
        }
        Verify();AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        Debug.Log("EXP4 STEP1 SAVED: "+newBananas+" bananas completed, "+plugs+" missing plugs added; five wire/pointer assignments completed. Only NEW EXP4 Step1 connections and their new visuals changed.");
    }
    [MenuItem("PV System/Audit NEW EXP4 Storyboard And UI")]
    public static void AuditStoryboard()
    {
        var steps=Steps();var owner=steps.GetComponentInParent<StoryboardSteps>();
        for(int i=0;i<owner.steps.Count;i++)Debug.Log("EXP4 STORYBOARD "+i+" interaction="+(owner.steps[i].interaction?owner.steps[i].interaction.name:"null")+" UI="+(owner.steps[i].uiObject?owner.steps[i].uiObject.name:"null"));
        foreach(Transform child in steps.Find("Step 1"))Debug.Log("EXP4 ORDER "+child.name+" children="+string.Join(",",child.Cast<Transform>().Select(t=>t.name)));
        foreach(var t in owner.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t.name.Contains("EXP4")||t.name=="Exp 4 UI"))Debug.Log("EXP4 UI "+t.name+" parent="+(t.parent?t.parent.name:"null"));
    }
    [MenuItem("PV System/Verify NEW EXP4 Step1 Bananas")]
    public static void Verify()
    {
        var steps=Steps();var step=steps.Find("Step 1");int count=0;
        foreach(Transform group in step)
        {
            var bananas=group.Cast<Transform>().Where(t=>t.GetComponent<MeshFilter>()!=null).ToArray();if(bananas.Length!=2)continue;
            var w=group.GetComponent<WireInteraction>();
            if(w==null||w.pointA==null||w.pointB==null||w.wireObject==null||w.plugA==null||w.plugB==null||w.markerA==null||w.markerB==null)throw new InvalidOperationException("Incomplete connection: "+group.name);
            if(w.pointA.transform.parent!=group||w.pointB.transform.parent!=group||w.plugA.transform.parent!=w.pointA.transform||w.plugB.transform.parent!=w.pointB.transform)throw new InvalidOperationException("Cross-assigned banana or plug: "+group.name);
            foreach(var banana in bananas)if(!banana.GetComponent<BoxCollider>()||!banana.GetComponent<Interaction>()||!banana.GetComponent<OnMouseDownEvent>())throw new InvalidOperationException("Missing banana component.");
            if(w.wireObject.name.Contains("Off-grid")||w.wireObject.name=="Full bringe rectifier output Red to AC Halogen Red input"||w.wireObject.name=="Full bringe rectifier output Black to AC Halogen Black input")
            {
                var c=w.wireObject.GetComponentInChildren<SplineContainer>(true);var s=c.Spline;
                if(Vector3.Distance(c.transform.TransformPoint((Vector3)s[0].Position),w.plugA.transform.position)>.01f||Vector3.Distance(c.transform.TransformPoint((Vector3)s[s.Count-1].Position),w.plugB.transform.position)>.01f)throw new InvalidOperationException("Spline endpoint mismatch: "+w.wireObject.name);
                var mesh=c.GetComponent<SplineExtrude>().targetMesh;if(mesh==null||mesh.vertexCount==0)throw new InvalidOperationException("Empty wire mesh.");
            }
            count++;
        }
        Debug.Log("EXP4 STEP1 VERIFIED: "+count+" connections; all banana components, point/plug/pointer/wire assignments valid; new spline endpoints meet their plugs.");
    }
}

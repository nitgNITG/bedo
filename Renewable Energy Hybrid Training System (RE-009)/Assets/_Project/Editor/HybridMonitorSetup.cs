using System;
using System.Linq;
using TMPro;
using TutorialFramework;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using Object=UnityEngine.Object;

public static class HybridMonitorSetup
{
    [MenuItem("RE009/Test And Preview Live Hybrid Monitor")]
    public static void TestLiveMonitor()
    {
        if(!Application.isPlaying)throw new Exception("Complete the guided run first.");var sys=All<HybridTrainingController>().Single(s=>s.isActiveAndEnabled);var bind=sys.storyboard.GetComponent<HybridGuidedBindings>();
        void Check(bool okay,string message){if(!okay)throw new Exception(message);}
        bind.tabs.ShowSystem();sys.Refresh();var reading=sys.Reading;
        void Meter(HybridTrainingController.Meter meter,float v,float a,float p,string name){
            Check(meter.voltage&&meter.current&&meter.power,name+" fields missing");
            Check(meter.voltage.gameObject.activeInHierarchy&&meter.voltage.text==v.ToString("F2")&&meter.current.text==a.ToString("F2")&&meter.power.text==p.ToString("F2"),name+" outputs do not match model");
        }
        Meter(sys.solar,reading.pvVoltage,reading.pvCurrent,reading.pvPower,"Solar");Meter(sys.windDC,reading.wind.dcVoltage,reading.wind.dcCurrent,reading.wind.dcPower,"Wind");
        Meter(sys.acLoad,reading.acVoltage,reading.acCurrent,reading.acPower,"AC loads");Meter(sys.battery,reading.busVoltage,reading.batteryCurrent,reading.batteryPower,"Battery");Meter(sys.inverter,reading.busVoltage,reading.inverterCurrent,reading.inverterPower,"Inverter");Meter(sys.utilityGrid,0,0,0,"Utility grid");
        Check(sys.irradianceText.text==(sys.irradiance*100).ToString("0"),"Intensity field mismatch");
        sys.graph.ClearRuntimeGraph();foreach(float rpm in new[]{200f,400f,600f,800f,1000f}){sys.windSpeed.SetSliderSpeed(rpm);sys.Refresh();}
        var graph=new SerializedObject(sys.graph);var x=(TMP_Text)graph.FindProperty("plottedXValuesText").objectReferenceValue;var y=(TMP_Text)graph.FindProperty("plottedYValuesText").objectReferenceValue;var current=(TMP_Text)graph.FindProperty("plottedCurrentValuesText").objectReferenceValue;
        int rows=x.text.Split('\n').Length;Check(rows>=5&&y.text.Split('\n').Length==rows&&current.text.Split('\n').Length==rows,"Graph columns have unmatched rows");
        string before=x.text;for(int i=0;i<20;i++)sys.Refresh();Check(x.text==before,"Unchanged settings generated duplicate graph rows");
        Check(y.text.Split('\n').Last()==sys.Reading.busVoltage.ToString("0.##")&&current.text.Split('\n').Last()==sys.Reading.controllerCurrent.ToString("0.##"),"Graph is not using combined controller output");
        foreach(float rpm in new[]{800f,600f,400f,200f,1000f,400f,1000f}){sys.windSpeed.SetSliderSpeed(rpm);sys.Refresh();}
        Check(x.text.Split('\n').Length==5&&x.text.Split('\n').Select(float.Parse).SequenceEqual(new[]{200f,400f,600f,800f,1000f}),"Revisiting speed duplicated or reordered rows");
        var exporter=(PVExperimentExporter)graph.FindProperty("exporter").objectReferenceValue;
        Check(exporter.BuildWindCSV().Trim().Split('\n').Length==6,"Export has duplicated samples");
        string csv=exporter.BuildWindCSV();var csvRows=csv.Trim().Split('\n');
        Check(csvRows[0]=="Speed (RPM),DC voltage (V),DC current (A)","Export must include all three columns");
        var displayRows=new[]{x,y,current}.Select(t=>t.text.Split('\n')).ToArray();
        for(int i=1;i<csvRows.Length;i++)Check(csvRows[i]==string.Join(",",displayRows.Select(r=>r[i-1])),"Export row differs from the displayed table: "+i);
        sys.graph.ShowACSpeedGraph();Check(exporter.BuildWindCSV()==csv,"Current graph selection changed the exported table");
        sys.graph.ShowVoltageGraph();Check(exporter.BuildWindCSV()==csv,"Voltage graph selection changed the exported table");
        System.IO.File.WriteAllText("Logs/HybridReview/three-column-export-tests.txt","PASS: All five RPM/voltage/current rows match the three displayed columns after revisiting speeds; both graph tabs export identical data.");

        sys.SendMessage("LateUpdate");
        Check(sys.powerAnalyzerCanvas.activeInHierarchy,"Wind Run analyzer is hidden");
        Check(sys.powerAnalyzerText.text==PVGuidedDataBinder.FormatPowerAnalyzer(sys.Reading.wind.acPower,Mathf.Sqrt(3f)*Mathf.Abs(sys.Reading.wind.acVoltage*sys.Reading.wind.acCurrent)),"Analyzer is not wind-only W/VAr/VA");
        foreach(string field in new[]{"voltageGraphTab","acSpeedGraphTab"}){
            var tab=(UnityEngine.UI.Button)graph.FindProperty(field).objectReferenceValue;
            Check(tab.targetGraphic.color.a==1&&tab.transition==UnityEngine.UI.Selectable.Transition.ColorTint&&tab.colors.highlightedColor.a>0&&tab.colors.disabledColor.a>tab.colors.highlightedColor.a,"Graph hover/selection tint is invisible");
        }
        bind.tabs.ShowGraph(bind.graphButton);Canvas.ForceUpdateCanvases();var scrolls=new[]{x,y,current}.Select(t=>t.GetComponentInParent<UnityEngine.UI.ScrollRect>(true)).ToArray();
        scrolls[0].content.anchoredPosition=new Vector2(0,20);scrolls[0].onValueChanged.Invoke(Vector2.zero);Check(scrolls.All(s=>Mathf.Abs(s.content.anchoredPosition.y-20)<.01f),"Graph scroll columns are not synchronized");
        foreach(var scroll in scrolls)scroll.content.anchoredPosition=Vector2.zero;
        sys.graph.ShowACSpeedGraph();var chart=bind.graphPage.GetComponentInChildren<XCharts.Runtime.LineChart>(true);Check(chart.GetChartComponent<XCharts.Runtime.YAxis>().axisName.name=="DC current (A)","Current graph axis is wrong");sys.graph.ShowVoltageGraph();bind.tabs.ShowSystem();
        All<SmoothCameraMovement>().Single().MoveToPosition(CameraPositionReorderUtility.HybridMonitorIndex());foreach(var row in sys.storyboard.steps)if(row.uiObject)row.uiObject.SetActive(false);
        System.IO.File.WriteAllText("Logs/HybridReview/new-monitor-tests.txt","PASS: 18 meter values and intensity match the mathematical model; combined-controller voltage/current graph rows match, stay synchronized, do not duplicate unchanged settings, and both existing graph tabs work.");
    }
    [MenuItem("RE009/Bind New Hybrid Monitor And Original Graph Tables")]
    public static void BindNewMonitor()
    {
        if(Application.isPlaying)throw new Exception("Stop Play first.");
        var system=All<HybridTrainingController>().Single();var binding=system.storyboard.GetComponent<HybridGuidedBindings>();
        var surface=system.monitor.GetComponentsInChildren<MeshRenderer>(true).Single(r=>r.sharedMaterial&&r.sharedMaterial.mainTexture&&AssetDatabase.GetAssetPath(r.sharedMaterial.mainTexture).EndsWith("hybrid system.jpg"));
        var mesh=surface.GetComponent<MeshFilter>().sharedMesh;var verts=mesh.vertices.Select(v=>surface.transform.TransformPoint(v)).ToArray();
        float left=verts.Min(v=>v.x),right=verts.Max(v=>v.x),bottom=verts.Min(v=>v.y),top=verts.Max(v=>v.y),z=verts.Average(v=>v.z)-.06f;
        Vector3 Pixel(float x,float y)=>new Vector3(Mathf.Lerp(left,right,x/4800f),Mathf.Lerp(top,bottom,y/2950f),z);
        var numbers=system.monitor.transform.Find("Hybrid Experiment Numbers");numbers.gameObject.SetActive(true);var style=numbers.GetComponentsInChildren<TMP_Text>(true).First();
        var template=Object.Instantiate(style.gameObject);template.name="Temporary original meter text style";template.SetActive(false);
        foreach(Transform child in numbers.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
        TMP_Text Field(string name,float x,float y,float w=140,float h=72){
            var copy=Object.Instantiate(template,numbers);copy.name=name;copy.SetActive(true);var text=copy.GetComponent<TMP_Text>();
            copy.transform.SetPositionAndRotation(Pixel(x+w/2,y+h/2),Quaternion.identity);copy.transform.localScale=Vector3.one;
            text.enabled=true;text.rectTransform.sizeDelta=new Vector2((right-left)*w/4800f,(top-bottom)*h/2950f);text.fontSize=.8f;text.enableAutoSizing=false;text.alignment=TextAlignmentOptions.Center;text.margin=Vector4.zero;text.text="0.00";text.raycastTarget=false;return text;
        }
        HybridTrainingController.Meter Meter(string name,float x,float y)=>new HybridTrainingController.Meter{
            voltage=Field(name+" Voltage",x+8,y+9),current=Field(name+" Current",x+8,y+134),power=Field(name+" Power",x+8,y+254)};
        system.solar=Meter("Solar PV",1730,915);system.windDC=Meter("Wind DC",3480,915);
        system.acLoad=Meter("AC Loads",970,1735);system.utilityGrid=Meter("Utility Grid",1715,1735);
        system.inverter=Meter("Inverter Input",2935,1965);system.battery=Meter("Battery Bank",3750,1965);
        system.irradianceText=Field("Light Intensity",170,2615,340,120);system.irradianceText.text="100";
        system.windAC=new HybridTrainingController.Meter();system.dump=new HybridTrainingController.Meter();system.controllerOutput=new HybridTrainingController.Meter();system.rpmText=null;
        Object.DestroyImmediate(template);
        var oldTable=system.monitor.transform.Find("Hybrid Readings Table");if(oldTable)oldTable.gameObject.SetActive(false);
        foreach(var name in new[]{"MAIN ARROW (1)","MAIN ARROW (2)","Button scale (2)","Down (2)","Up (2)"}){var t=system.monitor.transform.Find(name);if(t)t.gameObject.SetActive(false);}
        var graph=system.graph;var so=new SerializedObject(graph);var graphUI=binding.graphPage.transform.Find("EXP1 Wind Graph UI") as RectTransform;
        void Rect(RectTransform rect,float x,float y,float w,float h){rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2((x+w*.5f)/4800f*graphUI.rect.width-graphUI.rect.width*.5f,graphUI.rect.height*.5f-(y+h*.5f)/2950f*graphUI.rect.height);rect.sizeDelta=new Vector2(w/4800f*graphUI.rect.width,h/2950f*graphUI.rect.height);}
        var columns=new[]{"RPM Readings","DC Voltage Readings","DC Current Readings"};var xs=new[]{170f,735f,1260f};var widths=new[]{565f,525f,525f};var scrolls=new UnityEngine.UI.ScrollRect[3];
        for(int i=0;i<3;i++){
            var t=graphUI.Find(columns[i]);t.gameObject.SetActive(true);Rect((RectTransform)t,xs[i]+12,875,widths[i]-24,1620);
            var scroll=t.GetComponent<UnityEngine.UI.ScrollRect>();scrolls[i]=scroll;scroll.horizontal=false;scroll.vertical=true;scroll.content.anchoredPosition=Vector2.zero;
            var value=t.GetComponentInChildren<TMP_Text>(true);value.text="";value.fontSize=14;value.color=Color.black;
            var image=t.GetComponent<UnityEngine.UI.Image>();if(image)image.raycastTarget=true;
            if(scroll.verticalScrollbar){var r=(RectTransform)scroll.verticalScrollbar.transform;r.anchorMin=new Vector2(1,0);r.anchorMax=new Vector2(1,1);r.pivot=new Vector2(0,1);r.anchoredPosition=Vector2.zero;r.sizeDelta=new Vector2(8,0);scroll.verticalScrollbarVisibility=UnityEngine.UI.ScrollRect.ScrollbarVisibility.AutoHide;}
        }
        var sync=graphUI.GetComponent<PVGraphReadoutScrollSync>();if(!sync)sync=graphUI.gameObject.AddComponent<PVGraphReadoutScrollSync>();sync.Configure(scrolls[0],scrolls[1],scrolls[2]);
        var chart=(RectTransform)so.FindProperty("lineChartSpawnParent").objectReferenceValue;Rect(chart,1960,875,2080,1620);
        var voltage=(UnityEngine.UI.Button)so.FindProperty("voltageGraphTab").objectReferenceValue;var current=(UnityEngine.UI.Button)so.FindProperty("acSpeedGraphTab").objectReferenceValue;
        Rect((RectTransform)voltage.transform,4190,1145,545,105);Rect((RectTransform)current.transform,4190,1320,545,105);
        foreach(var pair in new[]{(voltage,"Voltage–Speed"),(current,"Current–Speed")}){
            pair.Item1.gameObject.SetActive(true);var colors=pair.Item1.colors;colors.normalColor=new Color(0,0,0,0);colors.highlightedColor=new Color(0,0,0,.12f);colors.selectedColor=new Color(0,0,0,.12f);colors.pressedColor=new Color(0,0,0,.2f);colors.disabledColor=new Color(0,0,0,.12f);pair.Item1.colors=colors;
            if(pair.Item1.targetGraphic)pair.Item1.targetGraphic.color=colors.normalColor;
            foreach(var label in pair.Item1.GetComponentsInChildren<TMP_Text>(true)){label.text=pair.Item2;label.fontSize=14;}
        }
        system.recordGraphMeasurements=true;Ref(system.windSpeed,"graphController",null);foreach(var r in binding.graphReadouts)if(r)r.SetActive(true);
        EditorUtility.SetDirty(system);EditorUtility.SetDirty(graph);EditorUtility.SetDirty(binding);EditorUtility.SetDirty(sync);
        EditorSceneManager.MarkSceneDirty(system.gameObject.scene);EditorSceneManager.SaveScene(system.gameObject.scene);
    }
    [MenuItem("RE009/Audit New Hybrid Monitor Layout")]
    public static void AuditCurrentMonitor()
    {
        var system=All<HybridTrainingController>().Single();var bind=system.storyboard.GetComponent<HybridGuidedBindings>();var log=new System.Collections.Generic.List<string>();
        foreach(var root in new[]{system.monitor,bind.graphPage})foreach(var t in root.GetComponentsInChildren<Transform>(true)){
            var r=t.GetComponent<Renderer>();var text=t.GetComponent<TMP_Text>();
            if(r&&!text){log.Add("SURFACE "+HybridExperimentSetup.PathOf(t)+" bounds="+r.bounds+" tex="+(r.sharedMaterial&&r.sharedMaterial.mainTexture?AssetDatabase.GetAssetPath(r.sharedMaterial.mainTexture):"none"));if(r.sharedMaterial&&r.sharedMaterial.name.Contains("Hybrid Control")){var mesh=t.GetComponent<MeshFilter>().sharedMesh;log.Add("MESH "+mesh.name+" bounds="+mesh.bounds+" sub="+mesh.GetSubMesh(0).bounds);for(int i=0;i<Mathf.Min(8,mesh.vertexCount);i++)log.Add("VERT "+t.TransformPoint(mesh.vertices[i])+" UV "+mesh.uv[i]);}}
            if(text)log.Add("TEXT "+HybridExperimentSetup.PathOf(t)+" pos="+t.position+" font="+text.fontSize+" color="+text.color+" rect="+text.rectTransform.rect+" scale="+t.lossyScale);
            if(t.GetComponent<Canvas>())log.Add("CANVAS "+HybridExperimentSetup.PathOf(t)+" pos="+t.position+" rect="+t.GetComponent<RectTransform>().rect+" scale="+t.lossyScale);
        }
        var serial=new SerializedObject(system.graph);foreach(string field in new[]{"lineChartSpawnParent","plottedXValuesText","plottedYValuesText","plottedCurrentValuesText","voltageGraphTab","acSpeedGraphTab"}){var c=serial.FindProperty(field).objectReferenceValue as Component;log.Add("GRAPHREF "+field+" "+(c?HybridExperimentSetup.PathOf(c.transform):"none"));}
        System.IO.File.WriteAllLines("Logs/HybridReview/new-monitor-layout.txt",log);
    }
    static T[] All<T>()where T:Component=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(c=>c.gameObject.scene==UnityEngine.SceneManagement.SceneManager.GetActiveScene()).ToArray();
    static void Ref(Object target,string key,Object value){var so=new SerializedObject(target);so.FindProperty(key).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(target);}
    [MenuItem("RE009/Prepare Temporary Solar Monitor")]
    public static void Prepare()
    {
        if(Application.isPlaying)throw new Exception("Stop Play Mode first.");
        var owner=All<StoryboardSteps>().Single(b=>b.StoryboardID==HybridExperimentSetup.Id);
        if(owner.gameObject.activeSelf)throw new Exception("Keep the staging experiment inactive during setup.");
        var previous=owner.GetComponentInChildren<HybridTrainingController>(true);if(previous)Object.DestroyImmediate(previous.gameObject);
        var windUI=All<Transform>().Single(t=>HybridExperimentSetup.PathOf(t)=="Equipment/Wind Lab/UI/Wind UI");
        var source=All<Transform>().Single(t=>HybridExperimentSetup.PathOf(t)=="Equipment/Wind Lab/UI/Solar UI/Pv characteristic");
        var oldPage=windUI.Find("Monitors/Hybrid Control (Temporary Solar Monitor)");if(oldPage)Object.DestroyImmediate(oldPage.gameObject);
        var page=Object.Instantiate(source.gameObject,windUI.Find("Monitors"),true);page.name="Hybrid Control (Temporary Solar Monitor)";page.SetActive(false);
        var numbersSource=All<Transform>().Single(t=>t.name=="Exp 4 Numbers"&&t.IsChildOf(windUI));
        var numbers=Object.Instantiate(numbersSource.gameObject,page.transform,true);numbers.name="Hybrid Experiment Numbers";
        var systemObject=new GameObject("Hybrid Energy Runtime");systemObject.transform.SetParent(owner.transform,false);
        var system=systemObject.AddComponent<HybridTrainingController>();system.storyboard=owner;system.monitor=page;system.blackScreen=windUI.Find("Black Screen").gameObject;
        system.lamps=All<FreeLabWindController>().Single().lamps.Where(l=>l.ac).Select(l=>new HybridTrainingController.Lamp{led=l.led,light=l.light,connection=new HybridTrainingController.Circuit()}).ToArray();
        var wires=owner.steps.Select(s=>s.interaction).OfType<WireInteraction>().ToArray();
        HybridTrainingController.Circuit At(int n)=>new HybridTrainingController.Circuit{guided=wires.Where(w=>w.transform.parent.name=="Step "+n).ToArray()};
        system.mainWiring=At(1);system.batteryWiring=At(5);system.inverterWiring=At(7);system.windWiring=At(8);system.solarWiring=At(10);
        foreach(var lamp in system.lamps){string type=lamp.led?"AC LED":"AC Halogen";var mapped=All<FreeLabWindController>().Single().lamps.Where(l=>l.ac&&l.led==lamp.led).ToArray();int n=Array.FindIndex(mapped,l=>l.light==lamp.light)+1;lamp.connection.guided=wires.Where(w=>w.transform.parent.name=="Step 9"&&w.name.Contains(type)&& (n==2||w.name.Contains(type+" L 1")||w.name.Contains(type+" N 1"))).ToArray();}
        var texts=numbers.GetComponentsInChildren<TMP_Text>(true);
        HybridTrainingController.Meter Meter(string name){var t=texts.FirstOrDefault(t=>t.transform.parent.name==name);if(!t)throw new Exception("Meter template missing: "+name);var parent=t.transform.parent;return new HybridTrainingController.Meter{voltage=parent.Find("V DC").GetComponent<TMP_Text>(),current=parent.Find("A DC").GetComponent<TMP_Text>(),power=parent.Find("P DC").GetComponent<TMP_Text>()};}
        // Preserve the existing world-space number fields and capture path.
        system.windAC=Meter("3 Phase Power Meter");
        var template=system.windAC.voltage.transform.parent;
        foreach(var child in numbers.GetComponentsInChildren<Transform>(true).Where(t=>t.parent==template.parent&&t!=template).ToArray())child.gameObject.SetActive(false);
        string[] labels={"Wind DC","Solar PV","Battery Bank","AC Loads","Dump Load","Inverter Input"};
        var meters=new HybridTrainingController.Meter[6];
        for(int i=0;i<6;i++){
         var copy=Object.Instantiate(template.gameObject,template.parent,false);copy.name=labels[i];copy.SetActive(true);
         copy.transform.localPosition+=new Vector3((i%3-1)*2f,-(i/3+1)*1.5f,0);
         meters[i]=new HybridTrainingController.Meter{voltage=copy.transform.Find("V DC").GetComponent<TMP_Text>(),current=copy.transform.Find("A DC").GetComponent<TMP_Text>(),power=copy.transform.Find("P DC").GetComponent<TMP_Text>()};
        }
        system.windDC=meters[0];system.solar=meters[1];system.battery=meters[2];system.acLoad=meters[3];system.dump=meters[4];system.inverter=meters[5];
        foreach(var text in texts)text.text="0.00";
        var button=page.transform.Find("Button (2)").gameObject;var collider=button.GetComponent<BoxCollider>();if(!collider)collider=button.AddComponent<BoxCollider>();
        if(button.GetComponent<MeshFilter>()) {var b=button.GetComponent<MeshFilter>().sharedMesh.bounds;collider.center=b.center;collider.size=b.size+Vector3.forward*.1f;}
        var enable=button.AddComponent<HybridEnableSwitch>();enable.system=system;enable.storyboard=owner;
        var interaction=button.GetComponent<Interaction>();if(!interaction)interaction=button.AddComponent<Interaction>();interaction.Allow=false;interaction.onClicked=new UnityEvent();enable.sound=interaction;
        var save=All<Bedo.Core.UI.MapCameraScreenshotButton_RT>().First(c=>c.transform.IsChildOf(windUI));
        var newSave=page.transform.Find("save screen (1)").gameObject;
        var capture=newSave.AddComponent<Bedo.Core.UI.MapCameraScreenshotButton_RT>();EditorUtility.CopySerialized(save,capture);
        Ref(capture,"captureSurface",page.transform.Find("PV CHARACTERISTIC").GetComponent<Renderer>());
        var saveCollider=newSave.GetComponent<BoxCollider>();if(!saveCollider)saveCollider=newSave.AddComponent<BoxCollider>();
        var saveBounds=newSave.GetComponent<MeshFilter>().sharedMesh.bounds;saveCollider.center=saveBounds.center;saveCollider.size=saveBounds.size+Vector3.forward*.1f;
        var click=newSave.AddComponent<Template.OnMouseDownE>();click.mouseDown=new UnityEvent();UnityEventTools.AddVoidPersistentListener(click.mouseDown,capture.SaveScreenshot);
        // Keep this staged page hidden until the complete storyboard can select it.
        EditorUtility.SetDirty(system);EditorSceneManager.MarkSceneDirty(owner.gameObject.scene);EditorSceneManager.SaveScene(owner.gameObject.scene);
        Debug.Log("RE009 temporary solar monitor prepared; original Button (2) retained as Enable switch. Numeric fields and screenshot handler copied from the working monitor.");
    }
}

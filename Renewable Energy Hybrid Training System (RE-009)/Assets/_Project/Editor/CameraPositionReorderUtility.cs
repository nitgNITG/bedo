using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Keeps scene camera positions and every saved SmoothCameraMovement.MoveToPosition
/// event in sync when the main camera's position list is reordered.
/// </summary>
public static class CameraPositionReorderUtility
{
    [MenuItem("Bedo/Cameras/Remove Extra Hybrid View And Reuse Camera 8")]
    private static void RemoveExtraHybridView()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play first.");
        var movement=FindSingleCameraMovement();var s=new SerializedObject(movement);var positions=s.FindProperty("cameraPositions");
        if(positions.arraySize!=18||!positions.GetArrayElementAtIndex(17).objectReferenceValue.name.Contains("Hybrid Controller"))throw new InvalidOperationException("Expected the extra Hybrid Controller view at displayed camera 18.");
        var obsolete=positions.GetArrayElementAtIndex(17).objectReferenceValue;var replacement=positions.GetArrayElementAtIndex(7).objectReferenceValue;
        var content=FindCameraGalleryContent();var extra=content.Cast<Transform>().Single(t=>TryGetMoveToPositionIndex(t,movement,out int index)&&index==17);
        var map=Enumerable.Range(0,18).ToDictionary(i=>i,i=>i==17?7:i);int calls=RemapAllMoveToPositionCalls(movement,map);
        foreach(var c in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
            if(!c||c==movement)continue;var serial=new SerializedObject(c);var p=serial.GetIterator();bool dirty=false;
            while(p.NextVisible(true))if(p.propertyType==SerializedPropertyType.ObjectReference&&p.objectReferenceValue==obsolete){p.objectReferenceValue=replacement;dirty=true;}
            if(c is CameraKeyboardNavigation){serial.FindProperty("cameraCount").intValue=17;if(serial.FindProperty("currentIndex").intValue==17)serial.FindProperty("currentIndex").intValue=7;dirty=true;}
            if(dirty){serial.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(c);}
        }
        positions.arraySize=17;s.FindProperty("cameraButtons").arraySize=17;s.ApplyModifiedPropertiesWithoutUndo();
        UnityEngine.Object.DestroyImmediate(extra.gameObject);UnityEngine.Object.DestroyImmediate(((Transform)obsolete).gameObject);
        EditorUtility.SetDirty(movement);EditorSceneManager.MarkSceneDirty(movement.gameObject.scene);EditorSceneManager.SaveScene(movement.gameObject.scene);
        System.IO.File.WriteAllText("Logs/HybridReview/camera-18-removal.txt","Removed displayed camera 18 (Hybrid Controller); redirected "+calls+" saved camera calls to displayed camera 8 (index 7). Existing camera 8 pose and image preserved.");
        VerifyHybridCameraGallery();
    }
    [MenuItem("Bedo/Cameras/Insert Hybrid Solar Views Before Camera 11")]
    private static void InsertHybridSolarViews()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
        var movement=FindSingleCameraMovement();var serial=new SerializedObject(movement);
        var positions=serial.FindProperty("cameraPositions");
        var old=Enumerable.Range(0,positions.arraySize).Select(i=>(Transform)positions.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
        if(old.Length!=17||old.Any(p=>!p)||old.Distinct().Count()!=old.Length)throw new InvalidOperationException("Expected 17 unique camera views.");
        var solar=Enumerable.Range(1,3).Select(n=>old.Single(t=>t.name.Contains("Hybrid Solar Board "+n))).ToArray();
        var ordered=old.Except(solar).ToList();ordered.InsertRange(11,solar);
        var map=Enumerable.Range(0,old.Length).ToDictionary(i=>i,i=>ordered.IndexOf(old[i]));
        // Snapshot every existing persistent call before remapping so verification
        // checks the original view identity, not just an in-range integer.
        var expected=new List<(Component component,string argument,Transform pose)>();
        foreach(var c in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
            if(!c)continue;var s=new SerializedObject(c);var p=s.GetIterator();
            while(p.NextVisible(true))if(p.name=="m_MethodName"&&p.stringValue=="MoveToPosition"){
                var prefix=p.propertyPath.Substring(0,p.propertyPath.Length-".m_MethodName".Length);
                if(s.FindProperty(prefix+".m_Target")?.objectReferenceValue!=movement)continue;
                var argument=s.FindProperty(prefix+".m_Arguments.m_IntArgument");
                if(argument==null||argument.intValue<0||argument.intValue>=old.Length)throw new InvalidOperationException("Invalid camera reference on "+c.name);
                expected.Add((c,argument.propertyPath,old[argument.intValue]));
            }
        }
        // The active scroll content is the authoritative gallery; legacy hidden
        // galleries are left intact and their camera events are remapped as well.
        var content=FindCameraGalleryContent();var cards=new Dictionary<Transform,Transform>();
        foreach(Transform card in content)if(TryGetMoveToPositionIndex(card,movement,out int index)&&index>=0&&index<old.Length){if(cards.ContainsKey(old[index]))throw new InvalidOperationException("Duplicate live camera card.");cards.Add(old[index],card);}
        if(cards.Count==0)throw new InvalidOperationException("No camera card template found.");
        var remapped=RemapAllMoveToPositionCalls(movement,map);
        foreach(var check in expected){var s=new SerializedObject(check.component);int index=s.FindProperty(check.argument).intValue;if(ordered[index]!=check.pose)throw new InvalidOperationException("Camera identity changed on "+check.component.name);}
        for(int i=0;i<ordered.Count;i++)positions.GetArrayElementAtIndex(i).objectReferenceValue=ordered[i];
        serial.ApplyModifiedPropertiesWithoutUndo();
        foreach(var slider in UnityEngine.Object.FindObjectsByType<SliderRangeChecker>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
            var s=new SerializedObject(slider);var target=s.FindProperty("turbineViewCamera");var index=s.FindProperty("turbineViewIndex");
            if(target.objectReferenceValue==movement&&index.intValue>=0){index.intValue=map[index.intValue];s.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(slider);}
        }
        foreach(var keyboard in UnityEngine.Object.FindObjectsByType<CameraKeyboardNavigation>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
            var s=new SerializedObject(keyboard);if(s.FindProperty("cameraMovement").objectReferenceValue!=movement)continue;
            var index=s.FindProperty("currentIndex");index.intValue=map[index.intValue];s.FindProperty("cameraCount").intValue=ordered.Count;s.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(keyboard);
        }
        var template=cards.Values.First();var gallery=new List<Transform>();
        for(int i=0;i<ordered.Count;i++){
            var pose=ordered[i];string description=pose.name.Contains("Hybrid Solar Board")?" - Hybrid Solar Board "+(Array.IndexOf(solar,pose)+1):pose.name.Contains("Wind Turbine View")?" - Wind Turbine View":pose.name.Contains("Hybrid Controller")?" - Hybrid Controller":"";
            pose.name="Camera Pos "+i+description;EditorUtility.SetDirty(pose.gameObject);
            if(!cards.TryGetValue(pose,out var card)){
                card=UnityEngine.Object.Instantiate(template,content);var image=card.Find("Musk/Image");if(image){var ui=image.GetComponent<UnityEngine.UI.Image>();if(ui){ui.sprite=null;ui.overrideSprite=null;}}
            }
            card.name="Camera "+i;card.SetSiblingIndex(i);card.gameObject.SetActive(true);SetMoveToPositionCalls(card,movement,i);
            foreach(var c in card.GetComponentsInChildren<MonoBehaviour>(true)){
                var s=new SerializedObject(c);var text=s.FindProperty("buttonText");if(text!=null&&text.propertyType==SerializedPropertyType.String){text.stringValue=(i+1).ToString();s.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(c);}
            }
            foreach(var text in card.GetComponentsInChildren<TMPro.TMP_Text>(true)){text.text=(i+1).ToString();EditorUtility.SetDirty(text);}
            gallery.Add(card);
        }
        // Names on camera-request helper objects must match their new integer.
        foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
            var match=System.Text.RegularExpressions.Regex.Match(t.name,@"^(move to camera\s+)(\d+)$",System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if(match.Success&&int.TryParse(match.Groups[2].Value,out int index)&&map.TryGetValue(index,out int next)){t.name=match.Groups[1].Value+next;EditorUtility.SetDirty(t.gameObject);}
        }
        SetCameraButtonList(movement,gallery);EditorUtility.SetDirty(movement);
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)content);
        EditorSceneManager.MarkSceneDirty(movement.gameObject.scene);EditorSceneManager.SaveScene(movement.gameObject.scene);
        System.IO.File.WriteAllLines("Logs/HybridReview/camera-remap.txt",new[]{"Verified original target identity for "+expected.Count+" persistent camera references; remapped "+remapped+" calls. No screenshots captured."}.Concat(Enumerable.Range(0,old.Length).Select(i=>"OLD "+i+" -> NEW "+map[i]+" : "+old[i].name)));
        VerifyHybridCameraGallery();
    }

    [MenuItem("Bedo/Cameras/Verify Hybrid Camera Gallery")]
    public static void VerifyHybridCameraGallery()
    {
        var movement=FindSingleCameraMovement();var s=new SerializedObject(movement);var poses=s.FindProperty("cameraPositions");var buttons=s.FindProperty("cameraButtons");var content=FindCameraGalleryContent();
        if(poses.arraySize!=17||buttons.arraySize!=17)throw new InvalidOperationException("Camera views and buttons must both have 17 entries.");
        int count=0;foreach(Transform card in content){if(!card.gameObject.activeSelf||!TryGetMoveToPositionIndex(card,movement,out int index))continue;
            if(index!=count||card.name!="Camera "+count)throw new InvalidOperationException("Gallery order mismatch: "+card.name);
            if(card.GetComponentsInChildren<TMPro.TMP_Text>(true).Any(t=>t.text!=(count+1).ToString()))throw new InvalidOperationException("Incorrect camera badge: "+card.name);
            if(buttons.GetArrayElementAtIndex(count).objectReferenceValue!=card.GetComponentInChildren<ButtonSelection>(true))throw new InvalidOperationException("Selection link mismatch: "+card.name);
            count++;
        }
        if(count!=17)throw new InvalidOperationException("Gallery does not expose all 17 views.");
        for(int i=0;i<3;i++)if(!poses.GetArrayElementAtIndex(11+i).objectReferenceValue.name.Contains("Hybrid Solar Board "+(i+1)))throw new InvalidOperationException("Solar camera order is incorrect.");
        System.IO.File.WriteAllText("Logs/HybridReview/camera-gallery-verification.txt","PASS: 17 ordered gallery entries, badges 1–18, matching selection links, solar views at indices 11–13. Existing screenshots preserved; new thumbnails left blank for the user.");
    }

    [MenuItem("Bedo/Cameras/Test Hybrid Gallery Button Links In Play Mode")]
    private static void TestHybridGalleryButtons()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Use Play Mode after the guided run.");
        VerifyHybridCameraGallery();var movement=FindSingleCameraMovement();var s=new SerializedObject(movement);var poses=s.FindProperty("cameraPositions");
        var targetField=typeof(SmoothCameraMovement).GetField("movementTarget",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
        var number=(TMPro.TMP_Text)s.FindProperty("cameraNumberText").objectReferenceValue;int tested=0;
        foreach(Transform card in FindCameraGalleryContent()){
            if(!card.gameObject.activeSelf||!TryGetMoveToPositionIndex(card,movement,out int index))continue;
            var button=card.GetComponentsInChildren<UnityEngine.UI.Button>(true).FirstOrDefault(b=>Enumerable.Range(0,b.onClick.GetPersistentEventCount()).Any(i=>b.onClick.GetPersistentTarget(i)==movement&&b.onClick.GetPersistentMethodName(i)=="MoveToPosition"));
            if(!button)throw new InvalidOperationException("No gallery camera button on "+card.name);
            button.onClick.Invoke();
            if(targetField.GetValue(movement)!=poses.GetArrayElementAtIndex(index).objectReferenceValue)throw new InvalidOperationException("Wrong requested view for "+card.name);
            if(number.text!=(index+1).ToString("D2"))throw new InvalidOperationException("Wrong HUD camera number for "+card.name);
            var selection=card.GetComponentInChildren<ButtonSelection>(true);var selected=(GameObject)new SerializedObject(selection).FindProperty("selectedObject").objectReferenceValue;
            if(!selected||!selected.activeSelf)throw new InvalidOperationException("Wrong gallery selection for "+card.name);
            tested++;
        }
        movement.MoveToPosition(HybridMonitorIndex());
        if(tested!=17)throw new InvalidOperationException("Not all gallery buttons tested.");
        System.IO.File.WriteAllText("Logs/HybridReview/camera-gallery-runtime.txt","PASS: all 17 menu button callbacks request the matching camera pose, show the matching HUD number, and select their own gallery card. The full guided test checks actual camera movement and endpoint framing.");
    }

    public static int HybridMonitorIndex()
    {
        var s=new SerializedObject(FindSingleCameraMovement());var p=s.FindProperty("cameraPositions");
        return p.GetArrayElementAtIndex(11).objectReferenceValue.name.Contains("Hybrid Solar Board")?15:12;
    }
    [MenuItem("Bedo/Cameras/Align Camera Pose Hierarchy With Menu")]
    private static void AlignPoseHierarchy()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play first.");
        var movement=FindSingleCameraMovement();var p=new SerializedObject(movement).FindProperty("cameraPositions");
        var poses=Enumerable.Range(0,p.arraySize).Select(i=>(Transform)p.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
        if(poses.Any(t=>t.parent!=poses[0].parent))throw new InvalidOperationException("Camera poses use different parents.");
        int start=poses.Min(t=>t.GetSiblingIndex());for(int i=0;i<poses.Length;i++){poses[i].SetSiblingIndex(start+i);EditorUtility.SetDirty(poses[i]);}
        EditorSceneManager.MarkSceneDirty(movement.gameObject.scene);EditorSceneManager.SaveScene(movement.gameObject.scene);
    }
    [MenuItem("Bedo/Cameras/Audit Hybrid Camera Gallery")]
    private static void AuditHybridGallery()
    {
        var movement=FindSingleCameraMovement();var log=new List<string>();
        var so=new SerializedObject(movement);var poses=so.FindProperty("cameraPositions");
        for(int i=0;i<poses.arraySize;i++)log.Add("POSE "+i+" "+poses.GetArrayElementAtIndex(i).objectReferenceValue.name);
        foreach(Transform t in FindCameraGalleryContent()){
            TryGetMoveToPositionIndex(t,movement,out int index);
            log.Add("CARD "+t.name+" index="+index+" active="+t.gameObject.activeSelf+" texts="+string.Join("|",t.GetComponentsInChildren<TMPro.TMP_Text>(true).Select(x=>x.name+"="+x.text)));
            foreach(var image in t.GetComponentsInChildren<UnityEngine.UI.RawImage>(true))log.Add(" RAW "+image.name+"="+(image.texture?AssetDatabase.GetAssetPath(image.texture):"none"));
            foreach(var image in t.GetComponentsInChildren<UnityEngine.UI.Image>(true))if(image.sprite)log.Add(" IMAGE "+image.name+"="+AssetDatabase.GetAssetPath(image.sprite));
        }
        foreach(var c in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
            var serial=new SerializedObject(c);var p=serial.GetIterator();while(p.NextVisible(true))if(p.propertyType==SerializedPropertyType.Integer&&(p.name.ToLower().Contains("camera")||p.name=="turbineViewIndex"||p.name=="currentIndex"))log.Add("INT "+c.GetType().Name+" "+c.name+" "+p.propertyPath+"="+p.intValue);
        }
        System.IO.File.WriteAllLines("Logs/HybridReview/camera-gallery-audit.txt",log);
    }
    // The camera-position identities, in the order requested for the main camera list.
    private static readonly int[] RequestedCameraOrder = { 0, 1, 2, 11, 3, 12, 10, 4, 5, 6, 8, 9, 7 };

    [MenuItem("Bedo/Cameras/Apply Requested Camera Order")]
    private static void ApplyRequestedCameraOrder()
    {
        var movement = FindSingleCameraMovement();
        var oldToNewIndex = BuildOldToNewIndexMap();

        Undo.SetCurrentGroupName("Reorder camera positions and remap camera calls");
        var undoGroup = Undo.GetCurrentGroup();

        ReorderCameraPositions(movement);
        var remappedCalls = RemapAllMoveToPositionCalls(movement, oldToNewIndex);
        var galleryCards = RebuildCameraGallery(movement);
        SetCameraButtonList(movement, galleryCards);

        EditorUtility.SetDirty(movement);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log($"Camera order applied. Remapped {remappedCalls} MoveToPosition calls and configured {galleryCards.Count} gallery choices.", movement);
    }

    [MenuItem("Bedo/Cameras/Keep One Gallery Choice Per Camera")]
    private static void KeepOneGalleryChoicePerCamera()
    {
        var movement = FindSingleCameraMovement();
        var content = FindCameraGalleryContent();
        var selectedCards = new Dictionary<int, Transform>();
        var duplicates = new List<GameObject>();

        foreach (Transform child in content)
        {
            if (!TryGetMoveToPositionIndex(child, movement, out var index))
                continue;

            if (index < 0 || index >= RequestedCameraOrder.Length || selectedCards.ContainsKey(index))
            {
                duplicates.Add(child.gameObject);
                continue;
            }

            selectedCards.Add(index, child);
        }

        if (selectedCards.Count != RequestedCameraOrder.Length)
            throw new InvalidOperationException("The gallery must contain one valid choice for every camera before duplicates can be removed.");

        Undo.SetCurrentGroupName("Remove duplicate camera gallery choices");
        var undoGroup = Undo.GetCurrentGroup();
        foreach (var duplicate in duplicates)
            Undo.DestroyObjectImmediate(duplicate);

        var orderedCards = new List<Transform>(RequestedCameraOrder.Length);
        for (var index = 0; index < RequestedCameraOrder.Length; index++)
        {
            var card = selectedCards[index];
            card.SetSiblingIndex(index);
            orderedCards.Add(card);
        }

        SetCameraButtonList(movement, orderedCards);
        EditorUtility.SetDirty(movement);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log($"Camera gallery now contains {orderedCards.Count} choices, one for each camera.", movement);
    }

    private static SmoothCameraMovement FindSingleCameraMovement()
    {
        var movements = UnityEngine.Object.FindObjectsOfType<SmoothCameraMovement>(true);
        if (movements.Length != 1)
            throw new InvalidOperationException($"Expected exactly one SmoothCameraMovement in the open scene, found {movements.Length}.");

        return movements[0];
    }

    private static Dictionary<int, int> BuildOldToNewIndexMap()
    {
        var result = new Dictionary<int, int>();
        for (var newIndex = 0; newIndex < RequestedCameraOrder.Length; newIndex++)
            result.Add(RequestedCameraOrder[newIndex], newIndex);
        return result;
    }

    private static void ReorderCameraPositions(SmoothCameraMovement movement)
    {
        var serializedMovement = new SerializedObject(movement);
        var positions = serializedMovement.FindProperty("cameraPositions");
        if (positions == null || positions.arraySize != RequestedCameraOrder.Length)
            throw new InvalidOperationException($"SmoothCameraMovement must contain exactly {RequestedCameraOrder.Length} camera positions before reordering.");

        var byIdentity = new Dictionary<int, UnityEngine.Object>();
        for (var index = 0; index < positions.arraySize; index++)
        {
            var transform = positions.GetArrayElementAtIndex(index).objectReferenceValue as Transform;
            if (transform == null || !TryGetCameraIdentity(transform.name, out var identity) || byIdentity.ContainsKey(identity))
                throw new InvalidOperationException("Each camera position must be a unique Transform named 'Camera Pos <number>'.");

            byIdentity.Add(identity, transform);
        }

        for (var newIndex = 0; newIndex < RequestedCameraOrder.Length; newIndex++)
        {
            var identity = RequestedCameraOrder[newIndex];
            if (!byIdentity.TryGetValue(identity, out var cameraPosition))
                throw new InvalidOperationException($"Camera Pos {identity} is missing from SmoothCameraMovement.");

            positions.GetArrayElementAtIndex(newIndex).objectReferenceValue = cameraPosition;
        }

        serializedMovement.ApplyModifiedProperties();
    }

    private static int RemapAllMoveToPositionCalls(SmoothCameraMovement movement, IReadOnlyDictionary<int, int> oldToNewIndex)
    {
        var changed = 0;

        foreach (var component in UnityEngine.Object.FindObjectsOfType<Component>(true))
        {
            if (component == null)
                continue;

            var serializedComponent = new SerializedObject(component);
            var iterator = serializedComponent.GetIterator();
            var dirty = false;
            var enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = true;
                if (iterator.name != "m_MethodName" || iterator.stringValue != "MoveToPosition")
                    continue;

                const string methodSuffix = ".m_MethodName";
                if (!iterator.propertyPath.EndsWith(methodSuffix, StringComparison.Ordinal))
                    continue;

                var callPath = iterator.propertyPath.Substring(0, iterator.propertyPath.Length - methodSuffix.Length);
                var target = serializedComponent.FindProperty(callPath + ".m_Target");
                var argument = serializedComponent.FindProperty(callPath + ".m_Arguments.m_IntArgument");

                if (target?.objectReferenceValue != movement || argument == null || !oldToNewIndex.TryGetValue(argument.intValue, out var newIndex))
                    continue;

                if (argument.intValue != newIndex)
                {
                    argument.intValue = newIndex;
                    dirty = true;
                    changed++;
                }
            }

            if (dirty)
            {
                Undo.RecordObject(component, "Remap camera call");
                serializedComponent.ApplyModifiedProperties();
            }
        }

        return changed;
    }

    private static List<Transform> RebuildCameraGallery(SmoothCameraMovement movement)
    {
        var content = FindCameraGalleryContent();

        var existingCards = new Dictionary<int, Transform>();
        foreach (Transform child in content)
        {
            if (!TryGetMoveToPositionIndex(child, movement, out var index) || existingCards.ContainsKey(index))
                continue;

            existingCards.Add(index, child);
        }

        if (existingCards.Count == 0)
            throw new InvalidOperationException("No existing camera gallery choice with a MoveToPosition call was found.");

        var template = existingCards.Values.First();
        var orderedCards = new List<Transform>(RequestedCameraOrder.Length);

        for (var newIndex = 0; newIndex < RequestedCameraOrder.Length; newIndex++)
        {
            var cameraIdentity = RequestedCameraOrder[newIndex];
            if (!existingCards.TryGetValue(newIndex, out var card))
            {
                card = UnityEngine.Object.Instantiate(template, content);
                Undo.RegisterCreatedObjectUndo(card.gameObject, "Create camera gallery choice");
            }

            card.name = $"Camera {cameraIdentity}";
            card.SetSiblingIndex(newIndex);
            SetMoveToPositionCalls(card, movement, newIndex);
            orderedCards.Add(card);
        }

        return orderedCards;
    }

    private static Transform FindCameraGalleryContent()
    {
        var canvas = FindSceneTransform("Cameras Indicator Ui Canvas");
        var galleryWindow = FindDescendant(canvas, "CamearaGalleryWindow") ?? FindDescendant(canvas, "CameraGalleryWindow");
        if (galleryWindow == null)
            throw new InvalidOperationException("Camera gallery window could not be found under Cameras Indicator Ui Canvas.");

        // The gallery also retains inactive legacy Content objects. Use the
        // content actually assigned to its ScrollRect, not the first name match.
        var scroll = galleryWindow.GetComponentsInChildren<UnityEngine.UI.ScrollRect>(true)
            .FirstOrDefault(s => s.gameObject.activeSelf && s.content != null);
        var content = scroll != null ? scroll.content : FindDescendant(galleryWindow, "Content");
        if (content == null)
            throw new InvalidOperationException("Camera gallery Content could not be found.");

        return content;
    }

    private static void SetCameraButtonList(SmoothCameraMovement movement, IReadOnlyList<Transform> orderedCards)
    {
        var serializedMovement = new SerializedObject(movement);
        var buttons = serializedMovement.FindProperty("cameraButtons");
        buttons.arraySize = orderedCards.Count;

        for (var index = 0; index < orderedCards.Count; index++)
        {
            var selection = orderedCards[index].GetComponentInChildren<ButtonSelection>(true);
            if (selection == null)
                throw new InvalidOperationException($"{orderedCards[index].name} has no ButtonSelection component.");

            buttons.GetArrayElementAtIndex(index).objectReferenceValue = selection;
        }

        serializedMovement.ApplyModifiedProperties();
    }

    private static bool TryGetMoveToPositionIndex(Transform root, SmoothCameraMovement movement, out int index)
    {
        foreach (var component in root.GetComponentsInChildren<Component>(true))
        {
            if (component == null)
                continue;

            var serializedComponent = new SerializedObject(component);
            var iterator = serializedComponent.GetIterator();
            var enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = true;
                if (iterator.name != "m_MethodName" || iterator.stringValue != "MoveToPosition")
                    continue;

                const string methodSuffix = ".m_MethodName";
                if (!iterator.propertyPath.EndsWith(methodSuffix, StringComparison.Ordinal))
                    continue;

                var callPath = iterator.propertyPath.Substring(0, iterator.propertyPath.Length - methodSuffix.Length);
                var target = serializedComponent.FindProperty(callPath + ".m_Target");
                var argument = serializedComponent.FindProperty(callPath + ".m_Arguments.m_IntArgument");
                if (target?.objectReferenceValue == movement && argument != null)
                {
                    index = argument.intValue;
                    return true;
                }
            }
        }

        index = -1;
        return false;
    }

    private static void SetMoveToPositionCalls(Transform root, SmoothCameraMovement movement, int index)
    {
        foreach (var component in root.GetComponentsInChildren<Component>(true))
        {
            if (component == null)
                continue;

            var serializedComponent = new SerializedObject(component);
            var iterator = serializedComponent.GetIterator();
            var dirty = false;
            var enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = true;
                if (iterator.name != "m_MethodName" || iterator.stringValue != "MoveToPosition")
                    continue;

                const string methodSuffix = ".m_MethodName";
                if (!iterator.propertyPath.EndsWith(methodSuffix, StringComparison.Ordinal))
                    continue;

                var callPath = iterator.propertyPath.Substring(0, iterator.propertyPath.Length - methodSuffix.Length);
                var target = serializedComponent.FindProperty(callPath + ".m_Target");
                var argument = serializedComponent.FindProperty(callPath + ".m_Arguments.m_IntArgument");
                if (target?.objectReferenceValue == movement && argument != null)
                {
                    argument.intValue = index;
                    dirty = true;
                }
            }

            if (dirty)
            {
                Undo.RecordObject(component, "Set camera gallery call");
                serializedComponent.ApplyModifiedProperties();
            }
        }
    }

    private static Transform FindSceneTransform(string name)
    {
        var match = UnityEngine.Object.FindObjectsOfType<Transform>(true)
            .FirstOrDefault(transform => transform.name.Trim().Equals(name, StringComparison.OrdinalIgnoreCase));
        if (match == null)
            throw new InvalidOperationException($"{name} could not be found in the open scene.");
        return match;
    }

    private static Transform FindDescendant(Transform parent, string name)
    {
        return parent.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(transform => transform != parent && transform.name.Trim().Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryGetCameraIdentity(string name, out int identity)
    {
        const string prefix = "Camera Pos ";
        identity = -1;
        return name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && int.TryParse(name.Substring(prefix.Length), out identity);
    }
}

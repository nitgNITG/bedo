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

        var content = FindDescendant(galleryWindow, "Content");
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SmoothCameraMovement : MonoBehaviour
{
    [Header("Camera Positions")]
    [SerializeField] private List<Transform> cameraPositions = new List<Transform>();

    [Header("Camera Buttons")]
    [SerializeField] private List<ButtonSelection> cameraButtons = new List<ButtonSelection>();

    [Header("Camera Number")]
    [SerializeField] private TMP_Text cameraNumberText;

    [Header("Movement Settings")]
    [SerializeField] private float moveDuration = 1f;

    private Coroutine currentMovement;

    public void MoveToPosition(int index)
    {
        if (index < 0 || index >= cameraPositions.Count)
        {
            Debug.LogWarning("Camera position index is out of range: " + index);
            return;
        }

        if (cameraPositions[index] == null)
        {
            Debug.LogWarning("Camera position at index " + index + " is not assigned.");
            return;
        }

        if (currentMovement != null)
        {
            StopCoroutine(currentMovement);
        }

        currentMovement = StartCoroutine(
            MoveCamera(cameraPositions[index])
        );

        // Highlight correct camera button
        if (index < cameraButtons.Count &&
            cameraButtons[index] != null)
        {
            ButtonSelection.SelectSpecific(
                cameraButtons[index]
            );
        }

        // Update camera number text (01, 02, 03...)
        if (cameraNumberText != null)
        {
            cameraNumberText.text = (index + 1).ToString("D2");
        }
    }

    private IEnumerator MoveCamera(Transform target)
    {
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;

        Vector3 targetPosition = target.position;
        Quaternion targetRotation = target.rotation;

        float elapsedTime = 0f;

        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;

            float t = elapsedTime / moveDuration;
            t = Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector3.Lerp(
                startPosition,
                targetPosition,
                t
            );

            transform.rotation = Quaternion.Slerp(
                startRotation,
                targetRotation,
                t
            );

            yield return null;
        }

        transform.position = target.position;
        transform.rotation = target.rotation;

        currentMovement = null;
    }
}

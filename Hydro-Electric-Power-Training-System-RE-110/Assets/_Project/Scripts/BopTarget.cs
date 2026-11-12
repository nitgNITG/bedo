using UnityEngine;

public class BopTarget : MonoBehaviour
{
    [Header("Bop Settings")]
    [SerializeField] private float intensity = 0.2f;
    [SerializeField] private float speed = 2f;

    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.localPosition;
    }

    private void Update()
    {
        float yOffset = Mathf.Sin(Time.time * speed) * intensity;

        transform.localPosition = startPosition + Vector3.up * yOffset;
    }
}
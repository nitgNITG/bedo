using UnityEngine;

public class PVRheostat : MonoBehaviour
{
    [Header("Resistance")]
    [SerializeField] private float maxResistance = 1000f;
    [SerializeField] private float minResistance = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float knobValue = 1f;

    public float Resistance
    {
        get
        {
            return Mathf.Lerp(
                minResistance,
                maxResistance,
                knobValue
            );
        }
    }

    public void SetKnobValue(float value)
    {
        knobValue = Mathf.Clamp01(value);
    }
}
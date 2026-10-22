using UnityEngine;
using UnityEngine.UI;
using Obvious.Soap;

[RequireComponent(typeof(Slider))]
public class BindSliderToFloatVariableUsingUpdate : MonoBehaviour
{
    [SerializeField] private FloatVariable floatVariable;

    private Slider slider;

    void Awake()
    {
            if (!enabled) return; // Restored legacy handlers stay inert while disabled.
        slider = GetComponent<Slider>();
    }

    void Update()
    {
        if (floatVariable != null)
        {
             floatVariable.Value=slider.value ;
        }
    }
}

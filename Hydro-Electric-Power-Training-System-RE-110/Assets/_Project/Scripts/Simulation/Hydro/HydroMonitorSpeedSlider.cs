using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bedo.Hydro
{
    // A monitor-local view of the existing speed input; it never owns RPM/calculation state.
    [DefaultExecutionOrder(600)]
    public sealed class HydroMonitorSpeedSlider : MonoBehaviour
    {
        public HydroSpeedInput input;
        public Slider control;
        public Transform meshHandle;
        public Vector3 bottomLocal, topLocal;
        public TMP_Text percentage;

        private void OnEnable() { control.onValueChanged.AddListener(SetPercentage); Refresh(); }
        private void OnDisable() { control.onValueChanged.RemoveListener(SetPercentage); }
        private void SetPercentage(float value)
        {
            if (input && input.SoftwareAvailable) input.SetPercentage(value);
            Refresh();
        }
        private void LateUpdate() { Refresh(); }
        private void Refresh()
        {
            if (!input || !control) return;
            float value = input.SpeedPercentage;
            control.interactable = input.SoftwareAvailable;
            control.SetValueWithoutNotify(value);
            meshHandle.localPosition = Vector3.Lerp(bottomLocal, topLocal, value / 100f);
            if (percentage) percentage.text = value.ToString("0.##") + "%";
        }
    }
}

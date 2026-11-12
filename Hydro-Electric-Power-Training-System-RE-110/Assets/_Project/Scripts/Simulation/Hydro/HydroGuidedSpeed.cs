using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Bedo.Hydro
{
    public sealed class HydroGuidedSpeed : MonoBehaviour
    {
        [SerializeField] private UnityEvent onThreeUnloadedReadings = new UnityEvent();
        [SerializeField] private UnityEvent<float, float> onReading = new UnityEvent<float, float>();

        private readonly HashSet<int> measuredStops = new HashSet<int>();
        private int unloadedStop;
        private float rpm;
        [SerializeField] private Transform rotorVisual;

        private void Update()
        {
            if (outputEnabled && rotorVisual && rpm > 0f)
                rotorVisual.Rotate(Vector3.forward, rpm * 6f * Time.deltaTime, Space.Self);
        }
        private float resistance = 100f;
        private bool positiveConnected;
        private bool negativeConnected;
        private bool outputEnabled;
        private bool countingUnloadedReadings;
        private bool unloadedStepCompleted;

        public bool IsLoaded => positiveConnected && negativeConnected;

        public HydroExperimentOneReadings.Reading CurrentReading => !outputEnabled
            ? default
            : IsLoaded
                ? HydroExperimentOneReadings.ReadLoaded(rpm, resistance)
                : HydroExperimentOneReadings.ReadUnloaded(unloadedStop);

        public void EnableOutput()
        {
            outputEnabled = true;
            PublishReading();
        }

        public void BeginUnloadedSpeedStep()
        {
            if (unloadedStepCompleted)
                return;

            measuredStops.Clear();
            countingUnloadedReadings = true;
        }

        public void IncreaseSpeed() => ChangeSpeed(1);
        public void DecreaseSpeed() => ChangeSpeed(-1);

        public void ConnectPositive()
        {
            positiveConnected = true;
            PublishReading();
        }

        public void ConnectNegative()
        {
            negativeConnected = true;
            PublishReading();
        }

        public void SetResistance(float ohms)
        {
            resistance = ohms;
            if (IsLoaded)
                PublishReading();
        }

        private void ChangeSpeed(int direction)
        {
            if (!outputEnabled)
                return;

            float previousRpm = rpm;
            if (IsLoaded)
            {
                int increment = HydroExperimentOneReadings.LoadedSpeedIncrement;
                float position = rpm / increment;
                int next = direction > 0 ? Mathf.FloorToInt(position) + 1 : Mathf.CeilToInt(position) - 1;
                rpm = Mathf.Clamp(next * increment, 0, HydroExperimentOneReadings.LoadedMaximumRpm);
            }
            else
            {
                unloadedStop = Mathf.Clamp(unloadedStop + direction, 0, HydroExperimentOneReadings.UnloadedMaximumStop);
                rpm = HydroExperimentOneReadings.ReadUnloaded(unloadedStop).Rpm;
            }

            FinishSpeedChange(previousRpm);
        }

        public int SliderStop => IsLoaded ? Mathf.FloorToInt(rpm / HydroExperimentOneReadings.LoadedSpeedIncrement) : unloadedStop;

        public void SetSliderStop(int stop)
        {
            if (!outputEnabled) return;
            float previousRpm = rpm;
            if (IsLoaded)
            {
                rpm = Mathf.Clamp(stop * HydroExperimentOneReadings.LoadedSpeedIncrement, 0, HydroExperimentOneReadings.LoadedMaximumRpm);
            }
            else
            {
                unloadedStop = Mathf.Clamp(stop, 0, HydroExperimentOneReadings.UnloadedMaximumStop);
                rpm = HydroExperimentOneReadings.ReadUnloaded(unloadedStop).Rpm;
            }
            FinishSpeedChange(previousRpm);
        }

        private void FinishSpeedChange(float previousRpm)
        {
            if (rpm == previousRpm)
                return;

            PublishReading();
            if (!IsLoaded && countingUnloadedReadings && unloadedStop > 0 &&
                measuredStops.Add(unloadedStop) && measuredStops.Count == 3)
            {
                countingUnloadedReadings = false;
                unloadedStepCompleted = true;
                onThreeUnloadedReadings.Invoke();
            }
        }

        private void PublishReading()
        {
            if (!outputEnabled)
                return;

            var reading = CurrentReading;
            onReading.Invoke(reading.Rpm, reading.DcVoltage);
        }
    }
}
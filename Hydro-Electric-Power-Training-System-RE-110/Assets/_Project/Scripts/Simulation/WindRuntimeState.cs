using System;
using Obvious.Soap;
using UnityEngine;

namespace TutorialFramework.Simulation
{
    [Serializable]
    public struct WindSimulationData
    {
        public float requestedRPM, activeResistance;
        public int activeExperiment;
        public bool turbineRunning;
        public bool offGridSystemOn, offGridInverterOn, pwmSystemOn, pwmLoadOn, pwmOutputOn;
        public PVCSVReader.WindReading source;
        public PVCSVReader.OffGridReading bus;
        public float batteryCurrent, dumpCurrent;
    }

    /// <summary>
    /// One simulation context, written by its existing controller/calculation
    /// owner. Progression and UI visibility are deliberately separate state.
    /// Reset by WindSimulationSession before scene components initialize.
    /// </summary>
    [CreateAssetMenu(menuName = "Wind Simulation/Runtime State")]
    public sealed class WindRuntimeState : ScriptableVariable<WindSimulationData>
    {
        protected override void OnEnable()
        {
            ResetType = Obvious.Soap.ResetType.None;
            base.OnEnable();
            ResetSession();
        }
        public void ResetSession()
        {
            _initialValue = _value;
            SetValueWithoutNotifying(_value);
        }
    }
}

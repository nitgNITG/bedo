using System;
using UnityEngine;
using UnityEngine.Events;

namespace TutorialFramework
{
    /// <summary>
    /// Shared operational-state view of the Free Lab. This is deliberately
    /// derived from the current physical configuration rather than advancing
    /// one interaction at a time. Experiment evaluators continue to run
    /// independently underneath it.
    /// </summary>
    [DisallowMultipleComponent]
    public class FreeLabOperationalStateMachine : MonoBehaviour
    {
        public enum OperationalState
        {
            A_Initial,
            B_ModulesConnected,
            C_SystemOperating,
            D_OutputVoltage,
            E_Warning,
            F_SoftwareMonitor
        }

        [Header("Optional State D Control")]
        [Tooltip("If left empty, a Free Lab control with 'Enable' in its ID is used. Assign a different control here if your final scene uses another output-voltage switch.")]
        [SerializeField] private FreeLabControl outputVoltageControl;

        [Header("Runtime State (Play Mode)")]
        [SerializeField] private OperationalState currentState =
            OperationalState.A_Initial;
        [SerializeField] private string currentStateName = "A - Initial";
        [SerializeField] private string lastWarning;
        [SerializeField] private bool softwareMonitorOpen;
        [SerializeField] private UnityEvent<string> onStateChanged;

        public OperationalState CurrentState => currentState;
        public string CurrentStateName => currentStateName;

        public void Evaluate(FreeLabManager lab)
        {
            if (softwareMonitorOpen)
            {
                SetState(OperationalState.F_SoftwareMonitor);
                return;
            }

            bool modulesConnected = lab.HasMatchingExperimentConfiguration;
            if (!modulesConnected)
            {
                SetState(OperationalState.A_Initial);
                return;
            }

            if (!lab.IsSystemOperating())
            {
                SetState(OperationalState.B_ModulesConnected);
                return;
            }

            FreeLabControl outputControl = outputVoltageControl != null
                ? outputVoltageControl
                : lab.FindControlContaining("Enable");

            SetState(outputControl != null && outputControl.Value
                ? OperationalState.D_OutputVoltage
                : OperationalState.C_SystemOperating);
        }

        public void ReportWarning(string message)
        {
            lastWarning = message;
            SetState(OperationalState.E_Warning);
        }

        public void SetSoftwareMonitorOpen(bool open)
        {
            softwareMonitorOpen = open;
        }

        private void SetState(OperationalState nextState)
        {
            string nextName = ToDisplayName(nextState);
            if (currentState == nextState && currentStateName == nextName)
                return;

            currentState = nextState;
            currentStateName = nextName;
            Debug.Log("FREE MODE LAB → " + nextName, this);
            onStateChanged?.Invoke(nextName);
        }

        private static string ToDisplayName(OperationalState state)
        {
            switch (state)
            {
                case OperationalState.B_ModulesConnected:
                    return "B - Modules Connected";
                case OperationalState.C_SystemOperating:
                    return "C - System Operating";
                case OperationalState.D_OutputVoltage:
                    return "D - Output Voltage";
                case OperationalState.E_Warning:
                    return "E - Warning";
                case OperationalState.F_SoftwareMonitor:
                    return "F - Software Monitor";
                default:
                    return "A - Initial";
            }
        }
    }
}

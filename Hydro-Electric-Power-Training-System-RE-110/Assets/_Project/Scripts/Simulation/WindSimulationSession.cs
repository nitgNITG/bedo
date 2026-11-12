using UnityEngine;

namespace TutorialFramework.Simulation
{
    /// <summary>
    /// Scene-local reset owner. Does not persist across scene reloads, does not
    /// start experiments, and never resets state merely because a step hides.
    /// </summary>
    [DefaultExecutionOrder(-10000), DisallowMultipleComponent]
    public sealed class WindSimulationSession : MonoBehaviour
    {
        public WindRuntimeState[] contexts;
        public WindFloatVariable[] numbers;
        public WindBoolVariable[] switches;
        public WindConnectionState connections;
        private void Awake()
        {
            foreach (var state in contexts) if (state) state.ResetSession();
            foreach (var number in numbers) if (number) number.ResetSession();
            foreach (var toggle in switches) if (toggle) toggle.ResetSession();
            if (connections) connections.Clear();
        }
        private void OnDestroy()
        {
            // Release scene object references. Never persist destroyed wires.
            if (connections) connections.Clear();
        }
    }
}

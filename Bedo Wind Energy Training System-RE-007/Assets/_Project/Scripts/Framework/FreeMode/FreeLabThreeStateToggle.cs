using System.Collections.Generic;
using UnityEngine;

namespace TutorialFramework
{
    /// <summary>
    /// Shows exactly one of three Free Lab objects based on the shared wire
    /// configuration. State Three has priority when both configured sets match.
    /// </summary>
    [DisallowMultipleComponent]
    public class FreeLabThreeStateToggle : MonoBehaviour
    {
        [Header("Objects")]
        [SerializeField] private GameObject stateOneObject;
        [SerializeField] private GameObject stateTwoObject;
        [SerializeField] private GameObject stateThreeObject;

        [Header("State Two - Connection Paths")]
        [Tooltip("State Two activates when every wire in any one complete path is connected. Leave the list empty to disable State Two.")]
        [SerializeField] private List<WirePath> stateTwoPaths =
            new List<WirePath>();

        [Header("State Three - Required Wires")]
        [Tooltip("Every listed authored wire must be connected. State Three wins if both sets match. Leave empty to disable this state.")]
        [SerializeField] private List<FreeLabWireDefinition> stateThreeRequiredWires =
            new List<FreeLabWireDefinition>();

        [Header("Runtime (Play Mode)")]
        [SerializeField] private int activeState = 1;

        public int ActiveState => activeState;

        private void OnEnable()
        {
            Evaluate(FreeLabManager.Instance);
        }

        public void Evaluate(FreeLabManager lab)
        {
            if (lab == null)
                return;

            int nextState = Matches(lab, stateThreeRequiredWires) ? 3 :
                MatchesAnyPath(lab, stateTwoPaths) ? 2 : 1;
            SetState(nextState);
        }

        private static bool Matches(
            FreeLabManager lab,
            List<FreeLabWireDefinition> requiredWires)
        {
            if (requiredWires == null || requiredWires.Count == 0)
                return false;

            foreach (FreeLabWireDefinition wire in requiredWires)
            {
                if (wire == null || wire.endpointA == null || wire.endpointB == null ||
                    !lab.HasConnection(wire.endpointA, wire.endpointB))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool MatchesAnyPath(
            FreeLabManager lab,
            List<WirePath> paths)
        {
            if (paths == null || paths.Count == 0)
                return false;

            foreach (WirePath path in paths)
            {
                if (path != null && Matches(lab, path.requiredWires))
                    return true;
            }

            return false;
        }

        [System.Serializable]
        private class WirePath
        {
            [Tooltip("Every listed authored wire must be connected for this path.")]
            public List<FreeLabWireDefinition> requiredWires =
                new List<FreeLabWireDefinition>();
        }

        private void SetState(int state)
        {
            activeState = state;

            if (stateOneObject != null)
                stateOneObject.SetActive(state == 1);
            if (stateTwoObject != null)
                stateTwoObject.SetActive(state == 2);
            if (stateThreeObject != null)
                stateThreeObject.SetActive(state == 3);
        }
    }
}

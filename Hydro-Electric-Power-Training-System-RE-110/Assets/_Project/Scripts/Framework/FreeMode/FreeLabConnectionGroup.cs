using System.Collections.Generic;
using UnityEngine;

namespace TutorialFramework
{
    /// <summary>
    /// A reusable named set of authored wires, such as every cable required
    /// to connect one rheostat.  Drag FreeLabWireDefinition components from
    /// the cable parents into this list; their endpoints are read directly
    /// from the cable definitions.
    /// </summary>
    [DisallowMultipleComponent]
    public class FreeLabConnectionGroup : MonoBehaviour
    {
        [SerializeField]
        private string groupId;

        [Tooltip("All of these authored cables must currently be connected. Drag the cable's FreeLabWireDefinition here; no endpoint pairs need to be entered again.")]
        [SerializeField]
        private List<FreeLabWireDefinition> requiredWires =
            new List<FreeLabWireDefinition>();

        public string GroupId => string.IsNullOrWhiteSpace(groupId)
            ? gameObject.name
            : groupId;

        public IReadOnlyList<FreeLabWireDefinition> RequiredWires =>
            requiredWires;

        public bool IsComplete(FreeLabManager lab)
        {
            if (lab == null || requiredWires.Count == 0)
                return false;

            foreach (FreeLabWireDefinition wire in requiredWires)
            {
                if (wire == null || wire.endpointA == null ||
                    wire.endpointB == null || !lab.HasConnection(
                        wire.endpointA, wire.endpointB))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Editor/setup helper used to create a named alternative from a set
        /// of authored wire definitions.
        /// </summary>
        public void Configure(
            string newGroupId,
            IEnumerable<FreeLabWireDefinition> wires)
        {
            groupId = newGroupId;
            requiredWires = wires != null
                ? new List<FreeLabWireDefinition>(wires)
                : new List<FreeLabWireDefinition>();
        }
    }
}

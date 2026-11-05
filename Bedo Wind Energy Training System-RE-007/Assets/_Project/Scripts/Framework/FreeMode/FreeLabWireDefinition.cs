using UnityEngine;

namespace TutorialFramework
{
    /// <summary>
    /// One authored, reusable Free Mode connection. Assign the existing
    /// logical wire parent here; its spline remains a child exactly as made
    /// in the scene. No wire is procedurally drawn by Free Mode.
    /// </summary>
    [DisallowMultipleComponent]
    public class FreeLabWireDefinition : MonoBehaviour
    {
        public FreeLabEndpoint endpointA;
        public FreeLabEndpoint endpointB;

        [Tooltip("Existing authored Wire Parent. The visual spline is expected to be a child of this object.")]
        public GameObject wireParentTemplate;

        [Tooltip("Existing plug visual for endpoint A. Optional if the template already contains it.")]
        public GameObject plugA;

        [Tooltip("Existing plug visual for endpoint B. Optional if the template already contains it.")]
        public GameObject plugB;

        [Tooltip("Permit this specific authored wire to be created or removed while the system is operating. Off by default.")]
        public bool allowWhileOperating;
    }
}

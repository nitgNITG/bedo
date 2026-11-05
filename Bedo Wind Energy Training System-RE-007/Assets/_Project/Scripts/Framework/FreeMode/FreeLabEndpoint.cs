using System.Collections.Generic;
using UnityEngine;

namespace TutorialFramework
{
    /// <summary>
    /// A physical banana/socket in the shared Free Mode lab.  The same
    /// endpoint can participate in any number of independent connections.
    /// </summary>
    [DisallowMultipleComponent]
    public class FreeLabEndpoint : MonoBehaviour
    {
        [SerializeField]
        private string endpointId;

        [SerializeField]
        private Interaction interaction;

        [SerializeField]
        private Transform wireAnchor;

        [Header("Selection Plug")]
        [Tooltip("Existing plug GameObject shown when this banana is selected in Free Mode.")]
        [SerializeField]
        private GameObject selectionPlug;

        [Header("Possible Connection Indicator")]
        [Tooltip("Centered child visual shown only while this endpoint is a valid next wire target.")]
        [SerializeField]
        private GameObject possibleConnectionIndicator;

        [Header("Additional Connection Plugs")]
        [Tooltip("The one authored duplicate used only when this endpoint has its second simultaneous Free Mode wire.")]
        [SerializeField]
        private List<GameObject> additionalConnectionPlugs =
            new List<GameObject>();

        private GameObject previewConnectionPlug;
        private readonly HashSet<GameObject> allocatedConnectionPlugs =
            new HashSet<GameObject>();

        public string EndpointId => string.IsNullOrWhiteSpace(endpointId)
            ? gameObject.name
            : endpointId;

        public Interaction Interaction => interaction;

        public Transform WireAnchor => wireAnchor != null
            ? wireAnchor
            : transform;

        public GameObject SelectionPlug => selectionPlug;
        public int AdditionalConnectionPlugCount =>
            additionalConnectionPlugs != null ? additionalConnectionPlugs.Count : 0;

        public void SetSelectionPlugVisible(bool visible)
        {
            if (visible)
            {
                // Preview the plug that would be used by the next connection.
                // This is the original plug for connection one, then the
                // second plug when the original is already allocated.
                if (previewConnectionPlug == null ||
                    allocatedConnectionPlugs.Contains(previewConnectionPlug))
                {
                    previewConnectionPlug = FindAvailableConnectionPlug();
                }
            }
            else
            {
                previewConnectionPlug = null;
            }

            RefreshPlugVisibility();
        }

        public void SetPossibleConnectionIndicatorVisible(bool visible)
        {
            if (possibleConnectionIndicator != null)
                possibleConnectionIndicator.SetActive(visible);
        }

        /// <summary>Editor setup only: assigns this endpoint's centered indicator instance.</summary>
        public void SetPossibleConnectionIndicator(GameObject indicator)
        {
            if (indicator != null)
                possibleConnectionIndicator = indicator;
        }

        public bool TryAcquireConnectionPlug(out GameObject plug)
        {
            if (previewConnectionPlug != null &&
                !allocatedConnectionPlugs.Contains(previewConnectionPlug))
            {
                plug = previewConnectionPlug;
                allocatedConnectionPlugs.Add(plug);
                RefreshPlugVisibility();
                return true;
            }

            foreach (GameObject candidate in GetConnectionPlugPool())
            {
                if (candidate == null || allocatedConnectionPlugs.Contains(candidate))
                    continue;

                allocatedConnectionPlugs.Add(candidate);
                plug = candidate;
                RefreshPlugVisibility();
                return true;
            }

            plug = null;
            return false;
        }

        public void ReleaseConnectionPlug(GameObject plug)
        {
            if (plug != null)
                allocatedConnectionPlugs.Remove(plug);

            RefreshPlugVisibility();
        }

        /// <summary>
        /// Called by the editor setup utility after duplicating this endpoint's
        /// own plug for another simultaneous wire.
        /// </summary>
        public void AddAdditionalConnectionPlug(GameObject plug)
        {
            if (plug != null && !additionalConnectionPlugs.Contains(plug))
                additionalConnectionPlugs.Add(plug);
        }

        /// <summary>Editor setup only: clears references before generated duplicates are rebuilt.</summary>
        public void ClearAdditionalConnectionPlugs()
        {
            allocatedConnectionPlugs.Clear();
            previewConnectionPlug = null;
            additionalConnectionPlugs.Clear();
        }

        public void SetSelectionPlugIfUnassigned(GameObject plug)
        {
            if (selectionPlug == null && plug != null)
                selectionPlug = plug;
        }

        /// <summary>
        /// Editor setup only: assigns the authored plug that physically belongs
        /// to this endpoint as the first-connection plug.
        /// </summary>
        public void SetSelectionPlug(GameObject plug)
        {
            if (plug != null)
                selectionPlug = plug;
        }

        /// <summary>Editor setup only: gives a duplicated physical socket its own logical identity.</summary>
        public void SetEndpointId(string newEndpointId)
        {
            endpointId = newEndpointId;
        }

        private void Reset()
        {
            interaction = GetComponent<Interaction>();
        }

        private void Awake()
        {
            if (interaction == null)
                interaction = GetComponent<Interaction>();
        }

        private void RefreshPlugVisibility()
        {
            if (selectionPlug != null)
                selectionPlug.SetActive(
                    previewConnectionPlug == selectionPlug ||
                    allocatedConnectionPlugs.Contains(selectionPlug)
                );

            if (additionalConnectionPlugs == null)
                return;

            foreach (GameObject plug in additionalConnectionPlugs)
            {
                if (plug != null)
                    plug.SetActive(
                        previewConnectionPlug == plug ||
                        allocatedConnectionPlugs.Contains(plug)
                    );
            }
        }

        private GameObject FindAvailableConnectionPlug()
        {
            foreach (GameObject candidate in GetConnectionPlugPool())
            {
                if (candidate != null &&
                    !allocatedConnectionPlugs.Contains(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        private IEnumerable<GameObject> GetConnectionPlugPool()
        {
            if (selectionPlug != null)
                yield return selectionPlug;

            if (additionalConnectionPlugs == null)
                yield break;

            foreach (GameObject plug in additionalConnectionPlugs)
            {
                if (plug != null)
                    yield return plug;
            }
        }
    }
}

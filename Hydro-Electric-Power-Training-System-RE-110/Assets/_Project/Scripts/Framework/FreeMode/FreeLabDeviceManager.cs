using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TutorialFramework
{
    /// <summary>
    /// Central readout for interchangeable Free Lab devices. Add this once
    /// under Free Lab, then make child FreeLabDeviceGroup objects for AC lamps,
    /// DC lamps, or future device families.
    /// </summary>
    [DisallowMultipleComponent]
    public class FreeLabDeviceManager : MonoBehaviour
    {
        [SerializeField] private List<FreeLabDeviceGroup> deviceGroups =
            new List<FreeLabDeviceGroup>();

        public IReadOnlyList<FreeLabDeviceGroup> DeviceGroups => deviceGroups;

        public FreeLabDevice GetConnectedDevice(string groupId)
        {
            FreeLabDeviceGroup group = deviceGroups.FirstOrDefault(candidate =>
                candidate != null && string.Equals(candidate.GroupId, groupId,
                    System.StringComparison.OrdinalIgnoreCase));
            return group != null ? group.ConnectedDevice : null;
        }

        public bool IsGroupComplete(string groupId) =>
            GetConnectedDevice(groupId) != null;

        /// <summary>Editor/setup helper for replacing the managed families.</summary>
        public void Configure(IEnumerable<FreeLabDeviceGroup> groups)
        {
            deviceGroups = groups != null
                ? groups.Where(group => group != null).Distinct().ToList()
                : new List<FreeLabDeviceGroup>();
        }

        public void Evaluate(FreeLabManager lab)
        {
            foreach (FreeLabDeviceGroup group in deviceGroups)
                if (group != null) group.Evaluate(lab);
        }

        private void Reset() => DiscoverGroups();

        private void OnValidate() => DiscoverGroups();

        private void OnTransformChildrenChanged() => DiscoverGroups();

        private void DiscoverGroups()
        {
            deviceGroups = GetComponentsInChildren<FreeLabDeviceGroup>(true)
                .Where(group => group != null && group != GetComponent<FreeLabDeviceGroup>())
                .ToList();
        }
    }
}

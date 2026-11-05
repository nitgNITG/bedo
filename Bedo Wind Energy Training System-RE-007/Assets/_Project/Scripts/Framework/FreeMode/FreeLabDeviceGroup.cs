using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TutorialFramework
{
    /// <summary>
    /// A named set of interchangeable physical devices. For example, an AC
    /// lamp group can accept either a fully connected LED lamp or a fully
    /// connected halogen lamp. The group never changes wiring; it only reports
    /// which configured device is currently connected.
    /// </summary>
    [DisallowMultipleComponent]
    public class FreeLabDeviceGroup : MonoBehaviour
    {
        [SerializeField] private string groupId;
        [Tooltip("Any one of these devices may satisfy this group.")]
        [SerializeField] private List<FreeLabDevice> devices =
            new List<FreeLabDevice>();
        [SerializeField] private UnityEvent<FreeLabDevice> onConnectedDeviceChanged;

        [Header("Runtime (Play Mode)")]
        [SerializeField] private FreeLabDevice connectedDevice;

        public string GroupId => string.IsNullOrWhiteSpace(groupId)
            ? gameObject.name : groupId;
        public FreeLabDevice ConnectedDevice => connectedDevice;
        public bool IsComplete => connectedDevice != null;
        public IReadOnlyList<FreeLabDevice> Devices => devices;

        public void Evaluate(FreeLabManager lab)
        {
            FreeLabDevice next = null;
            foreach (FreeLabDevice device in devices)
            {
                if (device != null && device.IsConnected)
                {
                    next = device;
                    break;
                }
            }

            // This is an exclusive physical-device family. Even if a stale
            // or overlapping connection makes two alternatives appear
            // complete, exactly one lamp/device is allowed to drive visuals.
            foreach (FreeLabDevice device in devices)
            {
                if (device != null)
                    device.SetSelectedByDeviceGroup(device == next);
            }

            if (next != connectedDevice)
            {
                connectedDevice = next;
                onConnectedDeviceChanged?.Invoke(connectedDevice);
            }
        }

        public bool Contains(FreeLabDevice device) => device != null &&
            devices.Contains(device);

        /// <summary>Editor/setup helper for a named interchangeable device family.</summary>
        public void Configure(string id, IEnumerable<FreeLabDevice> alternatives)
        {
            groupId = id;
            devices = alternatives != null
                ? new List<FreeLabDevice>(alternatives)
                : new List<FreeLabDevice>();
        }
    }
}

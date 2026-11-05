using System.Collections.Generic;
using Obvious.Soap;
using UnityEngine;

namespace TutorialFramework.Simulation
{
    /// <summary>The Free Lab manager's authoritative live physical connections.</summary>
    [CreateAssetMenu(menuName = "Wind Simulation/Connections")]
    public sealed class WindConnectionState : ScriptableList<FreeLabWire>, IReadOnlyList<FreeLabWire>
    {
        protected override void OnEnable() { _resetOn = ResetType.None; base.OnEnable(); }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using TutorialFramework;
using UnityEngine;

namespace Bedo.Hydro
{
    public sealed class HydroFreeLabCircuits : MonoBehaviour
    {
        [Serializable]
        public sealed class Circuit
        {
            public int experiment;
            public FreeLabWireDefinition[] startup;
            public FreeLabWireDefinition[] afterPower;

            public bool Contains(FreeLabWireDefinition wire) =>
                Array.IndexOf(startup, wire) >= 0 || Array.IndexOf(afterPower, wire) >= 0;

            public bool CanStart(FreeLabManager lab) =>
                startup.Length > 0 && startup.All(wire => lab.HasConnection(wire.endpointA, wire.endpointB));
        }

        [SerializeField] private Circuit[] circuits;
        [SerializeField] private FreeLabControl mainPower;

        private Circuit[] poweredCircuits;
        public int ActiveExperiment => mainPower && mainPower.Value && poweredCircuits != null ? poweredCircuits[0].experiment : 0;

        public bool AppliesTo(FreeLabControl control) => control == mainPower;

        private bool MatchesConnectedWires(Circuit circuit, FreeLabManager lab) =>
            lab.Connections.All(connection => circuit.startup.Any(wire => Matches(wire, connection)) ||
                                             circuit.afterPower.Any(wire => Matches(wire, connection)));

        public bool CanAdd(FreeLabManager lab, FreeLabWireDefinition wire)
        {
            if (mainPower.Value)
                return poweredCircuits != null && poweredCircuits.Any(circuit =>
                    Array.IndexOf(circuit.afterPower, wire) >= 0 && MatchesConnectedWires(circuit, lab));

            return circuits.Any(circuit => circuit.Contains(wire) && MatchesConnectedWires(circuit, lab));
        }

        public bool CanRemovePowered(FreeLabWireDefinition wire) =>
            poweredCircuits != null && poweredCircuits.Any(circuit => Array.IndexOf(circuit.afterPower, wire) >= 0);

        public bool TrySelectAtPowerOn(FreeLabManager lab)
        {
            Circuit[] matches = circuits.Where(circuit => circuit.CanStart(lab) &&
                MatchesConnectedWires(circuit, lab)).ToArray();
            if (matches.Length == 0 || matches.Any(circuit => circuit.experiment != matches[0].experiment))
                return false;

            poweredCircuits = matches;
            return true;
        }

        private void LateUpdate()
        {
            if (!mainPower.Value)
                poweredCircuits = null;
        }

        private static bool Matches(FreeLabWireDefinition wire, FreeLabWire connection) =>
            (wire.endpointA == connection.EndpointA && wire.endpointB == connection.EndpointB) ||
            (wire.endpointB == connection.EndpointA && wire.endpointA == connection.EndpointB);
    }
}

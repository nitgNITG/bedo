using Obvious.Soap;
using UnityEngine;

namespace TutorialFramework.Simulation
{
    [CreateAssetMenu(menuName = "Wind Simulation/Speed Settings")]
    public sealed class WindSimulationSettings : ScriptableObject
    {
        public FloatReference maximumRPM = new FloatReference(1000, true);
        public FloatReference sliderIncrement = new FloatReference(200, true);
        public FloatReference minimumRPM = new FloatReference(0, true);
        public FloatReference initialRPM = new FloatReference(0, true);
        public FloatReference legacyClickIncrement = new FloatReference(100, true);
        public FloatReference visualRotorMultiplier = new FloatReference(.05f, true);
    }

    public static class WindSoapReference
    {
        public static bool Bound(FloatReference r) => r != null && (r.UseLocal || r.Variable != null);
        public static bool Bound(BoolReference r) => r != null && (r.UseLocal || r.Variable != null);
        public static float Read(FloatReference r, float fallback) => Bound(r) ? r.Value : fallback;
        public static bool Read(BoolReference r, bool fallback) => Bound(r) ? r.Value : fallback;
        public static void Write(FloatReference r, ref float fallback, float value) { if (Bound(r)) r.Value = value; else fallback = value; }
        public static void Write(BoolReference r, ref bool fallback, bool value) { if (Bound(r)) r.Value = value; else fallback = value; }
    }
}

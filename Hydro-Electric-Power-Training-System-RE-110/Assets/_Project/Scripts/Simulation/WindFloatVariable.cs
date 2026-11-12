using Obvious.Soap;
using UnityEngine;

namespace TutorialFramework.Simulation
{
    // Initialize late-loaded assets in Editor Play Mode as well as players.
    // SOAP's normal editor initialization event precedes menu -> lab loading.
    [CreateAssetMenu(menuName = "Wind Simulation/Float")]
    public sealed class WindFloatVariable : FloatVariable
    {
        protected override void OnEnable()
        {
            ResetType = Obvious.Soap.ResetType.None; base.OnEnable(); ResetSession();
        }
        public void ResetSession() { _initialValue = _value; SetValueWithoutNotifying(_value); }
    }
}

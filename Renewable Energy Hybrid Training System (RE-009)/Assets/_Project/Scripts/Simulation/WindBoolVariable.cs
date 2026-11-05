using Obvious.Soap;
using UnityEngine;

namespace TutorialFramework.Simulation
{
    [CreateAssetMenu(menuName = "Wind Simulation/Bool")]
    public sealed class WindBoolVariable : BoolVariable
    {
        protected override void OnEnable()
        {
            ResetType = Obvious.Soap.ResetType.None; base.OnEnable(); ResetSession();
        }
        public void ResetSession() { _initialValue = _value; SetValueWithoutNotifying(_value); }
    }
}

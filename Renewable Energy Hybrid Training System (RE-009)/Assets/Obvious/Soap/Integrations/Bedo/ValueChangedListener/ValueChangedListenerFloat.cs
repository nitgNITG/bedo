using UnityEngine;
using UnityEngine.Events;

namespace Obvious.Soap
{
    [AddComponentMenu("Soap/ValueListeners/ValueChangedListenerFloat")]
    public class ValueChangedListenerFloat : ValueChangedListenerGeneric<float>
    {
        [SerializeField] private VariableResponse[] _variableResponses = null;
        protected override VariableResponse<float>[] VariableResponses => _variableResponses;

        [System.Serializable]
        public class VariableResponse : VariableResponse<float>
        {
            // Replace with your actual FloatVariable class name
            [SerializeField] private FloatVariable _floatVariable = null;
            public override ScriptableVariable<float> ScriptableVariable => _floatVariable;

            [SerializeField] private FloatUnityEvent _response = null;
            public override UnityEvent<float> Response => _response;
        }

        [System.Serializable]
        public class FloatUnityEvent : UnityEvent<float> { }
    }
}

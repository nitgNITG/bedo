using UnityEngine;
using UnityEngine.Events;

namespace Obvious.Soap
{
    [AddComponentMenu("Soap/ValueListeners/ValueChangedListenerInt")]
    public class ValueChangedListenerInt : ValueChangedListenerGeneric<int>
    {
        [SerializeField] private VariableResponse[] _variableResponses = null;
        protected override VariableResponse<int>[] VariableResponses => _variableResponses;

        [System.Serializable]
        public class VariableResponse : VariableResponse<int>
        {
            // Replace with your actual IntVariable class name
            [SerializeField] private IntVariable _intVariable = null;
            public override ScriptableVariable<int> ScriptableVariable => _intVariable;

            [SerializeField] private IntUnityEvent _response = null;
            public override UnityEvent<int> Response => _response;
        }

        [System.Serializable]
        public class IntUnityEvent : UnityEvent<int> { }
    }
}
using UnityEngine;
using UnityEngine.Events;

namespace Obvious.Soap
{
    [AddComponentMenu("Soap/ValueListeners/ValueChangedListenerString")]
    public class ValueChangedListenerString : ValueChangedListenerGeneric<string>
    {
        [SerializeField] private VariableResponse[] _variableResponses = null;
        protected override VariableResponse<string>[] VariableResponses => _variableResponses;

        [System.Serializable]
        public class VariableResponse : VariableResponse<string>
        {
            // Replace with your actual StringVariable class name
            [SerializeField] private StringVariable _stringVariable = null;
            public override ScriptableVariable<string> ScriptableVariable => _stringVariable;

            [SerializeField] private StringUnityEvent _response = null;
            public override UnityEvent<string> Response => _response;
        }

        [System.Serializable]
        public class StringUnityEvent : UnityEvent<string> { }
    }
}
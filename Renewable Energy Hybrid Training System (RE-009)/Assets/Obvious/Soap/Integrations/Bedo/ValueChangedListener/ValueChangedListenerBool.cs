using UnityEngine;
using UnityEngine.Events;
using Obvious.Soap;
namespace Bedo.Integrations.Soap
{
    /// <summary>
    /// A listener for a ScriptableVariableBool, replicating the SOAP style used in EventListenerBool.
    /// </summary>
    [AddComponentMenu("Soap/ValueListeners/ValueChangedListenerBool")]
    public class ValueChangedListenerBool : ValueChangedListenerGeneric<bool>
    {
        // This array holds the variable + response pairs for bool.
        [SerializeField]
        private VariableResponse[] _variableResponses = null;

        // We override the base property to provide our array of VariableResponse<bool>.
        protected override VariableResponse<bool>[] VariableResponses => _variableResponses;

        /// <summary>
        /// Internal class that ties together a ScriptableVariableBool and a UnityEvent<bool>.
        /// </summary>
        [System.Serializable]
        public class VariableResponse : VariableResponse<bool>
        {
            [SerializeField]
            private BoolVariable _scriptableVariable = null;
            public override ScriptableVariable<bool> ScriptableVariable => _scriptableVariable;

            [SerializeField]
            private BoolUnityEvent _response = null;
            public override UnityEvent<bool> Response => _response;
        }

        /// <summary>
        /// A UnityEvent<bool> so that we can respond to boolean changes in the Inspector.
        /// </summary>
        [System.Serializable]
        public class BoolUnityEvent : UnityEvent<bool> { }
    }
}
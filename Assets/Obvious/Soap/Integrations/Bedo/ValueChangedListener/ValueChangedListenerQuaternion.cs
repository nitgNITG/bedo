using UnityEngine;
using UnityEngine.Events;

namespace Obvious.Soap
{
    [AddComponentMenu("Soap/ValueListeners/ValueChangedListenerQuaternion")]
    public class ValueChangedListenerQuaternion : ValueChangedListenerGeneric<Quaternion>
    {
        [SerializeField] private VariableResponse[] _variableResponses = null;
        protected override VariableResponse<Quaternion>[] VariableResponses => _variableResponses;

        [System.Serializable]
        public class VariableResponse : VariableResponse<Quaternion>
        {
            // Replace with your actual QuaternionVariable class name
            [SerializeField] private QuaternionVariable _quatVariable = null;
            public override ScriptableVariable<Quaternion> ScriptableVariable => _quatVariable;

            [SerializeField] private QuaternionUnityEvent _response = null;
            public override UnityEvent<Quaternion> Response => _response;
        }

        [System.Serializable]
        public class QuaternionUnityEvent : UnityEvent<Quaternion> { }
    }
}
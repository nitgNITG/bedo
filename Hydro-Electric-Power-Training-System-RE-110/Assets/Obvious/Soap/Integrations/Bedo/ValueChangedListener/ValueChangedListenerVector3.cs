using UnityEngine;
using UnityEngine.Events;

namespace Obvious.Soap
{
    [AddComponentMenu("Soap/ValueListeners/ValueChangedListenerVector3")]
    public class ValueChangedListenerVector3 : ValueChangedListenerGeneric<Vector3>
    {
        [SerializeField] private VariableResponse[] _variableResponses = null;
        protected override VariableResponse<Vector3>[] VariableResponses => _variableResponses;

        [System.Serializable]
        public class VariableResponse : VariableResponse<Vector3>
        {
            // Replace with your actual Vector3Variable class name
            [SerializeField] private Vector3Variable _vector3Variable = null;
            public override ScriptableVariable<Vector3> ScriptableVariable => _vector3Variable;

            [SerializeField] private Vector3UnityEvent _response = null;
            public override UnityEvent<Vector3> Response => _response;
        }

        [System.Serializable]
        public class Vector3UnityEvent : UnityEvent<Vector3> { }
    }
}
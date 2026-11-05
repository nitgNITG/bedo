using UnityEngine;
using UnityEngine.Events;

namespace Obvious.Soap
{
    [AddComponentMenu("Soap/ValueListeners/ValueChangedListenerVector2")]
    public class ValueChangedListenerVector2 : ValueChangedListenerGeneric<Vector2>
    {
        [SerializeField] private VariableResponse[] _variableResponses = null;
        protected override VariableResponse<Vector2>[] VariableResponses => _variableResponses;

        [System.Serializable]
        public class VariableResponse : VariableResponse<Vector2>
        {
            // Replace with your actual Vector2Variable class name
            [SerializeField] private Vector2Variable _vector2Variable = null;
            public override ScriptableVariable<Vector2> ScriptableVariable => _vector2Variable;

            [SerializeField] private Vector2UnityEvent _response = null;
            public override UnityEvent<Vector2> Response => _response;
        }

        [System.Serializable]
        public class Vector2UnityEvent : UnityEvent<Vector2> { }
    }
}

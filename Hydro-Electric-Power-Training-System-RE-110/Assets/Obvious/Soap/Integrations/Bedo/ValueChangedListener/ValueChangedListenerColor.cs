using UnityEngine;
using UnityEngine.Events;

namespace Obvious.Soap
{
    [AddComponentMenu("Soap/ValueListeners/ValueChangedListenerComponent")]
    public class ValueChangedListenerComponent : ValueChangedListenerGeneric<Component>
    {
        [SerializeField] private VariableResponse[] _variableResponses = null;
        protected override VariableResponse<Component>[] VariableResponses => _variableResponses;

        [System.Serializable]
        public class VariableResponse : VariableResponse<Component>
        {
            // Replace with your actual ComponentVariable class name
            [SerializeField] private ComponentVariable _componentVariable = null;
            public override ScriptableVariable<Component> ScriptableVariable => _componentVariable;

            [SerializeField] private ComponentUnityEvent _response = null;
            public override UnityEvent<Component> Response => _response;
        }

        [System.Serializable]
        public class ComponentUnityEvent : UnityEvent<Component> { }
    }
}
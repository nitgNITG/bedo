using UnityEngine;
using UnityEngine.Events;

namespace Obvious.Soap
{
    [AddComponentMenu("Soap/ValueListeners/ValueChangedListenerGameObject")]
    public class ValueChangedListenerGameObject : ValueChangedListenerGeneric<GameObject>
    {
        [SerializeField] private VariableResponse[] _variableResponses = null;
        protected override VariableResponse<GameObject>[] VariableResponses => _variableResponses;

        [System.Serializable]
        public class VariableResponse : VariableResponse<GameObject>
        {
            // Replace with your actual GameObjectVariable class name
            [SerializeField] private GameObjectVariable _gameObjectVariable = null;
            public override ScriptableVariable<GameObject> ScriptableVariable => _gameObjectVariable;

            [SerializeField] private GameObjectUnityEvent _response = null;
            public override UnityEvent<GameObject> Response => _response;
        }

        [System.Serializable]
        public class GameObjectUnityEvent : UnityEvent<GameObject> { }
    }
}
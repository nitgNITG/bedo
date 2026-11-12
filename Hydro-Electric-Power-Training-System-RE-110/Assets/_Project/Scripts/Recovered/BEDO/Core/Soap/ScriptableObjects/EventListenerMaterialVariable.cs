using UnityEngine;
using UnityEngine.Events;
using Obvious.Soap;

[AddComponentMenu("Soap/EventListeners/EventListener"+nameof(MaterialVariable))]
public class EventListenerMaterialVariable : EventListenerGeneric<MaterialVariable>
{
    [SerializeField] private EventResponse[] _eventResponses = null;
    protected override EventResponse<MaterialVariable>[] EventResponses => _eventResponses;

    [System.Serializable]
    public class EventResponse : EventResponse<MaterialVariable>
    {
        [SerializeField] private ScriptableEventMaterialVariable _scriptableEvent = null;
        public override ScriptableEvent<MaterialVariable> ScriptableEvent => _scriptableEvent;

        [SerializeField] private MaterialVariableUnityEvent _response = null;
        public override UnityEvent<MaterialVariable> Response => _response;
    }

    [System.Serializable]
    public class MaterialVariableUnityEvent : UnityEvent<MaterialVariable>
    {
        
    }
}

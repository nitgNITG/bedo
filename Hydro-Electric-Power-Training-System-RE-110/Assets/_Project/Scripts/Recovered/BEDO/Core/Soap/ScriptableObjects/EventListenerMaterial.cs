using UnityEngine;
using UnityEngine.Events;
using Obvious.Soap;

[AddComponentMenu("Soap/EventListeners/EventListener"+nameof(Material))]
public class EventListenerMaterial : EventListenerGeneric<Material>
{
    [SerializeField] private EventResponse[] _eventResponses = null;
    protected override EventResponse<Material>[] EventResponses => _eventResponses;

    [System.Serializable]
    public class EventResponse : EventResponse<Material>
    {
        [SerializeField] private ScriptableEventMaterial _scriptableEvent = null;
        public override ScriptableEvent<Material> ScriptableEvent => _scriptableEvent;

        [SerializeField] private MaterialUnityEvent _response = null;
        public override UnityEvent<Material> Response => _response;
    }

    [System.Serializable]
    public class MaterialUnityEvent : UnityEvent<Material>
    {
        
    }
}

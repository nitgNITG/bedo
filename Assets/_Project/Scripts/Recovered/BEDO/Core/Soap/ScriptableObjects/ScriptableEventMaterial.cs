using UnityEngine;
using Obvious.Soap;

[CreateAssetMenu(fileName = "scriptable_event_" + nameof(Material), menuName = "Soap/ScriptableEvents/"+ nameof(Material))]
public class ScriptableEventMaterial : ScriptableEvent<Material>
{
    
}


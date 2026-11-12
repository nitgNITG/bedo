using UnityEngine;
using Obvious.Soap;

[CreateAssetMenu(fileName = "scriptable_variable_" + nameof(MaterialVariable), menuName = "Soap/ScriptableVariables/"+ nameof(MaterialVariable))]
public class MaterialVariable : ScriptableVariable<Material>
{
    [SerializeField]public string MaterialName;

    
}


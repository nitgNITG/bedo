using UnityEngine;

[CreateAssetMenu(fileName = "Tooltip Data", menuName = "UI/Tooltip Data")]
public class TooltipData : ScriptableObject
{
    [SerializeField] private string title;
    [TextArea]
    [SerializeField] private string description;

    public string Title => title;
    public string Description => description;
}
//using UnityEngine.UIElements;
using System;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "DialogRequestScriptableObject.asset", menuName = "ScriptableObjects/UI/DialogRequest")]
[Serializable]
public class DialogRequest : ScriptableObject
{
    [SerializeField] public Sprite Icon;
    [SerializeField] public string Title;
    [SerializeField] public string Message;
    [SerializeField] public bool ShowOk, ShowYes, ShowNo, ShowCancel;
    [SerializeField] public UnityEvent OnOk, OnYes, OnNo, OnCancel;
}

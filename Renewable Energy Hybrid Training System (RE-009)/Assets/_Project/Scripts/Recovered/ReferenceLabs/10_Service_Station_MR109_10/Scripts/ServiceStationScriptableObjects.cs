#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Obvious.Soap;
using Obvious.Soap.Attributes;
namespace Bedo.Experiments.Compact_Mr109
{
    [CreateAssetMenu(menuName = "Bedo/Experiments/Compact/ServiceStationScriptableObject")]
    public class ServiceStationScriptableObjects : ScriptableObject
    {
        // Start is called before the first frame update
        [SerializeField][SubAsset(order = 1)] public BoolVariable m_MainPowerOn;
        [SerializeField][SubAsset(order = 1)] public BoolVariable m_EmergencyPushButton;
        [Space(10)]

     
        [SerializeField][SubAsset(order = 2)] public IntVariable m_PneumaticValue;
        [SerializeField][SubAsset(order = 2)] public IntVariable m_VirtualPneumaticValue;
        [SerializeField][SubAsset(order = 3)] public IntVariable m_MinPneumaticValue;
        [SerializeField][SubAsset(order = 3)] public IntVariable m_MaxPneumaticValue;
        [Space(10)]
        [SerializeField][SubAsset(order = 4)] public ScriptableListGameObject m_PneumaticArrowRotations;

        [Space(10)]
        [SerializeField][SubAsset(order = 5)] public BoolVariable m_IsHoveringOnKnob;
        [SerializeField][SubAsset(order = 5)] public BoolVariable m_IsHoveringCanvasIncreaseOrDecrease;
    }
}
#endif


using UnityEngine;
using Obvious.Soap;
using Obvious.Soap.Attributes;
namespace Bedo.Experiments.Compact_Mr109
{
    [CreateAssetMenu(menuName = "Bedo/Experiments/Compact/LoadingStation/LoadingStationUnityVariables")]
    public class LoadingStationUnityVariables : ScriptableObject
    {
        //[SerializeField][SubAsset(order = 1)] public BoolVariable m_PneumaticOn;
        [SerializeField][SubAsset (order = 1)] public BoolVariable m_MainPowerOn;
        [SerializeField][SubAsset (order = 2)] public BoolVariable m_ReedMagnet;
        [SerializeField][SubAsset(order = 3)] public BoolVariable m_LightBarrierReciver;
        [SerializeField][SubAsset(order = 4)] public BoolVariable m_CapacitiveTrigger;
        [SerializeField][SubAsset(order = 4)] public BoolVariable m_Limit_01_Trigger;
        [SerializeField][SubAsset(order = 4)] public BoolVariable m_Limit_02_Trigger;

        [Space(20)]
        [SerializeField][SubAsset(order = 5)] public BoolVariable m_Led_SolenoidValveOfPistonOn;
        [SerializeField][SubAsset(order = 6)] public BoolVariable m_Led_SolenoidSolenoidValveOfArmOn;
        [SerializeField][SubAsset(order = 7)] public BoolVariable m_Led_SolenoidSolenoidValveOfSuctionOn;
        [SerializeField][SubAsset(order = 8)] public BoolVariable m_IsProcessOn;
        [Space(20)]
        [SerializeField][SubAsset(order = 8)] public BoolVariable m_Led_Start;
        [SerializeField][SubAsset(order = 9)] public BoolVariable m_Led_Running;
        [SerializeField][SubAsset(order = 10)] public BoolVariable m_Led_Stop;
        [Space(20)]
        [SerializeField][SubAsset(order = 11)] public IntVariable m_PneumaticValue;
        [Space(20)]
        [SerializeField][SubAsset(order = 12)] public GameObjectVariable m_DropSampleLocationTriggerGameObject;

        [Space(20)]
        [SerializeField][SubAsset(order = 13)] public BoolVariable IsLoadingSamplePositionEmpty;
        //[SerializeField][SubAsset(order = 2)] public BoolVariable m_ReedMagnet;
        //[SerializeField][SubAsset(order = 3)] public BoolVariable m_LightBarrierReciver;
        //[SerializeField][SubAsset(order = 4)] public BoolVariable m_CapacitiveTrigger;
    }
}
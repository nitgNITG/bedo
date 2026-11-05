using UnityEngine;

using Obvious.Soap;
using Obvious.Soap.Attributes;

namespace Bedo.Experiments.Compact_Mr109
{
    [CreateAssetMenu(menuName = "Bedo/Experiments/Compact/Assembly_Station_MR109_05/UnityRelated")]

    public class MR109_05_Assembly_UnityRelated : ScriptableObject
    {
        [SerializeField][SubAsset(order = 0)] public BoolVariable MR109_05_000_PowerOn;
        [SerializeField][SubAsset(order = 0)] public IntVariable MR109_05_001_PneumaticTurns;
        
        [SerializeField][SubAsset(order = 0)] public BoolVariable MR109_05_002_StartLed;
        [SerializeField][SubAsset(order = 0)] public BoolVariable MR109_05_003_RunningLed;
        [SerializeField][SubAsset(order = 0)] public BoolVariable MR109_05_004_StopLed;
        [SerializeField][SubAsset(order = 0)] public BoolVariable MR109_05_005_ProcessOn;

        [SerializeField][SubAsset(order = 0)] public BoolVariable MR109_05_006_Valve1_Solenoid_Led;
        [SerializeField][SubAsset(order = 0)] public BoolVariable MR109_05_007_Valve2_Solenoid_Led;
        [SerializeField][SubAsset(order = 0)] public BoolVariable MR109_05_008_Valve3_Solenoid_Led;

        [SerializeField][SubAsset(order = 0)] public BoolVariable MR109_05_009_Motor1_Signal;
        [SerializeField][SubAsset(order = 0)] public BoolVariable MR109_05_010_Motor2_Signal;


        [SerializeField][SubAsset(order = 0)] public BoolVariable MR109_05_011_Reed01_Led;
        [SerializeField][SubAsset(order = 0)] public BoolVariable MR109_05_012_Reed02_Led;
        [SerializeField][SubAsset(order = 0)] public BoolVariable MR109_05_013_Reed03_Led;
        [SerializeField][SubAsset(order = 0)] public BoolVariable MR109_05_014_Capasative_Led;



        [SerializeField][SubAsset(order = 0)] public GameObjectVariable MR109_05_015_SampleAtCapacitive_Sensor;
        [SerializeField][SubAsset(order = 0)] public GameObjectVariable MR109_05_016_CapAtSuction;

        [SerializeField][SubAsset(order = 0)] public ScriptableListGameObject MR109_05_017_AboveFirstBeltSamples;
        [SerializeField][SubAsset(order = 0)] public ScriptableListGameObject MR109_05_018_AboveSecondBeltCaps;

        [SerializeField][SubAsset(order = 0)] public FloatVariable MR109_05_019_FirstBeltVelocity;
        [SerializeField][SubAsset(order = 0)] public FloatVariable MR109_05_020_SecondBeltVelocity;

        [SerializeField][SubAsset(order = 0)] public BoolVariable MR109_05_021_L_B_1_Led;
        [SerializeField][SubAsset(order = 0)] public BoolVariable MR109_05_022_L_B_2_Led;
        [SerializeField][SubAsset(order = 0)] public BoolVariable MR109_05_023_L_B_3_Led;
        [SerializeField][SubAsset(order = 0)] public BoolVariable MR109_05_024_L_B_4_Led;



        [SerializeField][SubAsset(order = 0)] public BoolVariable MR109_05_025_IsCapPositionEmpty; 


        //[SerializeField][SubAsset(order = 0)] public BoolVariable MR109_07_010_Reed01_Led;
        //[SerializeField][SubAsset(order = 0)] public BoolVariable MR109_07_011_Reed02_Led;
    }
}
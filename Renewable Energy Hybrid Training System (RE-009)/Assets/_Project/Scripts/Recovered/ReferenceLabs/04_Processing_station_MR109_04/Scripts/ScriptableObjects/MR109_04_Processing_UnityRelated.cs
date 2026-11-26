#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Obvious.Soap;
using Obvious.Soap.Attributes;



namespace Bedo.Experiments.Compact_Mr109
{
    [CreateAssetMenu(menuName = "Bedo/Experiments/Compact/Processing_Station_MR109_04/UnityRelated")]
    public class MR109_04_Processing_UnityRelated : ScriptableObject
    {
        [SerializeField][SubAsset(order = 0)] public BoolVariable MR109_04_000_PowerOn;
        [SerializeField][SubAsset(order = 0)] public IntVariable MR109_04_001_PneumaticTurns;
        [SerializeField][SubAsset(order = 0)] public BoolVariable MR109_04_002_ProcessOn;

        [SerializeField][SubAsset(order = 0)] public BoolVariable MR109_04_003_StartLed;
        [SerializeField][SubAsset(order = 0)] public BoolVariable MR109_04_004_RunningLed;
        [SerializeField][SubAsset(order = 0)] public BoolVariable MR109_04_005_StopLed;

        [SerializeField][SubAsset(order = 0)] public BoolVariable MR109_04_006_Stepper_Fiber_Dir_Signal;
        [SerializeField][SubAsset(order = 0)] public BoolVariable MR109_04_007_Stepper_Fiber_Pul_Signal;
        [SerializeField][SubAsset(order = 0)] public BoolVariable MR109_04_008_Stepper_Drill_Dir_Signal;
        [SerializeField][SubAsset(order = 0)] public BoolVariable MR109_04_009_Stepper_Drill_Pul_Signal;

        [SerializeField][SubAsset(order = 0)] public BoolVariable MR109_04_010_Valve_01_Signal;
        [SerializeField][SubAsset(order = 0)] public BoolVariable MR109_04_011_Valve_02_Signal;
        [SerializeField][SubAsset(order = 0)] public BoolVariable MR109_04_012_Valve_03_Signal;

        [SerializeField][SubAsset(order = 0)] public BoolVariable MR109_04_013_Rotary_Motor_CW_Signal;
        [SerializeField][SubAsset(order = 0)] public BoolVariable MR109_04_014_Rotary_Motor_CCW_Signal;
        [SerializeField][SubAsset(order = 0)] public BoolVariable MR109_04_015_Driller_Motor_Signal;

        [SerializeField][SubAsset] public BoolVariable MR109_04_016_Encoder_P_Signal;
        [SerializeField][SubAsset] public BoolVariable MR109_04_017_Limit_1_P_Signal;
        [SerializeField][SubAsset] public BoolVariable MR109_04_018_Limit_2_P_Signal;
        [SerializeField][SubAsset] public BoolVariable MR109_04_019_Limit_3_P_Signal;
        [SerializeField][SubAsset] public BoolVariable MR109_04_020_Limit_4_P_Signal;



        [SerializeField][SubAsset] public BoolVariable MR109_04_021_Inductive_P_Signal;

        [SerializeField][SubAsset] public BoolVariable MR109_04_022_Capacitive_1_P_Signal;
        [SerializeField][SubAsset] public BoolVariable MR109_04_023_Capacitive_2_P_Signal;
        [SerializeField][SubAsset] public BoolVariable MR109_04_024_Capacitive_3_P_Signal;
        [SerializeField][SubAsset] public BoolVariable MR109_04_025_Capacitive_4_P_Signal;
        [SerializeField][SubAsset] public BoolVariable MR109_04_026_Capacitive_5_P_Signal;


        [SerializeField][SubAsset] public BoolVariable MR109_04_027_Main_Sensor_P_Signal;

        [SerializeField][SubAsset] public BoolVariable MR109_04_028_Reed_1_Signal;
        [SerializeField][SubAsset] public BoolVariable MR109_04_029_Reed_2_Signal;
        [SerializeField][SubAsset] public BoolVariable MR109_04_030_Reed_3_Signal;
        
        
        [SerializeField][SubAsset] public IntVariable MR109_04_031_Measuring_System;
        [SerializeField][SubAsset] public BoolVariable MR109_04_032_Measuring_DetectingIsDrilled;
        [SerializeField][SubAsset] public GameObjectVariable MR109_04_033_Sample_At_Measuring_Device;

        [SerializeField][SubAsset] public BoolVariable MR109_04_034_Rotary_Inductive_P;

        [SerializeField][SubAsset] public ScriptableEventBool MR109_04_035_FireRayValve03;
        [SerializeField][SubAsset] public BoolVariable MR109_04_036_IsDrillingEnabled;

        [SerializeField][SubAsset] public GameObjectVariable MR109_04_037_GameObjectAtCapacitive60Deg;
        [SerializeField][SubAsset] public GameObjectVariable MR109_04_038_GameObjectAtCapacitive120Deg;

        [SerializeField][SubAsset] public ScriptableEventBool MR109_04_035_FireRayValve01;
        [SerializeField][SubAsset] public ScriptableEventBool MR109_04_035_FireRayValve02;



    }
}
#endif

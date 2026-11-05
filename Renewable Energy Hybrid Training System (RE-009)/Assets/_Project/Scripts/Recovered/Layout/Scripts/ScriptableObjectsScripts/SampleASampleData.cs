using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Obvious.Soap;
using Obvious.Soap.Attributes;
namespace Bedo.Experiments.Compact_Mr109
{


    [CreateAssetMenu(menuName = "Bedo/Experiments/Compact/Sample A Sample Data")]
    public class SampleASampleData : ScriptableObject
    {

        [SerializeField][SubAsset(order = 1)] FloatVariable Height;
        //[SerializeField] ScriptableEnumSampleColor Color;
        //[SerializeField] ScriptableEnumSampleType Type;
        //[SerializeField][SubAsset(order = 2)] Mater
        [SerializeField] MaterialVariable CurrentMaterial;
       
     
        //[SerializeField][SubAsset(order = 3)] Color TypeMetal;

        [SerializeField][SubAsset(order = 4)] BoolVariable IsDrilled;

        public void ChangeMaterial(MaterialVariable materialVariable)
        {
            CurrentMaterial = materialVariable;
        }

    }
}

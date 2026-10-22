using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Obvious.Soap;
using Obvious.Soap.Attributes;

namespace Bedo.Experiments.Compact_Mr109
{
    [CreateAssetMenu(menuName = "Bedo/Experiments/Compact/LoadingStationEvents")]
    public class LoadingStationEvents : ScriptableObject
    {
       

        [Space]

        [SerializeField][SubAsset(order = 6)] public ScriptableEventNoParam OnClickPutSample;
        [SerializeField][SubAsset(order = 6)] public ScriptableEventNoParam OnClickPutCap;



        
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Obvious.Soap;
using UnityEngine.UI;

namespace Bedo.Core
{
    [CreateAssetMenu(fileName = "MechaTooltipScriptableObject", menuName = "ScriptableObjects/MechaTooltipScriptableObject", order = 1)]


    public class MechaTooltipScriptableObject : ScriptableObject
    {

        public StringVariable ArabicDesciptionStringVariable;
        public StringVariable EnglishDesciptionStringVariable;
        public Sprite Sprite;
    }
}
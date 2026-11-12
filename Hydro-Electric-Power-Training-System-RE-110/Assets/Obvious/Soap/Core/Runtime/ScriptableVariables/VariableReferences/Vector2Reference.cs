using UnityEngine;

namespace Obvious.Soap
{
    [System.Serializable]
    public class Vector2Reference : VariableReference<Vector2Variable, UnityEngine.Vector2>
    {

        public Vector2Reference () : base()
        {

        }

        public Vector2Reference (Vector2 initialValue, bool useLocal = false) : base()
        {
            LocalValue = initialValue;
            UseLocal = useLocal;
        }

    }
}
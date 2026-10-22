
namespace Obvious.Soap
{
    [System.Serializable]
    public class Vector3Reference : VariableReference<Vector3Variable, UnityEngine.Vector3>
    {

        public Vector3Reference() : base()
        {
        }

        public Vector3Reference(UnityEngine.Vector3 initialValue, bool useLocal = false) : base ()
        {
            LocalValue = initialValue;
            UseLocal = useLocal;
        }
    }
}
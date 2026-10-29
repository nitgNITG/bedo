namespace Obvious.Soap
{
    [System.Serializable]
    public class BoolReference : VariableReference<BoolVariable, bool>
    {
        public BoolReference() : base()
        {

        }

        public BoolReference(bool initialValue, bool useLocal = false) : base()
        {
            LocalValue = initialValue;
            UseLocal = useLocal;
        }

    }
}
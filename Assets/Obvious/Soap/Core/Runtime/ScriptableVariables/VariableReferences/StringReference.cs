namespace Obvious.Soap
{
    [System.Serializable]
    public class StringReference : VariableReference<StringVariable, string>
    {
        public StringReference() : base()
        {

        }

        public StringReference(string initialValue, bool useLocal = false) : base()
        {
            LocalValue = initialValue;
            UseLocal = useLocal;
        }

    }
}
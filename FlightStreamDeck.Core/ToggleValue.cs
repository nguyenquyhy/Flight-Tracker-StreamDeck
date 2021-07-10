namespace FlightStreamDeck.Core
{
    public class ToggleValue
    {
        const string DEFAULT_UNIT = "number";
        const int DEFAULT_DECIMALS = 0;
        public ToggleValue(string name, string unit, int decimals)
        {
            Name = name;
            Decimals = decimals;
            Unit = unit;
        }

        public ToggleValue(string name): this(name, DEFAULT_UNIT, DEFAULT_DECIMALS)
        {
        }

        public string Name
        {
            get;
            set;
        }

        public string Value
        {
            get;
            set;
        }

        public int Decimals
        {
            get;
            set;
        }

        public string Unit
        {
            get;
            set;
        }
    }
}

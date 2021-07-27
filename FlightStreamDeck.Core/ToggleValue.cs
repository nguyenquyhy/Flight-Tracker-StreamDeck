namespace FlightStreamDeck.Core
{
    public class ToggleValue
    {
        private const string DEFAULT_UNIT = "number";
        private const int DEFAULT_DECIMALS = 0;
        public ToggleValue(string name, string unit, int decimals)
        {
            Name = name;
            Decimals = decimals;
            Unit = unit;
        }

        public ToggleValue(string name): this(name, DEFAULT_UNIT, DEFAULT_DECIMALS)
        {
        }
        public ToggleValue(string name, string unit) : this(name, unit, DEFAULT_DECIMALS)
        {
        }


        private string _name;
        public string Name
        {
            get => _name ??= string.Empty;
            set => _name = value;
        }

        public double Value
        {
            get;
            set;
        }

        public int? Decimals
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

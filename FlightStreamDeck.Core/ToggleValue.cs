using System;

namespace FlightStreamDeck.Core
{
    public class ToggleValue: BaseToggle
    {
        public const string DEFAULT_UNIT = "number";
        public const string LVARS_PREFIX = "L_";
        private const int DEFAULT_DECIMALS = 0;
        public ToggleValue(string name, string unit, int? decimals, double? minValue, double?  maxValue): base(name)
        {
            Decimals = decimals;
            Unit = unit;
            MinValue = minValue;
            MaxValue = maxValue;
        }
        public ToggleValue(string name, string unit, int? decimals) : this(name, unit, decimals, null, null)
        {
        }
        public ToggleValue(string name): this(name, DEFAULT_UNIT, DEFAULT_DECIMALS)
        {
        }
        public ToggleValue(string name, string unit) : this(name, unit, DEFAULT_DECIMALS)
        {
        }

        public double Value
        {
            get;
            set;
        }
        public double? LVarID
        {
            get;
            set;
        }

        public double? MinValue
        {
            get;
            set;
        }
        public double? MaxValue
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

        public static bool IsLVar(ToggleValue val)
        {
            return val.Name.StartsWith(LVARS_PREFIX);
        }

        public static string GetSimvarName(ToggleValue val)
        {
            return string.Format("({0})", val.Name.Replace(ToggleValue.LVARS_PREFIX, "L:")); ;
        }
    }
}

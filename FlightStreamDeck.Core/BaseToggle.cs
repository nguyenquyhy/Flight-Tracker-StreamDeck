using System;

namespace FlightStreamDeck.Core
{
    public enum VarType
    {
        NORMAL,
        LVAR,
        MOBIFLIGHT
    }
    public class BaseToggle
    {
        public static string LVARS_PREFIX = "L:";
        public static string MOBIFLIGHT_PREFIX = "MobiFlight.";
        public string Name
        {
            get;
            set;
        }
        private VarType _varType;
        public VarType VarType
        {
            get
            {
                return _varType;
            }
        }
        public BaseToggle(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Name cannot be null or empty");
            }
            Name = name;
            _varType = GetVarType(name);
        }
        private static VarType GetVarType(string name)
        {
            if (IsLVar(name))
            {
                return Core.VarType.LVAR;
            }
            if (IsMobiflighVar(name))
            {
                return Core.VarType.MOBIFLIGHT;
            }
            return Core.VarType.NORMAL;
        }
        private static bool IsLVar(string name)
        {
            return IsVarType(name, LVARS_PREFIX);
        }
        private static bool IsMobiflighVar(string name)
        {
            return IsVarType(name, MOBIFLIGHT_PREFIX);
        }
        private static bool IsVarType(string name, string varTypePrefix)
        {
            return name.StartsWith(varTypePrefix);
        }
    }
}

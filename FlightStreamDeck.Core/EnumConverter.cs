using System;

namespace FlightStreamDeck.Core
{
    public class EnumConverter
    {
        public TOGGLE_VALUE? GetVariableEnum(string? value)
        {
            if (value != null && !float.TryParse(value, out _) 
                && Enum.TryParse(value.Trim().Replace(":", "__").Replace(" ", "_"), true, out TOGGLE_VALUE result))
            {
                return result;
            }

            return null;
        }

        public (uint? number, TOGGLE_VALUE? variable) GetUIntOrVariable(string value)
        {
            if (uint.TryParse(value, out var result))
            {
                return (result, null);
            }
            else if (int.TryParse(value, out var intResult))
            {
                return (unchecked((uint)intResult), null);
            }
            else
            {
                var variable = GetVariableEnum(value);
                return (null, variable);
            }
        }
    }
}

using System;

namespace FlightStreamDeck.Core
{
    public class EnumConverter
    {
        public TOGGLE_EVENT? GetEventEnum(string value)
        {
            if (value != null && Enum.TryParse(value.Replace(":", "__").Replace(" ", "_"), true, out TOGGLE_EVENT result))
            {
                return result;
            }

            return null;
        }

        public TOGGLE_VALUE? GetVariableEnum(string value)
        {
            if (!float.TryParse(value, out float floatvalue) && value != null && Enum.TryParse(value.Replace(":", "__").Replace(" ", "_"), true, out TOGGLE_VALUE result))
            {
                return result;
            }

            return null;
        }
    }
}

using FlightStreamDeck.Core;

namespace FlightStreamDeck.Logics.Actions.NavCom
{
    internal class XpdrHandler : BcdHandler
    {
        public XpdrHandler(
            IFlightConnector flightConnector, IEventRegistrar eventRegistrar, IEventDispatcher eventDispatcher, 
            TOGGLE_VALUE active, TOGGLE_VALUE? standby, TOGGLE_VALUE? batteryVariable, TOGGLE_VALUE? avionicsVariable,
            KnownEvents? toggle, KnownEvents? set,
            string minPattern, string mask) : 
            base(flightConnector, eventRegistrar, eventDispatcher, active, standby, batteryVariable, avionicsVariable, toggle, set, minPattern, mask)
        {
        }

        protected override uint FormatValueForSimConnect(string value)
        {
            uint data = 0;
            for (var i = 0; i < value.Length; i++)
            {
                uint digit = (byte)value[i] - (uint)48;
                data = data * 16 + digit;
            }
            return data;
        }

        protected override string FormatValueForDisplay(double value, TOGGLE_VALUE simvar)
        {
            var stringValue = base.FormatValueForDisplay(value, simvar);
            if (stringValue != string.Empty) stringValue = stringValue.PadLeft(4, '0');
            return stringValue;
        }
    }
}

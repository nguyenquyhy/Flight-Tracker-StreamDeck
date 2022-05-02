using FlightStreamDeck.Core;

namespace FlightStreamDeck.Logics.Actions.NavCom
{
    public class BcdHandler : NavComHandler
    {
        public BcdHandler(
            IFlightConnector flightConnector, IEventRegistrar eventRegistrar, IEventDispatcher eventDispatcher, 
            TOGGLE_VALUE active, TOGGLE_VALUE? standby, TOGGLE_VALUE? batteryVariable, TOGGLE_VALUE? avionicsVariable,
            KnownEvents? toggle, KnownEvents? set,
            string minPattern, string mask) :
            base(flightConnector, eventRegistrar, eventDispatcher, active, standby, batteryVariable, avionicsVariable, toggle, set, minPattern, mask)
        {
        }

        /// <summary>
        /// BCD encode
        /// </summary>
        protected override uint FormatValueForSimConnect(string value)
        {
            uint data = 0;
            // NOTE: SimConnect ignore first 1
            value = value[1..];
            for (var i = 0; i < value.Length; i++)
            {
                uint digit = (byte)value[i] - (uint)48;
                data = data * 16 + digit;
            }
            return data;
        }
    }
}

using FlightStreamDeck.Core;
using System;

namespace FlightStreamDeck.Logics.Actions.NavCom
{
    public class AdfHandler : HzHandler
    {
        public AdfHandler(
            IFlightConnector flightConnector, IEventRegistrar eventRegistrar, IEventDispatcher eventDispatcher, 
            TOGGLE_VALUE active, TOGGLE_VALUE? standby, TOGGLE_VALUE? batteryVariable, TOGGLE_VALUE? avionicsVariable, 
            KnownEvents? toggle, KnownEvents? set, 
            string minPattern, string mask) : 
            base(flightConnector, eventRegistrar, eventDispatcher, active, standby, batteryVariable, avionicsVariable, toggle, set, minPattern, mask)
        {
        }

        /// <summary>
        /// Convert from default MHz to kHz
        /// </summary>
        protected override string FormatValueForDisplay(double value, TOGGLE_VALUE simvar)
        {
            return ((int)Math.Round(value * 1000)).ToString();
        }
    }
}

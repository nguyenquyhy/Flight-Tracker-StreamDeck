using FlightStreamDeck.Core;

namespace FlightStreamDeck.Logics.Actions.NavCom;

public class HzHandler : NavComHandler
{
    public HzHandler(
        IFlightConnector flightConnector, IEventRegistrar eventRegistrar, IEventDispatcher eventDispatcher, 
        TOGGLE_VALUE active, TOGGLE_VALUE? standby, TOGGLE_VALUE? batteryVariable, TOGGLE_VALUE? avionicsVariable,
        KnownEvents? toggle, KnownEvents? set,
        string minPattern, string maxPattern, string mask) : 
        base(flightConnector, eventRegistrar, eventDispatcher, active, standby, batteryVariable, avionicsVariable, toggle, set, minPattern, maxPattern, mask)
    {
    }

    protected override uint FormatValueForSimConnect(string value)
    {
        return uint.Parse(value) * 1000;
    }
}

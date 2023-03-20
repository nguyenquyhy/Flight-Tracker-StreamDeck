namespace FlightStreamDeck.Logics.Actions.NavCom;

public class HzHandler : NavComHandler
{
    public HzHandler(
        IEventRegistrar eventRegistrar, IEventDispatcher eventDispatcher, SimVarManager simVarManager,
        string active, string? standby, string? batteryVariable, string? avionicsVariable,
        KnownEvents? toggle, KnownEvents? set,
        string minPattern, string maxPattern, string mask) :
        base(eventRegistrar, eventDispatcher, simVarManager, active, standby, batteryVariable, avionicsVariable, toggle, set, minPattern, maxPattern, mask)
    {
    }

    protected override uint FormatValueForSimConnect(string value)
    {
        return uint.Parse(value) * 1000;
    }
}

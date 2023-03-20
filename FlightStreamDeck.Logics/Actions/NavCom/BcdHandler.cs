namespace FlightStreamDeck.Logics.Actions.NavCom;

public class BcdHandler : NavComHandler
{
    public BcdHandler(
        IEventRegistrar eventRegistrar, IEventDispatcher eventDispatcher, SimVarManager simVarManager,
        string active, string? standby, string? batteryVariable, string? avionicsVariable,
        KnownEvents? toggle, KnownEvents? set,
        string minPattern, string maxPattern, string mask) :
        base(eventRegistrar, eventDispatcher, simVarManager, active, standby, batteryVariable, avionicsVariable, toggle, set, minPattern, maxPattern, mask)
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

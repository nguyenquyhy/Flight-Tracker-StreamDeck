namespace FlightStreamDeck.Logics.Actions.NavCom;

internal class XpdrHandler : BcdHandler
{
    public XpdrHandler(
        IEventRegistrar eventRegistrar, IEventDispatcher eventDispatcher, SimVarManager simVarManager,
        string active, string? standby, string? batteryVariable, string? avionicsVariable,
        KnownEvents? toggle, KnownEvents? set,
        string minPattern, string maxPattern, string mask) :
        base(eventRegistrar, eventDispatcher, simVarManager, active, standby, batteryVariable, avionicsVariable, toggle, set, minPattern, maxPattern, mask)
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

    protected override string FormatValueForDisplay(double value, SimVarRegistration simvar)
    {
        var stringValue = base.FormatValueForDisplay(value, simvar);
        if (stringValue != string.Empty) stringValue = stringValue.PadLeft(4, '0');
        return stringValue;
    }
}

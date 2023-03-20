using System;

namespace FlightStreamDeck.Logics.Actions.NavCom;

public class AdfHandler : HzHandler
{
    public AdfHandler(
        IEventRegistrar eventRegistrar, IEventDispatcher eventDispatcher, SimVarManager simVarManager,
        string active, string? standby, string? batteryVariable, string? avionicsVariable,
        KnownEvents? toggle, KnownEvents? set) :
        base(
            eventRegistrar, eventDispatcher, simVarManager, active, standby, batteryVariable, avionicsVariable, toggle, set,
            "0100", "8888",
            ""
        )
    {
    }

    protected override string AddDefaultPattern(string value)
    {
        // no-op
        return value;
    }

    /// <summary>
    /// Convert from default MHz to kHz
    /// </summary>
    protected override string FormatValueForDisplay(double value, SimVarRegistration simvar)
    {
        return ((int)Math.Round(value * 1000)).ToString();
    }

    /// <summary>
    /// BCD encode of Hz value
    /// </summary>
    protected override uint FormatValueForSimConnect(string value)
    {
        uint data = 0;
        // NOTE: SimConnect ignore first 1
        value += "0000";
        for (var i = 0; i < value.Length; i++)
        {
            uint digit = (byte)value[i] - (uint)48;
            data = data * 16 + digit;
        }
        return data;
    }
}

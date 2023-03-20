using FlightStreamDeck.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace FlightStreamDeck.Logics.Actions.NavCom;

public abstract class NavComHandler
{
    private readonly IEventRegistrar eventRegistrar;
    private readonly IEventDispatcher eventDispatcher;
    private readonly SimVarManager simVarManager;
    private readonly SimVarRegistration active;
    private readonly SimVarRegistration? standby;
    private readonly SimVarRegistration? batteryVariable;
    private readonly SimVarRegistration? avionicsVariable;
    private readonly KnownEvents? toggle;
    private readonly KnownEvents? set;

    public string MinPattern { get; }
    public string MaxPattern { get; }
    public string Mask { get; }

    public bool IsSettable => set != null;

    public NavComHandler(
        IEventRegistrar eventRegistrar,
        IEventDispatcher eventDispatcher,
        SimVarManager simVarManager,
        string active,
        string? standby,
        string? batteryVariable,
        string? avionicsVariable,
        KnownEvents? toggle,
        KnownEvents? set,
        string minPattern,
        string maxPattern,
        string mask
    )
    {
        this.eventRegistrar = eventRegistrar;
        this.eventDispatcher = eventDispatcher;
        this.simVarManager = simVarManager;
        this.active = simVarManager.GetRegistration(active) ?? throw new ArgumentException("Invalid Active variable!", "active");
        this.standby = simVarManager.GetRegistration(standby);
        this.batteryVariable = simVarManager.GetRegistration(batteryVariable);
        this.avionicsVariable = simVarManager.GetRegistration(avionicsVariable);
        this.toggle = toggle;
        this.set = set;
        MinPattern = minPattern;
        MaxPattern = maxPattern;
        Mask = mask;
    }

    public void RegisterSimValuesAndEvents()
    {
        if (toggle != null)
        {
            eventRegistrar.RegisterEvent(toggle.Value.ToString());
        }
        eventRegistrar.RegisterEvent(set.ToString());

        simVarManager.RegisterSimValues(GetSimVars().ToArray());
    }

    public void DeRegisterSimValues()
    {
        simVarManager.DeRegisterSimValues(GetSimVars().ToArray());
    }

    public async Task TriggerAsync(string value, bool swap)
    {
        value = AddDefaultPattern(value);

        uint data = FormatValueForSimConnect(value);
        eventDispatcher.Trigger(set.ToString(), data);

        if (toggle != null && swap)
        {
            await Task.Delay(500);
            eventDispatcher.Trigger(toggle.Value.ToString());
        }
    }

    public (string activeString, string standbyString, bool showActiveOnly, bool dependant) GetDisplayValues(Dictionary<SimVarRegistration, double> genericValues)
    {
        bool TryGetValue([NotNullWhen(true)] SimVarRegistration? variable, out double value)
        {
            if (variable != null && genericValues.TryGetValue(variable, out var newValue))
            {
                value = newValue;
                return true;
            }
            value = 0;
            return false;
        }

        bool dependant = true;
        if (TryGetValue(batteryVariable, out var batteryValue))
        {
            dependant = dependant && batteryValue != 0;
        }
        if (TryGetValue(avionicsVariable, out var avionicsValue))
        {
            dependant = dependant && avionicsValue != 0;
        }

        var value1 = string.Empty;
        var value2 = string.Empty;
        if (dependant)
        {
            if (TryGetValue(active, out var doubleValue1))
            {
                value1 = FormatValueForDisplay(doubleValue1, active);
            }
            if (TryGetValue(standby, out var doubleValue2))
            {
                value2 = FormatValueForDisplay(doubleValue2, standby);
            }
        }
        return (value1, value2, standby == null, dependant);
    }

    public void SwapFrequencies()
    {
        if (toggle != null)
        {
            eventDispatcher.Trigger(toggle.Value.ToString());
        }
    }

    protected virtual string AddDefaultPattern(string value)
    {
        if (value.Length < MinPattern.Length)
        {
            value += MinPattern.Substring(value.Length);
        }

        return value;
    }

    protected abstract uint FormatValueForSimConnect(string value);

    protected virtual string FormatValueForDisplay(double value, SimVarRegistration simvar)
    {
        return value.ToString("F" + simvar.variableName.GetDecimals(), CultureInfo.InvariantCulture);
    }

    protected virtual List<SimVarRegistration> GetSimVars()
    {
        var values = new List<SimVarRegistration>
        {
            active
        };
        if (standby != null)
        {
            values.Add(standby);
        }
        if (batteryVariable != null)
        {
            values.Add(batteryVariable);
        }
        if (avionicsVariable != null)
        {
            values.Add(avionicsVariable);
        }

        return values;
    }
}

using FlightStreamDeck.Core;
using System.Collections.Generic;

namespace FlightStreamDeck.Logics;

public class SimVarManager
{
    private readonly ILogger<SimVarManager> logger;
    private readonly IFlightConnector flightConnector;

    public SimVarManager(ILogger<SimVarManager> logger, IFlightConnector flightConnector)
    {
        this.logger = logger;
        this.flightConnector = flightConnector;
    }

    public SimVarRegistration? GetRegistration(string? name, string? unit = null)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        unit = string.IsNullOrWhiteSpace(unit) ? null : unit.Trim();
        name = name.Trim();

        if (!name.StartsWith("L:"))
        {
            name = name.Replace("__", ":").Replace("_", " ");
        }

        if (name.IsKnown())
        {
            // Variable is in the enum list
        }
        if (unit == null)
        {
            unit = name.GetUnit(unit);
        }
        return new SimVarRegistration(name, unit);
    }

    public void RegisterSimValues(params SimVarRegistration[] registrations)
    {
        flightConnector.RegisterSimValues(FilterRegistrations(registrations));
    }

    public void DeRegisterSimValues(params SimVarRegistration[] registrations)
    {
        flightConnector.DeRegisterSimValues(FilterRegistrations(registrations));
    }

    private IEnumerable<SimVarRegistration> FilterRegistrations(IEnumerable<SimVarRegistration> registrations)
    {
        foreach (var registration in registrations)
        {
            var variable = registration.variableName;
            var unit = registration.variableUnit;
            if (variable.IsKnown())
            {
                // Variable is in the enum list
                var unitFinal = variable.GetUnit(unit);

                yield return new(variable, unitFinal);
            }
            else if (variable.StartsWith("L:"))
            {
                // LVar
                yield return registration;
            }
            else
            {
                // Unknown
                logger.LogInformation("Unknown variable {variable}", variable);
            }
        }
    }
}

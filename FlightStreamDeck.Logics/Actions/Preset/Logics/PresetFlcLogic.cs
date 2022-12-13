using System;

namespace FlightStreamDeck.Logics.Actions.Preset;

public class PresetFlcLogic : PresetBaseValueLogic
{
    public PresetFlcLogic(ILogger<PresetFlcLogic> logger, IFlightConnector flightConnector) : base(logger, flightConnector)
    {
    }

    public override bool GetActive(AircraftStatus status) => status.IsApFlcOn;

    public override double? GetValue(AircraftStatus status) => status.ApAirspeed;

    public override void Toggle(AircraftStatus status)
    {
        logger.LogInformation("Toggle AP FLC.");
        if (status.IsApFlcOn)
        {
            flightConnector.ApFlcOff();
        }
        else
        {
            flightConnector.ApAirSpeedSet((uint)Math.Round(status.IndicatedAirSpeed));
            flightConnector.ApFlcOn();
        }
    }

    public override void Sync(AircraftStatus status)
    {
        logger.LogInformation("Sync AP FLC Airspeed.");
        UpdateValue(status.IndicatedAirSpeed);
    }

    protected override double CalculateNewValue(double currentValue, int sign, int increment) => Math.Max(0, currentValue + increment * sign);

    protected override void UpdateSimValue(double value) => flightConnector.ApAirSpeedSet((uint)value);
}

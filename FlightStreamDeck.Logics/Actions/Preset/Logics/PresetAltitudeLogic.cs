using System;
using System.Threading.Tasks;

namespace FlightStreamDeck.Logics.Actions;

public class PresetAltitudeLogic : PresetBaseValueLogic
{
    private readonly ILogger<PresetAltitudeLogic> logger;

    public PresetAltitudeLogic(ILogger<PresetAltitudeLogic> logger, IFlightConnector flightConnector) : base(flightConnector)
    {
        this.logger = logger;
    }

    public override bool GetActive(AircraftStatus status) => status.IsApAltOn;

    public override double? GetValue(AircraftStatus status) => status.ApAltitude;

    public override bool IsChanged(AircraftStatus? oldStatus, AircraftStatus newStatus)
        => newStatus.ApAltitude != oldStatus?.ApAltitude || newStatus.IsApAltOn != oldStatus?.IsApAltOn;

    public override void Toggle(AircraftStatus status)
    {
        logger.LogInformation("Toggle AP ALT.");
        flightConnector.ApAltToggle();
    }

    public override void Sync(AircraftStatus status)
    {
        logger.LogInformation("Sync AP ALT. Current value: {value}.", status.ApAltitude);
        UpdateValue(Math.Max(0, Math.Floor(status.Altitude / 100)) * 100);
    }

    protected override double CalculateNewValue(double currentValue, int sign, int increment)
        => (uint)(currentValue + 100 * sign * increment);

    protected override void UpdateSimValue(double value) => flightConnector.ApAltSet((uint)value);
}

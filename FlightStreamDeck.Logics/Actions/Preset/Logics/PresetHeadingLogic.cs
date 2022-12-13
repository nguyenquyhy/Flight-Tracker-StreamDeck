namespace FlightStreamDeck.Logics.Actions.Preset;

public class PresetHeadingLogic : PresetBaseValueLogic
{
    public PresetHeadingLogic(ILogger<PresetHeadingLogic> logger, IFlightConnector flightConnector) : base(logger, flightConnector)
    {
    }

    public override bool GetActive(AircraftStatus status) => status.IsApHdgOn;

    public override double? GetValue(AircraftStatus status) => status.ApHeading;

    public override void Toggle(AircraftStatus status)
    {
        logger.LogInformation("Toggle AP HDG.");
        flightConnector.ApHdgToggle();
    }

    public override void Sync(AircraftStatus status)
    {
        logger.LogInformation("Sync AP HDG. Current value: {value}.", status.ApHeading);
        UpdateValue(status.Heading);
    }

    protected override double CalculateNewValue(double currentValue, int sign, int increment)
        => ((uint)currentValue).IncreaseSpherical(increment * sign);

    protected override void UpdateSimValue(double value)
        => flightConnector.ApHdgSet((uint)value);
}

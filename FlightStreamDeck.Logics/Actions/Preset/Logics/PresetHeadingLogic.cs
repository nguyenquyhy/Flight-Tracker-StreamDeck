namespace FlightStreamDeck.Logics.Actions;

public class PresetHeadingLogic : PresetBaseValueLogic
{
    private readonly ILogger<PresetHeadingLogic> logger;

    public PresetHeadingLogic(ILogger<PresetHeadingLogic> logger, IFlightConnector flightConnector) : base(flightConnector)
    {
        this.logger = logger;
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
        => CalculateSphericalIncrement((uint)currentValue, sign, increment);

    protected override void UpdateSimValue(double value)
        => flightConnector.ApHdgSet((uint)value);

    private uint CalculateSphericalIncrement(uint currentValue, int sign, int increment)
        => (uint)(currentValue + 360 + sign * increment) % 360;
}

namespace FlightStreamDeck.Logics.Actions;

public class PresetVerticalSpeedLogic : PresetBaseValueLogic
{
    private readonly ILogger<PresetVerticalSpeedLogic> logger;

    public PresetVerticalSpeedLogic(ILogger<PresetVerticalSpeedLogic> logger, IFlightConnector flightConnector) : base(flightConnector)
    {
        this.logger = logger;
    }

    public override bool GetActive(AircraftStatus status) => status.IsApVsOn;

    public override double? GetValue(AircraftStatus status) => status.ApVs;

    public override void Toggle(AircraftStatus status)
    {
        logger.LogInformation("Toggle AP VS.");
        flightConnector.ApVsToggle();
    }

    public override void Sync(AircraftStatus status)
    {
        logger.LogInformation("Level AP VS.");
        UpdateValue(0);
    }

    protected override double CalculateNewValue(double currentValue, int sign, int increment) => currentValue + 100 * sign;

    protected override void UpdateSimValue(double value) => flightConnector.ApVsSet((uint)value);
}
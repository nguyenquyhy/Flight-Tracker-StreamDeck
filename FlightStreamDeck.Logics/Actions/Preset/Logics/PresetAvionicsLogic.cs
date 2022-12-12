namespace FlightStreamDeck.Logics.Actions;

internal class PresetAvionicsLogic : PresetBaseToggleLogic
{
    private readonly ILogger<PresetAvionicsLogic> logger;

    public PresetAvionicsLogic(ILogger<PresetAvionicsLogic> logger, IFlightConnector flightConnector) : base(flightConnector)
    {
        this.logger = logger;
    }

    public override bool GetActive(AircraftStatus status) => status.IsAvMasterOn;

    public override void Toggle(AircraftStatus status)
    {
        logger.LogInformation("Toggle AV Master.");
        flightConnector.AvMasterToggle(status.IsAvMasterOn ? 0u : 1u);
    }
}

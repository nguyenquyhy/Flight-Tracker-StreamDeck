namespace FlightStreamDeck.Logics.Actions;

public class PresetNavLogic : PresetBaseToggleLogic
{
    private readonly ILogger<PresetNavLogic> logger;

    public PresetNavLogic(ILogger<PresetNavLogic> logger, IFlightConnector flightConnector) : base(flightConnector)
    {
        this.logger = logger;
    }

    public override bool GetActive(AircraftStatus status) => status.IsApNavOn;

    public override void Toggle(AircraftStatus status)
    {
        logger.LogInformation("Toggle AP NAV.");
        flightConnector.ApNavToggle();
    }
}

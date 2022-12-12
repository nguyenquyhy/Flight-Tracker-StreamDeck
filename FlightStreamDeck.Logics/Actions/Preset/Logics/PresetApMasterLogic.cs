namespace FlightStreamDeck.Logics.Actions;

public class PresetApMasterLogic : PresetBaseToggleLogic
{
    private readonly ILogger<PresetApMasterLogic> logger;

    public PresetApMasterLogic(ILogger<PresetApMasterLogic> logger, IFlightConnector flightConnector) : base(flightConnector)
    {
        this.logger = logger;
    }

    public override bool GetActive(AircraftStatus status) => status.IsAutopilotOn;

    public override void Toggle(AircraftStatus status)
    {
        logger.LogInformation("Toggle AP Master.");
        flightConnector.ApToggle();
    }
}

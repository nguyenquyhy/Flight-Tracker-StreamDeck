namespace FlightStreamDeck.Logics.Actions;

public class PresetApproachLogic : PresetBaseToggleLogic
{
    private readonly ILogger<PresetApproachLogic> logger;

    public PresetApproachLogic(ILogger<PresetApproachLogic> logger, IFlightConnector flightConnector) : base(flightConnector)
    {
        this.logger = logger;
    }

    public override bool GetActive(AircraftStatus status) => status.IsApAprOn;

    public override void Toggle(AircraftStatus status)
    {
        logger.LogInformation("Toggle AP APR.");
        flightConnector.ApAprToggle();
    }
}

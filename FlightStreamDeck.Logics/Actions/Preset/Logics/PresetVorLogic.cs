namespace FlightStreamDeck.Logics.Actions.Preset;

public class PresetVor1Logic : PresetVorLogic
{
    private readonly IEventDispatcher eventDispatcher;

    public PresetVor1Logic(
        ILogger<PresetVor1Logic> logger,
        IFlightConnector flightConnector,
        IEventDispatcher eventDispatcher
    ) : base(logger, flightConnector)
    {
        this.eventDispatcher = eventDispatcher;
    }

    public override double? GetValue(AircraftStatus status) => status.Nav1OBS;

    protected override void UpdateSimValue(double value)
        => eventDispatcher.Trigger(KnownEvents.VOR1_SET.ToString(), (uint)value);
}

public class PresetVor2Logic : PresetVorLogic
{
    private readonly IEventDispatcher eventDispatcher;

    public PresetVor2Logic(
        ILogger<PresetVor2Logic> logger,
        IFlightConnector flightConnector,
        IEventDispatcher eventDispatcher
    ) : base(logger, flightConnector)
    {
        this.eventDispatcher = eventDispatcher;
    }

    public override double? GetValue(AircraftStatus status) => status.Nav2OBS;

    protected override void UpdateSimValue(double value)
        => eventDispatcher.Trigger(KnownEvents.VOR2_SET.ToString(), (uint)value);
}

public abstract class PresetVorLogic : PresetBaseValueLogic
{
    public PresetVorLogic(ILogger logger, IFlightConnector flightConnector) : base(logger, flightConnector)
    {
    }

    public override bool GetActive(AircraftStatus status) => false;

    public override void Toggle(AircraftStatus status)
    {
    }

    protected override double CalculateNewValue(double currentValue, int sign, int increment)
        => ((uint)currentValue).IncreaseSpherical(increment * sign);
}

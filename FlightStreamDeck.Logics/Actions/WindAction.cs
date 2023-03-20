namespace FlightStreamDeck.Logics.Actions;

[StreamDeckAction("tech.flighttracker.streamdeck.wind.direction")]
public class WindAction : BaseAction
{
    private readonly ILogger<WindAction> logger;
    private readonly IFlightConnector flightConnector;
    private readonly IImageLogic imageLogic;
    private readonly SimVarManager simVarManager;
    private string windDirectionValue = "AMBIENT WIND DIRECTION";
    private string windVelocityValue = "AMBIENT WIND VELOCITY";
    private string headingValue = "PLANE HEADING DEGREES TRUE";

    private double currentWindDirectionValue = 0;
    private double currentWindVelocityValue = 0;
    private double currentHeadingValue = 0;
    private bool currentRelative = true;

    public WindAction(ILogger<WindAction> logger, IFlightConnector flightConnector, IImageLogic imageLogic, SimVarManager simVarManager)
    {
        this.logger = logger;
        this.flightConnector = flightConnector;
        this.imageLogic = imageLogic;
        this.simVarManager = simVarManager;
    }

    protected override async Task OnWillAppear(ActionEventArgs<AppearancePayload> args)
    {
        flightConnector.GenericValuesUpdated += FlightConnector_GenericValuesUpdated;

        RegisterValues();

        await UpdateImage();
    }

    private async void FlightConnector_GenericValuesUpdated(object? sender, ToggleValueUpdatedEventArgs e)
    {
        bool TryGetNewValue(string variable, double currentValue, out double value)
        {
            if (e.GenericValueStatus.TryGetValue(new SimVarRegistration(variable, null), out var newValue) && currentValue != newValue)
            {
                value = newValue;
                return true;
            }
            value = 0;
            return false;
        }

        bool isUpdated = false;

        if (TryGetNewValue(windDirectionValue, currentWindDirectionValue, out var newDirection))
        {
            currentWindDirectionValue = newDirection;
            isUpdated = true;
        }
        if (TryGetNewValue(headingValue, currentHeadingValue, out var newHeading))
        {
            currentHeadingValue = newHeading;
            isUpdated = true;
        }
        if (TryGetNewValue(windVelocityValue, currentWindVelocityValue, out var newVelocity))
        {
            currentWindVelocityValue = newVelocity;
            isUpdated = true;
        }

        if (isUpdated)
        {
            await UpdateImage();
        }
    }

    protected override Task OnWillDisappear(ActionEventArgs<AppearancePayload> args)
    {
        flightConnector.GenericValuesUpdated -= FlightConnector_GenericValuesUpdated;
        DeRegisterValues();
        return Task.CompletedTask;
    }

    private void RegisterValues()
    {
        simVarManager.RegisterSimValues(simVarManager.GetRegistration(windDirectionValue), simVarManager.GetRegistration(windVelocityValue), simVarManager.GetRegistration(headingValue));
    }

    private void DeRegisterValues()
    {
        simVarManager.DeRegisterSimValues(simVarManager.GetRegistration(windDirectionValue), simVarManager.GetRegistration(windVelocityValue), simVarManager.GetRegistration(headingValue));
    }

    protected override Task OnKeyDown(ActionEventArgs<KeyPayload> args)
    {
        currentRelative = !currentRelative;
        _ = UpdateImage();
        return Task.CompletedTask;
    }

    private async Task UpdateImage()
    {
        await SetImageSafeAsync(imageLogic.GetWindImage(currentWindDirectionValue, currentWindVelocityValue, currentHeadingValue, currentRelative));
    }
}

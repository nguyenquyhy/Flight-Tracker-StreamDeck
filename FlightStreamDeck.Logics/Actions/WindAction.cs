using System;

namespace FlightStreamDeck.Logics.Actions;

[StreamDeckAction("tech.flighttracker.streamdeck.wind.direction")]
public class WindAction : BaseAction
{
    private readonly ILogger<WindAction> logger;
    private readonly IFlightConnector flightConnector;
    private readonly IImageLogic imageLogic;
    private readonly SimVarManager simVarManager;
    private readonly SimVarRegistration windDirection;
    private readonly SimVarRegistration windVelocity;
    private readonly SimVarRegistration heading;

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

        this.windDirection = simVarManager.GetRegistration("AMBIENT WIND DIRECTION") ?? throw new InvalidOperationException("Cannot get registration for AMBIENT WIND DIRECTION");
        this.windVelocity = simVarManager.GetRegistration("AMBIENT WIND VELOCITY") ?? throw new InvalidOperationException("Cannot get registration for AMBIENT WIND VELOCITY");
        this.heading = simVarManager.GetRegistration("PLANE HEADING DEGREES TRUE") ?? throw new InvalidOperationException("Cannot get registration for PLANE HEADING DEGREES TRUE");
    }

    protected override async Task OnWillAppear(ActionEventArgs<AppearancePayload> args)
    {
        flightConnector.GenericValuesUpdated += FlightConnector_GenericValuesUpdated;

        RegisterValues();

        await UpdateImage();
    }

    private async void FlightConnector_GenericValuesUpdated(object? sender, ToggleValueUpdatedEventArgs e)
    {
        bool TryGetNewValue(SimVarRegistration variable, double currentValue, out double value)
        {
            if (e.GenericValueStatus.TryGetValue(variable, out var newValue) && currentValue != newValue)
            {
                value = newValue;
                return true;
            }
            value = 0;
            return false;
        }

        bool isUpdated = false;

        if (TryGetNewValue(windDirection, currentWindDirectionValue, out var newDirection))
        {
            currentWindDirectionValue = newDirection;
            isUpdated = true;
        }
        if (TryGetNewValue(heading, currentHeadingValue, out var newHeading))
        {
            currentHeadingValue = newHeading;
            isUpdated = true;
        }
        if (TryGetNewValue(windVelocity, currentWindVelocityValue, out var newVelocity))
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
        simVarManager.RegisterSimValues(windDirection, windVelocity, heading);
    }

    private void DeRegisterValues()
    {
        simVarManager.DeRegisterSimValues(windDirection, windVelocity, heading);
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

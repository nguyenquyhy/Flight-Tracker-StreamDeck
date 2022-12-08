using FlightStreamDeck.Core;

namespace FlightStreamDeck.Logics.Actions;

[StreamDeckAction("tech.flighttracker.streamdeck.wind.direction")]
public class WindAction : BaseAction
{
    private readonly ILogger<WindAction> logger;
    private readonly IFlightConnector flightConnector;
    private readonly IImageLogic imageLogic;

    private TOGGLE_VALUE windDirectionValue = TOGGLE_VALUE.AMBIENT_WIND_DIRECTION;
    private TOGGLE_VALUE windVelocityValue = TOGGLE_VALUE.AMBIENT_WIND_VELOCITY;
    private TOGGLE_VALUE headingValue = TOGGLE_VALUE.PLANE_HEADING_DEGREES_TRUE;

    private double currentWwindDirectionValue = 0;
    private double currentWindVelocityValue = 0;
    private double currentHeadingValue = 0;
    private bool currentRelative = true;

    public WindAction(ILogger<WindAction> logger, IFlightConnector flightConnector, IImageLogic imageLogic)
    {
        this.logger = logger;
        this.flightConnector = flightConnector;
        this.imageLogic = imageLogic;
    }

    protected override async Task OnWillAppear(ActionEventArgs<AppearancePayload> args)
    {
        flightConnector.GenericValuesUpdated += FlightConnector_GenericValuesUpdated;

        RegisterValues();

        await UpdateImage();
    }

    private async void FlightConnector_GenericValuesUpdated(object? sender, ToggleValueUpdatedEventArgs e)
    {
        bool isUpdated = false;

        if (e.GenericValueStatus.ContainsKey((windDirectionValue, null)) && currentWwindDirectionValue != e.GenericValueStatus[(windDirectionValue, null)])
        {
            currentWwindDirectionValue = e.GenericValueStatus[(windDirectionValue, null)];
            isUpdated = true;
        }
        if (e.GenericValueStatus.ContainsKey((headingValue, null)) && currentHeadingValue != e.GenericValueStatus[(headingValue, null)])
        {
            currentHeadingValue = e.GenericValueStatus[(headingValue, null)];
            isUpdated = true;
        }
        if (e.GenericValueStatus.ContainsKey((windVelocityValue, null)) && currentWindVelocityValue != e.GenericValueStatus[(windVelocityValue, null)])
        {
            currentWindVelocityValue = e.GenericValueStatus[(windVelocityValue, null)];
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
        flightConnector.RegisterSimValues((windDirectionValue, null), (windVelocityValue, null), (headingValue, null));
    }

    private void DeRegisterValues()
    {
        flightConnector.DeRegisterSimValues((windDirectionValue, null), (windVelocityValue, null), (headingValue, null));
    }

    protected override Task OnKeyDown(ActionEventArgs<KeyPayload> args)
    {
        currentRelative = !currentRelative;
        _ = UpdateImage();
        return Task.CompletedTask;
    }

    private async Task UpdateImage()
    {
        await SetImageSafeAsync(imageLogic.GetWindImage(currentWwindDirectionValue, currentWindVelocityValue, currentHeadingValue, currentRelative));
    }
}

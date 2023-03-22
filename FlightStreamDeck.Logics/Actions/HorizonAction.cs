using System;

namespace FlightStreamDeck.Logics.Actions;

[StreamDeckAction("tech.flighttracker.streamdeck.artificial.horizon")]
public class HorizonAction : BaseAction
{
    private readonly ILogger<HorizonAction> logger;
    private readonly IFlightConnector flightConnector;
    private readonly IImageLogic imageLogic;
    private readonly SimVarManager simVarManager;
    private readonly SimVarRegistration bank;
    private readonly SimVarRegistration pitch;

    private double currentBankValue = 0;
    private double currentPitchValue = 0;

    public HorizonAction(ILogger<HorizonAction> logger, IFlightConnector flightConnector, IImageLogic imageLogic, SimVarManager simVarManager)
    {
        this.logger = logger;
        this.flightConnector = flightConnector;
        this.imageLogic = imageLogic;
        this.simVarManager = simVarManager;

        this.bank = simVarManager.GetRegistration("PLANE BANK DEGREES") ?? throw new InvalidOperationException("Cannot get registration for AMBIENT WIND DIRECTION");
        this.pitch = simVarManager.GetRegistration("PLANE PITCH DEGREES") ?? throw new InvalidOperationException("Cannot get registration for PLANE PITCH DEGREES");
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

        if (TryGetNewValue(bank, currentBankValue, out var newBank))
        {
            currentBankValue = newBank;
            isUpdated = true;
        }
        if (TryGetNewValue(pitch, currentPitchValue, out var newPitch))
        {
            currentPitchValue = newPitch;
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
        simVarManager.RegisterSimValues(bank, pitch);
    }

    private void DeRegisterValues()
    {
        simVarManager.DeRegisterSimValues(bank, pitch);
    }

    protected override Task OnKeyDown(ActionEventArgs<KeyPayload> args)
    {
        currentBankValue = 0;
        _ = UpdateImage();
        return Task.CompletedTask;
    }

    private async Task UpdateImage()
    {
        await SetImageSafeAsync(imageLogic.GetHorizonImage(currentPitchValue, currentBankValue));
    }
}

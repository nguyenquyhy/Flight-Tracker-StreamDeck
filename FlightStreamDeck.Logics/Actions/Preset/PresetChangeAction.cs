﻿using FlightStreamDeck.Logics.Actions.Preset;
using Newtonsoft.Json.Linq;
using System;
using System.Runtime.InteropServices;
using System.Timers;

namespace FlightStreamDeck.Logics.Actions;

#region Action Registration

[StreamDeckAction("tech.flighttracker.streamdeck.preset.increase")]
public class ValueIncreaseAction : PresetChangeAction
{
    public ValueIncreaseAction(ILogger<ValueIncreaseAction> logger, IFlightConnector flightConnector, IEventRegistrar eventRegistrar, IEventDispatcher eventDispatcher, PresetLogicFactory logicFactory)
        : base(logger, flightConnector, eventRegistrar, eventDispatcher, logicFactory) { }
}
[StreamDeckAction("tech.flighttracker.streamdeck.preset.decrease")]
public class ValueDecreaseAction : PresetChangeAction
{
    public ValueDecreaseAction(ILogger<ValueDecreaseAction> logger, IFlightConnector flightConnector, IEventRegistrar eventRegistrar, IEventDispatcher eventDispatcher, PresetLogicFactory logicFactory)
        : base(logger, flightConnector, eventRegistrar, eventDispatcher, logicFactory) { }
}

#endregion

public class ValueChangeSettings
{
    public string Type { get; set; }
}

public abstract class PresetChangeAction : BaseAction<ValueChangeSettings>
{
    private readonly ILogger logger;
    private readonly IFlightConnector flightConnector;
    private readonly IEventDispatcher eventDispatcher;
    private readonly PresetLogicFactory logicFactory;
    private Timer timer;
    private string? action;
    private bool timerHaveTick = false;
    private uint? originalValue = null;
    private AircraftStatus? status;

    private IPresetToggleLogic? logic;

    public PresetChangeAction(
        ILogger logger,
        IFlightConnector flightConnector,
        IEventRegistrar eventRegistrar,
        IEventDispatcher eventDispatcher,
        PresetLogicFactory logicFactory
    )
    {
        this.logger = logger;
        this.flightConnector = flightConnector;
        this.eventDispatcher = eventDispatcher;
        this.logicFactory = logicFactory;
        timer = new Timer { Interval = 400 };
        timer.Elapsed += Timer_Elapsed;
        eventRegistrar.RegisterEvent(KnownEvents.VOR1_SET.ToString());
        eventRegistrar.RegisterEvent(KnownEvents.VOR2_SET.ToString());
        eventRegistrar.RegisterEvent(KnownEvents.ADF_SET.ToString());
        eventRegistrar.RegisterEvent(KnownEvents.KOHLSMAN_SET.ToString());
    }

    public override Task InitializeSettingsAsync(ValueChangeSettings? settings)
    {
        this.settings = settings;
        this.logic = logicFactory.Create(settings?.Type);
        return Task.CompletedTask;
    }

    private async void Timer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        timerHaveTick = true;
        await ProcessAsync(false);
    }

    private async Task ProcessAsync(bool isUp)
    {
        if (string.IsNullOrEmpty(action) || status == null) return;
        if (isUp && timerHaveTick) return;

        var actions = action.Split('.');

        if (actions.Length < 2)
        {
            return;
        }

        var change = actions[^1];
        var sign = change == "increase" ? 1 : -1;
        var increment = isUp ? 1 : 10;

        var buttonType = settings?.Type;
        if (string.IsNullOrWhiteSpace(buttonType))
        {
            return;
        }

        if (originalValue == null) originalValue = buttonType switch
        {
            PresetFunctions.Heading => 0,
            PresetFunctions.Altitude => 0,
            PresetFunctions.VerticalSpeed => 0,
            PresetFunctions.AirSpeed => 0,
            PresetFunctions.VerticalSpeedAirSpeed => status.IsApFlcOn ? (uint)status.ApAirspeed : (uint)status.ApVs,
            PresetFunctions.VOR1 => (uint)status.Nav1OBS,
            PresetFunctions.VOR2 => (uint)status.Nav2OBS,
            PresetFunctions.ADF => (uint)status.ADFCard,
            PresetFunctions.QNH => (uint)status.QNHMbar,

            _ => throw new NotImplementedException($"Value type: {buttonType}")
        };

        try
        {
            switch (buttonType)
            {
                case PresetFunctions.Heading:
                case PresetFunctions.VerticalSpeed:
                case PresetFunctions.Altitude:
                case PresetFunctions.AirSpeed:
                case PresetFunctions.VOR1:
                case PresetFunctions.VOR2:
                    if (logic is IPresetValueLogic valueLogic)
                    {
                        valueLogic.ChangeValue(status, sign, increment);
                    }
                    else
                    {
                        await ShowAlertAsync();
                    }
                    break;

                case PresetFunctions.VerticalSpeedAirSpeed:
                    if (status.IsApFlcOn)
                    {
                        ChangeAirSpeed(originalValue.Value, sign, increment);
                    }
                    else
                    {
                        ChangeVerticalSpeed(originalValue.Value, sign);
                    }
                    break;
                case PresetFunctions.QNH:
                    double newValue = (double)originalValue + (sign * increment * 50);  // Value is in nanobar, increment per 50 nanobar (0.5 mbar)
                    flightConnector.QNHSet((uint)(newValue * .16));                     // Custom factor of 16, because SimConnect ;)
                    break;
                case PresetFunctions.ADF:
                    ChangeSphericalValue(originalValue.Value, sign, increment, KnownEvents.ADF_SET);
                    break;

            }
        }
        catch (COMException ex)
        {
            logger.LogError(ex, "Cannot set value!");
            await ShowAlertAsync();
        }
    }

    private void FlightConnector_AircraftStatusUpdated(object? sender, AircraftStatusUpdatedEventArgs e)
    {
        status = e.AircraftStatus;
    }

    protected override Task OnKeyDown(ActionEventArgs<KeyPayload> args)
    {
        action = args.Action;
        timerHaveTick = false;
        timer.Start();
        return Task.CompletedTask;
    }

    protected override async Task OnKeyUp(ActionEventArgs<KeyPayload> args)
    {
        timer.Stop();
        await ProcessAsync(true);
        action = null;
        originalValue = null;
        timerHaveTick = false;
    }

    protected override async Task OnWillAppear(ActionEventArgs<AppearancePayload> args)
    {
        await InitializeSettingsAsync(args.Payload.GetSettings<ValueChangeSettings>());
        status = null;
        this.flightConnector.AircraftStatusUpdated += FlightConnector_AircraftStatusUpdated;
    }

    protected override Task OnWillDisappear(ActionEventArgs<AppearancePayload> args)
    {
        status = null;
        this.flightConnector.AircraftStatusUpdated -= FlightConnector_AircraftStatusUpdated;
        return Task.CompletedTask;
    }

    protected override async Task OnSendToPlugin(ActionEventArgs<JObject> args)
    {
        await InitializeSettingsAsync(args.Payload.ToObject<ValueChangeSettings>());
        this.logic = logicFactory.Create(settings?.Type);
    }

    private void ChangeVerticalSpeed(uint value, int sign)
    {
        value = (uint)(value + 100 * sign);
        originalValue = value;
        flightConnector.ApVsSet(value);
    }

    private void ChangeAirSpeed(uint value, int sign, int increment)
    {
        value = (uint)Math.Max(0, value + increment * sign);
        originalValue = value;
        flightConnector.ApAirSpeedSet(value);
    }

    private void ChangeSphericalValue(uint value, int sign, int increment, KnownEvents evt)
    {
        value = value.IncreaseSpherical(increment * sign);
        originalValue = value;
        eventDispatcher.Trigger(evt.ToString(), value);
    }
}

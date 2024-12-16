using FlightStreamDeck.Logics.Actions.Preset;
using SharpDeck.Layouts;
using System;

namespace FlightStreamDeck.Logics.Actions;

[StreamDeckAction("tech.flighttracker.streamdeck.preset.dial")]
public class PresetDialAction : PresetBaseAction
{
    public PresetDialAction(
        ILogger<PresetDialAction> logger, 
        IFlightConnector flightConnector, 
        IImageLogic imageLogic,
        IEventRegistrar eventRegistrar, 
        PresetLogicFactory logicFactory,
        RegistrationParameters registrationParameters
    ) : base(logger, flightConnector, imageLogic, logicFactory, registrationParameters)
    {
        eventRegistrar.RegisterEvent(KnownEvents.VOR1_SET.ToString());
        eventRegistrar.RegisterEvent(KnownEvents.VOR2_SET.ToString());
    }

    protected override async Task OnTouchTap(ActionEventArgs<TouchTapPayload> args)
    {
        if (!args.Payload.Hold)
        {
            await ToggleAsync();
        }
        else
        {
            await SyncAsync();
        }
    }

    protected override async Task OnDialShortPress(ActionEventArgs<DialPayload> args)
    {
        if (logic != null && status != null)
        {
            logic.Toggle(status);
        }
        else
        {
            await ShowAlertAsync();
        }
    }

    protected override async Task OnDialLongPress(ActionEventArgs<DialPayload> args)
    {
        if (status != null && logic is IPresetValueLogic valueLogic)
        {
            valueLogic.Sync(status);
        }
        else
        {
            await ShowAlertAsync();
        }
    }

    protected override async Task OnDialRotate(ActionEventArgs<DialRotatePayload> args)
    {
        if (status != null && logic is IPresetValueLogic valueLogic)
        {
            valueLogic.ChangeValue(status, args.Payload.Ticks, 1);
        }
        else
        {
            await ShowAlertAsync();
        }
    }

    protected override async Task UpdateImageAsync()
    {
        await base.UpdateImageAsync();

        var currentStatus = status;
        if (currentStatus != null && settings != null && logic is IPresetValueLogic valueLogic)
        {
            var value = valueLogic.GetValue(currentStatus);

            byte[]? imageOnBytes = settings.ImageOn_base64 != null ? Convert.FromBase64String(settings.ImageOn_base64) : null;
            byte[]? imageOffBytes = settings.ImageOff_base64 != null ? Convert.FromBase64String(settings.ImageOff_base64) : null;

            var active = logic.GetActive(currentStatus);
            var image = imageLogic.GetImage(
                "",
                active,
                imageOnFilePath: settings.ImageOn, imageOnBytes: imageOnBytes,
                imageOffFilePath: settings.ImageOff, imageOffBytes: imageOffBytes
            );

            var min = GetMin(settings.Type);
            var max = GetMax(settings.Type);
            var indicator = Math.Clamp(((int)(value ?? 0) - min) * 100 / (max - min), 0, 100);
            var showValue = active || GetShowValueWhenInactive(settings.Type);
            await SetFeedbackSafeAsync(new LayoutB2
            {
                Title = GetHeader(settings.Type),
                Value = new Text
                {
                    Opacity = showValue ? 1 : 0,
                    Value = value?.ToString("#,0") + GetValueUnit(settings.Type),
                },
                Icon = image,
                Indicator = value == null ? null : new GBar
                {
                    Opacity = showValue ? 1 : 0,
                    Value = indicator,
                    BackgroundColor = GetBackgroundColor(settings.Type),
                }
            });
        }
    }

    private string GetHeader(string type) =>
        type switch
        {
            PresetFunctions.VerticalSpeed => "Vertical Speed",
            _ => type.ToString()
        };

    private bool GetShowValueWhenInactive(string type) =>
        type switch
        {
            PresetFunctions.Heading => true,
            PresetFunctions.Altitude => true,
            PresetFunctions.VerticalSpeed => false,
            PresetFunctions.FLC => false,
            PresetFunctions.VOR1 => true,
            PresetFunctions.VOR2 => true,
            _ => false
        };

    private string GetValueUnit(string type) =>
        type switch
        {
            PresetFunctions.Heading => "°",
            PresetFunctions.Altitude => " ft",
            PresetFunctions.VerticalSpeed => " ft/m",
            PresetFunctions.FLC => " kt",
            PresetFunctions.VOR1 => "°",
            PresetFunctions.VOR2 => "°",
            _ => ""
        };

    private int GetMax(string type) =>
        type switch
        {
            PresetFunctions.Heading => 360,
            PresetFunctions.Altitude => 50000,
            PresetFunctions.VerticalSpeed => 2000,
            PresetFunctions.FLC => 400,
            PresetFunctions.VOR1 => 360,
            PresetFunctions.VOR2 => 360,
            _ => 1
        };

    private int GetMin(string type) =>
        type switch
        {
            PresetFunctions.Heading => 0,
            PresetFunctions.Altitude => 0,
            PresetFunctions.VerticalSpeed => -2000,
            PresetFunctions.FLC => 0,
            PresetFunctions.VOR1 => 0,
            PresetFunctions.VOR2 => 0,
            _ => 0
        };

    private const string ColorDegreeMarking = "0:#ffffff,0.05:#999999,0.225:#999999,0.25:#ffffff,0.275:#999999,0.475:#999999,0.5:#ffffff,0.525:#999999,0.725:#999999,0.75:#ffffff,0.775:#999999,0.95:#999999,1:#ffffff";

    private string GetBackgroundColor(string type) =>
        type switch
        {
            PresetFunctions.Heading => ColorDegreeMarking,
            PresetFunctions.Altitude => "0:#ffffff,1:#87ceeb",
            PresetFunctions.VerticalSpeed => "0.4:#a52a2a,0.5:#ffffff,0.6:#87ceeb",
            PresetFunctions.VOR1 => ColorDegreeMarking,
            PresetFunctions.VOR2 => ColorDegreeMarking,
            _ => "0:#ffffff,1:#ffffff"
        };
}

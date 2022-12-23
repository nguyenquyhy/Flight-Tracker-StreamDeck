using FlightStreamDeck.Logics.Actions.Preset;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Timers;

namespace FlightStreamDeck.Logics.Actions;

/// <summary>
/// Note: We need to fix the JSON property names to avoid conversion to camel case
/// </summary>
public class PresetToggleSettings
{
    [JsonProperty(nameof(Type))]
    public string Type { get; set; }
    [JsonProperty(nameof(HideHeader))]
    public bool HideHeader { get; set; }
    [JsonProperty(nameof(ImageOn))]
    public string ImageOn { get; set; }
    [JsonProperty(nameof(ImageOn_base64))]
    public string? ImageOn_base64 { get; set; }
    [JsonProperty(nameof(ImageOff))]
    public string ImageOff { get; set; }
    [JsonProperty(nameof(ImageOff_base64))]
    public string? ImageOff_base64 { get; set; }
}

[StreamDeckAction("tech.flighttracker.streamdeck.preset.toggle")]
public class PresetToggleAction : PresetBaseAction
{
    public PresetToggleAction(ILogger<PresetToggleAction> logger, IFlightConnector flightConnector, IImageLogic imageLogic, PresetLogicFactory logicFactory)
        : base(logger, flightConnector, imageLogic, logicFactory)
    {
    }
}

public abstract class PresetBaseAction : BaseAction<PresetToggleSettings>, EmbedLinkLogic.IAction
{
    protected readonly ILogger logger;
    protected readonly IFlightConnector flightConnector;
    protected readonly IImageLogic imageLogic;
    private readonly PresetLogicFactory logicFactory;
    private readonly EmbedLinkLogic embedLinkLogic;

    protected AircraftStatus? status = null;

    protected IPresetToggleLogic? logic = null;

    public PresetBaseAction(ILogger logger, IFlightConnector flightConnector, IImageLogic imageLogic, PresetLogicFactory logicFactory)
    {
        this.logger = logger;
        this.flightConnector = flightConnector;
        this.imageLogic = imageLogic;
        this.logicFactory = logicFactory;
        this.embedLinkLogic = new EmbedLinkLogic(this);
    }

    private async void FlightConnector_AircraftStatusUpdated(object? sender, AircraftStatusUpdatedEventArgs e)
    {
        if (StreamDeck == null || logic == null) return;

        var lastStatus = status;
        status = e.AircraftStatus;
        if (logic.IsChanged(lastStatus, e.AircraftStatus))
        {
            await UpdateImageAsync();
        }
    }

    protected override async Task OnWillAppear(ActionEventArgs<AppearancePayload> args)
    {
        var settings = args.Payload.GetSettings<PresetToggleSettings>();
        await InitializeSettingsAsync(settings);

        status = null;
        this.flightConnector.AircraftStatusUpdated += FlightConnector_AircraftStatusUpdated;
    }

    protected override Task OnWillDisappear(ActionEventArgs<AppearancePayload> args)
    {
        this.flightConnector.AircraftStatusUpdated -= FlightConnector_AircraftStatusUpdated;
        status = null;

        return Task.CompletedTask;
    }

    protected override async Task OnSendToPlugin(ActionEventArgs<JObject> args)
    {
        if (args.Payload.TryGetValue("convertToEmbed", out JToken? fileKeyObject))
        {
            var fileKey = fileKeyObject.Value<string>();
            await embedLinkLogic.ConvertLinkToEmbedAsync(fileKey);
        }
        else if (args.Payload.TryGetValue("convertToLink", out fileKeyObject))
        {
            var fileKey = fileKeyObject.Value<string>();
            await embedLinkLogic.ConvertEmbedToLinkAsync(fileKey);
        }
        else
        {
            await InitializeSettingsAsync(args.Payload.ToObject<PresetToggleSettings>());
        }
        await UpdateImageAsync();
    }

    protected override async Task OnKeyPress(ActionEventArgs<KeyPayload> args)
    {
        await ToggleAsync();
    }

    protected override async Task OnKeyLongPress(ActionEventArgs<KeyPayload> args)
    {
        await SyncAsync();
    }

    public override Task InitializeSettingsAsync(PresetToggleSettings? settings)
    {
        this.settings = settings;
        this.logic = logicFactory.Create(settings?.Type);
        LongKeyPressInterval = (logic is IPresetValueLogic) ? TimeSpan.FromSeconds(1) : TimeSpan.Zero;
        return Task.CompletedTask;
    }

    protected async Task ToggleAsync()
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

    protected async Task SyncAsync()
    {
        if (status != null && logic is IPresetValueLogic valueLogic)
        {
            valueLogic.Sync(status);
            await ShowOkAsync();
        }
        else
        {
            await ShowAlertAsync();
        }
    }

    protected string GetImageText(string type) =>
        type switch
        {
            PresetFunctions.Avionics => "AV",
            PresetFunctions.ApMaster => "AP",
            PresetFunctions.Heading => "HDG",
            PresetFunctions.Nav => "NAV",
            PresetFunctions.Altitude => "ALT",
            PresetFunctions.VerticalSpeed => "VS",
            PresetFunctions.FLC => "FLC",
            PresetFunctions.Approach => "APR",
            _ => ""
        };

    protected virtual async Task UpdateImageAsync()
    {
        var currentStatus = status;
        if (currentStatus != null && settings != null && logic != null)
        {
            byte[]? imageOnBytes = settings.ImageOn_base64 != null ? Convert.FromBase64String(settings.ImageOn_base64) : null;
            byte[]? imageOffBytes = settings.ImageOff_base64 != null ? Convert.FromBase64String(settings.ImageOff_base64) : null;

            var image = imageLogic.GetImage(
                settings.HideHeader ? "" : GetImageText(settings.Type),
                logic.GetActive(currentStatus),
                logic is IPresetValueLogic valueLogic ? valueLogic.GetValue(currentStatus)?.ToString() : null,
                imageOnFilePath: settings.ImageOn, imageOnBytes: imageOnBytes,
                imageOffFilePath: settings.ImageOff, imageOffBytes: imageOffBytes
            );

            if (image != null)
            {
                await SetImageSafeAsync(image);
            }
        }
    }

    public string? GetImagePath(string fileKey) => fileKey switch
    {
        "ImageOn" => settings?.ImageOn,
        "ImageOff" => settings?.ImageOff,
        _ => throw new ArgumentOutOfRangeException(nameof(fileKey), $"'{fileKey}' is invalid.")
    };

    public string? GetImageBase64(string fileKey) => fileKey switch
    {
        "ImageOn" => settings?.ImageOn_base64,
        "ImageOff" => settings?.ImageOff_base64,
        _ => throw new ArgumentOutOfRangeException(nameof(fileKey), $"'{fileKey}' is invalid.")
    };

    public void SetImagePath(string fileKey, string path)
    {
        if (settings != null)
        {
            switch (fileKey)
            {
                case "ImageOn": settings.ImageOn = path; break;
                case "ImageOff": settings.ImageOff = path; break;
                default: throw new ArgumentOutOfRangeException(nameof(fileKey), $"'{fileKey}' is invalid.");
            }
        }
    }

    public void SetImageBase64(string fileKey, string? base64)
    {
        if (settings != null)
        {
            switch (fileKey)
            {
                case "ImageOn": settings.ImageOn_base64 = base64; break;
                case "ImageOff": settings.ImageOff_base64 = base64; break;
                default: throw new ArgumentOutOfRangeException(nameof(fileKey), $"'{fileKey}' is invalid.");
            }
        }
    }
}

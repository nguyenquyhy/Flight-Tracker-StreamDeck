using FlightStreamDeck.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace FlightStreamDeck.Logics.Actions;

/// <summary>
/// Note: We need to fix the JSON property names to avoid conversion to camel case
/// </summary>
public class GenericToggleSettings
{
    [JsonProperty(nameof(Header))]
    public string Header { get; set; }

    [JsonProperty(nameof(ToggleValue))]
    public string ToggleValue { get; set; }
    [JsonProperty(nameof(ToggleValueData))]
    public string ToggleValueData { get; set; }

    [JsonProperty(nameof(HoldValue))]
    public string HoldValue { get; set; }
    [JsonProperty(nameof(HoldValueData))]
    public string HoldValueData { get; set; }
    [JsonProperty(nameof(HoldValueRepeat))]
    public bool HoldValueRepeat { get; set; }
    [JsonProperty(nameof(HoldValueSuppressToggle))]
    public bool HoldValueSuppressToggle { get; set; }

    [JsonProperty(nameof(FeedbackValue))]
    public string FeedbackValue { get; set; }
    [JsonProperty(nameof(DisplayValue))]
    public string DisplayValue { get; set; }
    [JsonProperty(nameof(DisplayValueUnit))]
    public string DisplayValueUnit { get; set; }
    [JsonProperty(nameof(DisplayValuePrecision))]
    public string DisplayValuePrecision { get; set; }
    [JsonProperty(nameof(ImageOn))]
    public string ImageOn { get; set; }
    [JsonProperty(nameof(ImageOn_base64))]
    public string? ImageOn_base64 { get; set; }
    [JsonProperty(nameof(ImageOff))]
    public string ImageOff { get; set; }
    [JsonProperty(nameof(ImageOff_base64))]
    public string? ImageOff_base64 { get; set; }
}

[StreamDeckAction("tech.flighttracker.streamdeck.generic.toggle")]
public class GenericToggleAction : BaseAction<GenericToggleSettings>, EmbedLinkLogic.IAction
{
    private readonly ILogger<GenericToggleAction> logger;
    private readonly IFlightConnector flightConnector;
    private readonly IImageLogic imageLogic;
    private readonly IEvaluator evaluator;
    private readonly IEventRegistrar eventRegistrar;
    private readonly IEventDispatcher eventDispatcher;
    private readonly EnumConverter enumConverter;
    private readonly EmbedLinkLogic embedLinkLogic;
    private Timer? timer = null;

    private string? toggleEvent = null;
    private uint? toggleEventDataUInt = null;
    private TOGGLE_VALUE? toggleEventDataVariable = null;
    private double? toggleEventDataVariableValue = null;
    private string? holdEvent = null;
    private uint? holdEventDataUInt = null;
    private TOGGLE_VALUE? holdEventDataVariable = null;
    private double? holdEventDataVariableValue = null;

    private IEnumerable<TOGGLE_VALUE> feedbackVariables = new List<TOGGLE_VALUE>();
    private IExpression? expression;
    private TOGGLE_VALUE? displayValue = null;

    private string? customUnit = null;
    private int? customDecimals = null;

    private double? currentValue = null;
    private string? currentValueTime = null;
    private bool currentStatus = false;

    private bool holdEventTriggerred = false;

    public GenericToggleAction(
        ILogger<GenericToggleAction> logger,
        IFlightConnector flightConnector,
        IImageLogic imageLogic,
        IEvaluator evaluator,
        IEventRegistrar eventRegistrar,
        IEventDispatcher eventDispatcher,
        EnumConverter enumConverter)
    {
        this.logger = logger;
        this.flightConnector = flightConnector;
        this.imageLogic = imageLogic;
        this.evaluator = evaluator;
        this.eventRegistrar = eventRegistrar;
        this.eventDispatcher = eventDispatcher;
        this.enumConverter = enumConverter;
        this.embedLinkLogic = new EmbedLinkLogic(this);
    }

    protected override async Task OnWillAppear(ActionEventArgs<AppearancePayload> args)
    {
        var settings = args.Payload.GetSettings<GenericToggleSettings>();
        await InitializeSettingsAsync(settings);

        flightConnector.GenericValuesUpdated += FlightConnector_GenericValuesUpdated;

        await UpdateImage();
    }

    public override Task InitializeSettingsAsync(GenericToggleSettings? settings)
    {
        this.settings = settings;
        if (settings == null) return Task.CompletedTask;

        var newToggleEvent = settings.ToggleValue;
        (var newToggleEventDataUInt, var newToggleEventDataVariable) = enumConverter.GetUIntOrVariable(settings.ToggleValueData);
        var newHoldEvent = settings.HoldValue;
        (var newHoldEventDataUInt, var newHoldEventDataVariable) = enumConverter.GetUIntOrVariable(settings.HoldValueData);

        (var newFeedbackVariables, var newExpression) = evaluator.Parse(settings.FeedbackValue);
        TOGGLE_VALUE? newDisplayValue = enumConverter.GetVariableEnum(settings.DisplayValue);

        if (int.TryParse(settings.DisplayValuePrecision, out int decimals))
        {
            customDecimals = decimals;
        }
        var newUnit = settings.DisplayValueUnit?.Trim();
        if (string.IsNullOrWhiteSpace(newUnit)) newUnit = null;

        if (!newFeedbackVariables.SequenceEqual(feedbackVariables) || newDisplayValue != displayValue
            || newUnit != customUnit
            || newToggleEventDataVariable != toggleEventDataVariable
            || newHoldEventDataVariable != holdEventDataVariable
            )
        {
            DeRegisterValues();
        }

        toggleEvent = newToggleEvent;
        toggleEventDataUInt = newToggleEventDataUInt;
        toggleEventDataVariable = newToggleEventDataVariable;
        holdEvent = newHoldEvent;
        holdEventDataUInt = newHoldEventDataUInt;
        holdEventDataVariable = newHoldEventDataVariable;
        feedbackVariables = newFeedbackVariables;
        expression = newExpression;
        displayValue = newDisplayValue;
        customUnit = newUnit;

        RegisterValues();

        return Task.CompletedTask;
    }

    private async void FlightConnector_GenericValuesUpdated(object? sender, ToggleValueUpdatedEventArgs e)
    {
        if (StreamDeck == null) return;

        var valuesWithDefaultUnits = e.GenericValueStatus.Where(o => o.Key.unit == null).ToDictionary(o => o.Key.variable, o => o.Value);
        var newStatus = expression != null && evaluator.Evaluate(valuesWithDefaultUnits, expression);
        var isUpdated = newStatus != currentStatus;
        currentStatus = newStatus;

        if (displayValue.HasValue && e.GenericValueStatus.ContainsKey((displayValue.Value, customUnit)))
        {
            var newValue = e.GenericValueStatus[(displayValue.Value, customUnit)];
            isUpdated |= newValue != currentValue;
            currentValue = newValue;

            if (displayValue.Value == TOGGLE_VALUE.ZULU_TIME
                || displayValue.Value == TOGGLE_VALUE.LOCAL_TIME)
            {
                string hours = Math.Floor(newValue / 3600).ToString().PadLeft(2, '0');
                newValue = newValue % 3600;

                string minutes = Math.Floor(newValue / 60).ToString().PadLeft(2, '0');
                newValue = newValue % 60;

                string seconds = Math.Floor(newValue).ToString().PadLeft(2, '0');

                switch (customDecimals)
                {
                    case 0: //HH:MM:SS
                        currentValueTime = $"{hours}:{minutes}:{seconds}{(displayValue.Value == TOGGLE_VALUE.ZULU_TIME ? "Z" : String.Empty)}";
                        currentValue = e.GenericValueStatus[(displayValue.Value, customUnit)];
                        break;
                    case 1: //HH:MM
                        currentValueTime = $"{hours}:{minutes}{(displayValue.Value == TOGGLE_VALUE.ZULU_TIME ? "Z" : String.Empty)}";
                        currentValue = e.GenericValueStatus[(displayValue.Value, customUnit)];
                        break;
                    default:
                        currentValueTime = string.Empty;
                        currentValue = e.GenericValueStatus[(displayValue.Value, customUnit)];
                        break;
                }
            }
        }

        if (toggleEventDataVariable.HasValue && e.GenericValueStatus.ContainsKey((toggleEventDataVariable.Value, null)))
        {
            toggleEventDataVariableValue = e.GenericValueStatus[(toggleEventDataVariable.Value, null)];
        }

        if (holdEventDataVariable.HasValue && e.GenericValueStatus.ContainsKey((holdEventDataVariable.Value, null)))
        {
            holdEventDataVariableValue = e.GenericValueStatus[(holdEventDataVariable.Value, null)];
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
            await InitializeSettingsAsync(args.Payload.ToObject<GenericToggleSettings>());
        }
        await UpdateImage();
    }

    private void RegisterValues()
    {
        eventRegistrar.RegisterEvent(toggleEvent);
        eventRegistrar.RegisterEvent(holdEvent);

        var values = new List<(TOGGLE_VALUE variables, string? unit)>();
        foreach (var feedbackVariable in feedbackVariables) values.Add((feedbackVariable, null));
        if (displayValue.HasValue) values.Add((displayValue.Value, customUnit));
        if (toggleEventDataVariable.HasValue) values.Add((toggleEventDataVariable.Value, null));
        if (holdEventDataVariable.HasValue) values.Add((holdEventDataVariable.Value, null));

        if (values.Count > 0)
        {
            flightConnector.RegisterSimValues(values.ToArray());
        }
    }

    private void DeRegisterValues()
    {
        var values = new List<(TOGGLE_VALUE variables, string? unit)>();
        foreach (var feedbackVariable in feedbackVariables) values.Add((feedbackVariable, null));
        if (displayValue.HasValue) values.Add((displayValue.Value, customUnit));
        if (toggleEventDataVariable.HasValue) values.Add((toggleEventDataVariable.Value, null));
        if (holdEventDataVariable.HasValue) values.Add((holdEventDataVariable.Value, null));

        if (values.Count > 0)
        {
            flightConnector.DeRegisterSimValues(values.ToArray());
        }

        currentValue = null;
        currentValueTime = string.Empty;
        toggleEventDataVariableValue = null;
        holdEventDataVariableValue = null;
    }

    protected override async Task OnKeyDown(ActionEventArgs<KeyPayload> args)
    {
        if (settings != null)
        {
            holdEventTriggerred = false;

            if (!eventDispatcher.IsValid(holdEvent) || !settings.HoldValueSuppressToggle)
            {
                await TriggerToggleEventAsync();
            }

            if (eventDispatcher.IsValid(holdEvent))
            {
                timer = new Timer { Interval = settings.HoldValueRepeat ? 400 : 1000 };
                timer.Elapsed += Timer_Elapsed;
                timer.Start();
            }
        }
    }

    protected override async Task OnKeyUp(ActionEventArgs<KeyPayload> args)
    {
        if (settings != null)
        {
            if (settings.HoldValueSuppressToggle && !holdEventTriggerred)
            {
                await TriggerToggleEventAsync();
            }

            var localTimer = timer;
            if (localTimer != null)
            {
                localTimer.Elapsed -= Timer_Elapsed;
                localTimer.Stop();
                localTimer = null;
            }
        }
    }

    private void Timer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        if (eventDispatcher.Trigger(holdEvent, CalculateEventParam(holdEventDataVariable, holdEventDataVariableValue, holdEventDataUInt)))
        {
            holdEventTriggerred = true;

            if (settings?.HoldValueRepeat != true && timer != null)
            {
                timer?.Stop();
                timer = null;
            }
        }
    }

    private async Task<bool> TriggerToggleEventAsync()
    {
        var result = eventDispatcher.Trigger(toggleEvent, CalculateEventParam(toggleEventDataVariable, toggleEventDataVariableValue, toggleEventDataUInt));

        if (!result)
        {
            await ShowAlertAsync();
        }

        return result;
    }

    private uint CalculateEventParam(TOGGLE_VALUE? variable, double? variableValue, uint? inputValue)
    {
        if (variable is not null && variableValue.HasValue)
        {
            var rounded = Math.Round(variableValue.Value);// - 360;
            return rounded < 0 ? unchecked((uint)(int)rounded) : (uint)rounded;
        }
        return inputValue ?? 0;
    }

    private async Task UpdateImage()
    {
        if (settings != null)
        {
            var imageOnBytes = settings.ImageOn_base64 != null ? Convert.FromBase64String(settings.ImageOn_base64) : null;
            var imageOffBytes = settings.ImageOff_base64 != null ? Convert.FromBase64String(settings.ImageOff_base64) : null;

            var valueToShow = !string.IsNullOrEmpty(currentValueTime) ?
                currentValueTime :
                (displayValue.HasValue && currentValue.HasValue) ? currentValue.Value.ToString("F" + EventValueLibrary.GetDecimals(displayValue.Value, customDecimals)) : "";

            var image = imageLogic.GetImage(settings.Header, currentStatus,
                value: valueToShow,
                imageOnFilePath: settings.ImageOn, imageOnBytes: imageOnBytes,
                imageOffFilePath: settings.ImageOff, imageOffBytes: imageOffBytes);

            await SetImageSafeAsync(image);
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

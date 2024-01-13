using FlightStreamDeck.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Timers;

namespace FlightStreamDeck.Logics.Actions;

/// <summary>
/// Note: We need to fix the JSON property names to avoid conversion to camel case
/// </summary>
public class GenericToggleSettings4
{
    [JsonProperty(nameof(Header))]
    public string Header { get; set; }

    [JsonProperty(nameof(ToggleValue))]
    public string ToggleValue { get; set; }
    [JsonProperty(nameof(ToggleValueData))]
    public string ToggleValueData { get; set; }

    [JsonProperty(nameof(ToggleValue2))]
    public string ToggleValue2 { get; set; }
    [JsonProperty(nameof(ToggleValueData2))]
    public string ToggleValueData2 { get; set; }

    [JsonProperty(nameof(ToggleValue3))]
    public string ToggleValue3 { get; set; }
    [JsonProperty(nameof(ToggleValueData3))]
    public string ToggleValueData3 { get; set; }

    [JsonProperty(nameof(ToggleValue4))]
    public string ToggleValue4 { get; set; }
    [JsonProperty(nameof(ToggleValueData4))]
    public string ToggleValueData4 { get; set; }

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
    [JsonProperty(nameof(FontSize))]
    public string? FontSize { get; set; }
}


[StreamDeckAction("tech.flighttracker.streamdeck.generic.toggle4")]
public class GenericToggleAction4 : BaseAction<GenericToggleSettings4>, EmbedLinkLogic.IAction
{
    private readonly ILogger<GenericToggleAction4> logger;
    private readonly IFlightConnector flightConnector;
    private readonly IImageLogic imageLogic;
    private readonly IEvaluator evaluator;
    private readonly IEventRegistrar eventRegistrar;
    private readonly IEventDispatcher eventDispatcher;
    private readonly SimVarManager simVarManager;
    private readonly EmbedLinkLogic embedLinkLogic;
    private Timer? timer = null;

    private string? toggleEvent = null;
    private string? toggleEvent2 = null;
    private string? toggleEvent3 = null;
    private string? toggleEvent4 = null;
    private uint? toggleEventDataUInt = null;
    private uint? toggleEventDataUInt2 = null;
    private uint? toggleEventDataUInt3 = null;
    private uint? toggleEventDataUInt4 = null;
    private SimVarRegistration? toggleEventDataVariable = null;
    private SimVarRegistration? toggleEventDataVariable2= null;
    private SimVarRegistration? toggleEventDataVariable3 = null;
    private SimVarRegistration? toggleEventDataVariable4 = null;
    private double? toggleEventDataVariableValue = null;
    private double? toggleEventDataVariableValue2 = null;
    private double? toggleEventDataVariableValue3 = null;
    private double? toggleEventDataVariableValue4 = null;

    private string? holdEvent = null;
    private uint? holdEventDataUInt = null;
    private SimVarRegistration? holdEventDataVariable = null;
    private double? holdEventDataVariableValue = null;

    private IEnumerable<SimVarRegistration> feedbackVariables = new List<SimVarRegistration>();
    private IExpression? expression;
    private SimVarRegistration? displayVariable = null;

    private int? customDecimals = null;

    private double? currentValue = null;
    private string? currentValueTime = null;
    private bool currentStatus = false;

    private bool holdEventTriggerred = false;

    public GenericToggleAction4(
        ILogger<GenericToggleAction4> logger,
        IFlightConnector flightConnector,
        IImageLogic imageLogic,
        IEvaluator evaluator,
        IEventRegistrar eventRegistrar,
        IEventDispatcher eventDispatcher,
        SimVarManager simVarManager)
    {
        this.logger = logger;
        this.flightConnector = flightConnector;
        this.imageLogic = imageLogic;
        this.evaluator = evaluator;
        this.eventRegistrar = eventRegistrar;
        this.eventDispatcher = eventDispatcher;
        this.simVarManager = simVarManager;
        this.embedLinkLogic = new EmbedLinkLogic(this);
    }

    protected override async Task OnWillAppear(ActionEventArgs<AppearancePayload> args)
    {
        var settings = args.Payload.GetSettings<GenericToggleSettings4>();
        await InitializeSettingsAsync(settings);

        flightConnector.GenericValuesUpdated += FlightConnector_GenericValuesUpdated;

        await UpdateImage();
    }

    public override Task InitializeSettingsAsync(GenericToggleSettings4? settings)
    {
        this.settings = settings;
        if (settings == null) return Task.CompletedTask;

        var newToggleEvent = settings.ToggleValue;
        (var newToggleEventDataUInt, var newToggleEventDataVariable) = GetUIntOrVariable(settings.ToggleValueData);

        var newToggleEvent2 = settings.ToggleValue2;
        (var newToggleEventDataUInt2, var newToggleEventDataVariable2) = GetUIntOrVariable(settings.ToggleValueData2);

        var newToggleEvent3 = settings.ToggleValue3;
        (var newToggleEventDataUInt3, var newToggleEventDataVariable3) = GetUIntOrVariable(settings.ToggleValueData3);

        var newToggleEvent4 = settings.ToggleValue4;
        (var newToggleEventDataUInt4, var newToggleEventDataVariable4) = GetUIntOrVariable(settings.ToggleValueData4);

        var newHoldEvent = settings.HoldValue;
        (var newHoldEventDataUInt, var newHoldEventDataVariable) = GetUIntOrVariable(settings.HoldValueData);

        (var newFeedbackVariables, var newExpression) = evaluator.Parse(settings.FeedbackValue);
        var newDisplayValue = simVarManager.GetRegistration(settings.DisplayValue, settings.DisplayValueUnit);

        if (int.TryParse(settings.DisplayValuePrecision, out int decimals))
        {
            customDecimals = decimals;
        }

        if (!newFeedbackVariables.SequenceEqual(feedbackVariables)
            || newDisplayValue != displayVariable
            || newToggleEventDataVariable != toggleEventDataVariable
            || newHoldEventDataVariable != holdEventDataVariable
            )
        {
            DeRegisterValues();
        }

        toggleEvent = newToggleEvent;
        toggleEvent2 = newToggleEvent2;
        toggleEvent3 = newToggleEvent3;
        toggleEvent4 = newToggleEvent4;

        toggleEventDataUInt = newToggleEventDataUInt;
        toggleEventDataVariable = newToggleEventDataVariable;

        toggleEventDataUInt2 = newToggleEventDataUInt2;
        toggleEventDataVariable2 = newToggleEventDataVariable2;

        toggleEventDataUInt3 = newToggleEventDataUInt3;
        toggleEventDataVariable3 = newToggleEventDataVariable3;

        toggleEventDataUInt4 = newToggleEventDataUInt4;
        toggleEventDataVariable4 = newToggleEventDataVariable4;


        holdEvent = newHoldEvent;
        holdEventDataUInt = newHoldEventDataUInt;
        holdEventDataVariable = newHoldEventDataVariable;
        feedbackVariables = newFeedbackVariables;
        expression = newExpression;
        displayVariable = newDisplayValue;

        RegisterValues();

        return Task.CompletedTask;
    }

    private (uint? number, SimVarRegistration? variable) GetUIntOrVariable(string? value)
    {
        if (uint.TryParse(value, out var result))
        {
            return (result, null);
        }
        else if (int.TryParse(value, out var intResult))
        {
            return (unchecked((uint)intResult), null);
        }
        else if (value != null)
        {
            var variable = simVarManager.GetRegistration(value, null);
            return (null, variable);
        }
        else
        {
            return (null, null);
        }
    }

    private async void FlightConnector_GenericValuesUpdated(object? sender, ToggleValueUpdatedEventArgs e)
    {
        if (StreamDeck == null) return;

        var newStatus = expression != null && expression.Evaluate(e.GenericValueStatus);
        var isUpdated = newStatus != currentStatus;
        currentStatus = newStatus;

        bool TryGetValue([NotNullWhen(true)] SimVarRegistration? variable, out double value)
        {
            if (variable != null && e.GenericValueStatus.ContainsKey(variable))
            {
                value = e.GenericValueStatus[variable];
                return true;
            }
            value = 0;
            return false;
        }

        if (TryGetValue(displayVariable, out var newValue))
        {
            isUpdated |= newValue != currentValue;
            currentValue = newValue;

            if (displayVariable.variableName == "ZULU TIME"
                || displayVariable.variableName == "LOCAL TIME")
            {
                string hours = Math.Floor(newValue / 3600).ToString().PadLeft(2, '0');
                newValue = newValue % 3600;

                string minutes = Math.Floor(newValue / 60).ToString().PadLeft(2, '0');
                newValue = newValue % 60;

                string seconds = Math.Floor(newValue).ToString().PadLeft(2, '0');

                switch (customDecimals)
                {
                    case 0: //HH:MM:SS
                        currentValueTime = $"{hours}:{minutes}:{seconds}{(displayVariable.variableName == "ZULU TIME" ? "Z" : String.Empty)}";
                        currentValue = e.GenericValueStatus[displayVariable];
                        break;
                    case 1: //HH:MM
                        currentValueTime = $"{hours}:{minutes}{(displayVariable.variableName == "ZULU TIME" ? "Z" : String.Empty)}";
                        currentValue = e.GenericValueStatus[displayVariable];
                        break;
                    default:
                        currentValueTime = string.Empty;
                        currentValue = e.GenericValueStatus[displayVariable];
                        break;
                }
            }
        }

        if (TryGetValue(toggleEventDataVariable, out var newToggleValue))
        {
            toggleEventDataVariableValue = newToggleValue;
        }

        if (TryGetValue(holdEventDataVariable, out var newHoldValue))
        {
            holdEventDataVariableValue = newHoldValue;
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
            await InitializeSettingsAsync(args.Payload.ToObject<GenericToggleSettings4>());
        }
        await UpdateImage();
    }

    private void RegisterValues()
    {
        eventRegistrar.RegisterEvent(toggleEvent);
        eventRegistrar.RegisterEvent(toggleEvent2);
        eventRegistrar.RegisterEvent(toggleEvent3);
        eventRegistrar.RegisterEvent(toggleEvent4);
        eventRegistrar.RegisterEvent(holdEvent);

        var variables = new List<SimVarRegistration>();
        foreach (var feedbackVariable in feedbackVariables) variables.Add(feedbackVariable);

        if (displayVariable != null) variables.Add(displayVariable);
        if (toggleEventDataVariable != null) variables.Add(toggleEventDataVariable);
        if (toggleEventDataVariable2 != null) variables.Add(toggleEventDataVariable2);
        if (toggleEventDataVariable3 != null) variables.Add(toggleEventDataVariable3);
        if (toggleEventDataVariable4 != null) variables.Add(toggleEventDataVariable4);
        if (holdEventDataVariable != null) variables.Add(holdEventDataVariable);

        if (variables.Count > 0)
        {
            simVarManager.RegisterSimValues(variables.ToArray());
        }
    }

    private void DeRegisterValues()
    {
        var variables = new List<SimVarRegistration>();
        foreach (var feedbackVariable in feedbackVariables) variables.Add(feedbackVariable);
        if (displayVariable != null) variables.Add(displayVariable);
        if (toggleEventDataVariable != null) variables.Add(toggleEventDataVariable);
        if (toggleEventDataVariable2 != null) variables.Add(toggleEventDataVariable2);
        if (toggleEventDataVariable3 != null) variables.Add(toggleEventDataVariable3);
        if (toggleEventDataVariable4 != null) variables.Add(toggleEventDataVariable4);
        if (holdEventDataVariable != null) variables.Add(holdEventDataVariable);

        if (variables.Count > 0)
        {
            simVarManager.DeRegisterSimValues(variables.ToArray());
        }

        currentValue = null;
        currentValueTime = string.Empty;
        toggleEventDataVariableValue = null;
        toggleEventDataVariableValue2 = null;
        toggleEventDataVariableValue3 = null;
        toggleEventDataVariableValue4 = null;
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
        var result  = eventDispatcher.Trigger(toggleEvent, CalculateEventParam(toggleEventDataVariable, toggleEventDataVariableValue, toggleEventDataUInt));
        var result2 = eventDispatcher.Trigger(toggleEvent2, CalculateEventParam(toggleEventDataVariable2, toggleEventDataVariableValue2, toggleEventDataUInt2));
        var result3 = eventDispatcher.Trigger(toggleEvent3, CalculateEventParam(toggleEventDataVariable3, toggleEventDataVariableValue3, toggleEventDataUInt3));
        var result4 = eventDispatcher.Trigger(toggleEvent4, CalculateEventParam(toggleEventDataVariable4, toggleEventDataVariableValue4, toggleEventDataUInt4));
        if (!result)
        {
            await ShowAlertAsync();
        }

        return result;
    }

    private uint CalculateEventParam(SimVarRegistration? variable, double? variableValue, uint? inputValue)
    {
        if (variable != null && variableValue.HasValue)
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
            int? fontSize = null;
            if (int.TryParse(settings.FontSize, out var size))
            {
                fontSize = size;
            }

            var valueToShow = !string.IsNullOrEmpty(currentValueTime) ?
                currentValueTime :
                (displayVariable != null && currentValue.HasValue) ? currentValue.Value.ToString("F" + displayVariable.variableName.GetDecimals(customDecimals)) : "";

            var image = imageLogic.GetImage(settings.Header, currentStatus,
                value: valueToShow,
                fontSize: fontSize,
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

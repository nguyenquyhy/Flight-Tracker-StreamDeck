using FlightStreamDeck.Core;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace FlightStreamDeck.Logics.Actions;

public class GenericGaugeSettings
{
    public string Header { get; set; }
    public string FontSize { get; set; }
    public string MinValue { get; set; }
    public string MaxValue { get; set; }
    public string ToggleValue { get; set; }
    public string DisplayValue { get; set; }
    public string SubDisplayValue { get; set; }
    public string Type { get; set; }
    public string ValueUnit { get; set; }
    public string ValuePrecision { get; set; }
    public string HeaderBottom { get; set; }
    public string DisplayValueBottom { get; set; }
    public bool DisplayHorizontalValue { get; set; }
    public string ChartSplitValue { get; set; }
    public string ChartThicknessValue { get; set; }
    public string ChartChevronSizeValue { get; set; }
    public string AbsValText { get; set; }
    public bool HideLabelOutsideMinMaxTop { get; set; }
    public bool HideLabelOutsideMinMaxBottom { get; set; }

    public bool IsEmptyPayload()
        =>
            string.IsNullOrEmpty(Header) &&
            string.IsNullOrEmpty(HeaderBottom) &&
            string.IsNullOrEmpty(ToggleValue) &&
            string.IsNullOrEmpty(DisplayValue) &&
            string.IsNullOrEmpty(SubDisplayValue) &&
            string.IsNullOrEmpty(DisplayValueBottom) &&
            string.IsNullOrEmpty(ChartSplitValue) &&
            string.IsNullOrEmpty(AbsValText) &&
            string.IsNullOrEmpty(ValueUnit) &&
            string.IsNullOrEmpty(ValuePrecision) &&
            !HideLabelOutsideMinMaxTop &&
            !HideLabelOutsideMinMaxBottom &&
            string.IsNullOrEmpty(MinValue) &&
            string.IsNullOrEmpty(MaxValue) &&
            string.IsNullOrEmpty(ChartThicknessValue) &&
            string.IsNullOrEmpty(ChartChevronSizeValue) &&
            !DisplayHorizontalValue;
}

[StreamDeckAction("tech.flighttracker.streamdeck.generic.gauge")]
public class GenericGaugeAction : BaseAction<GenericGaugeSettings>
{
    private readonly ILogger<GenericGaugeAction> logger;
    private readonly IFlightConnector flightConnector;
    private readonly IImageLogic imageLogic;
    private readonly IEventRegistrar eventRegistrar;
    private readonly IEventDispatcher eventDispatcher;
    private readonly SimVarManager simVarManager;

    private string? toggleEvent = null;
    private SimVarRegistration? displayValue = null;
    private SimVarRegistration? subDisplayValue = null;
    private SimVarRegistration? displayValueBottom = null;
    private SimVarRegistration? minValue = null;
    private SimVarRegistration? maxValue = null;
    private int? customDecimals = null;

    private double currentValue = 0;
    private double currentValueBottom = 0;
    private double currentSubValue = float.MinValue;

    private double? currentMinValue = null;
    private double? currentMaxValue = null;

    private static readonly GenericGaugeSettings DefaultSettings = new()
    {
        Type = "Generic",
        DisplayHorizontalValue = true,
        ChartSplitValue = "12:red,24:yellow,64:green",
        ChartThicknessValue = "13",
        ChartChevronSizeValue = "3",
        Header = "L",
        DisplayValue = "FUEL LEFT QUANTITY",
        HeaderBottom = string.Empty,
        DisplayValueBottom = string.Empty,
        MinValue = "0",
        MaxValue = "30",
        AbsValText = "false",
        ValueUnit = string.Empty,
        ValuePrecision = "2",
        HideLabelOutsideMinMaxTop = false,
        HideLabelOutsideMinMaxBottom = false
    };

    public GenericGaugeAction(
        ILogger<GenericGaugeAction> logger,
        IFlightConnector flightConnector,
        IImageLogic imageLogic,
        IEventRegistrar eventRegistrar,
        IEventDispatcher eventDispatcher,
        SimVarManager simVarManager)
    {
        this.settings = DefaultSettings;
        this.logger = logger;
        this.flightConnector = flightConnector;
        this.imageLogic = imageLogic;
        this.eventRegistrar = eventRegistrar;
        this.eventDispatcher = eventDispatcher;
        this.simVarManager = simVarManager;
    }

    protected override async Task OnWillAppear(ActionEventArgs<AppearancePayload> args)
    {
        await InitializeSettingsAsync(args.Payload.GetSettings<GenericGaugeSettings>());

        flightConnector.GenericValuesUpdated += FlightConnector_GenericValuesUpdated;

        await UpdateImage();
    }

    protected override Task OnWillDisappear(ActionEventArgs<AppearancePayload> args)
    {
        flightConnector.GenericValuesUpdated -= FlightConnector_GenericValuesUpdated;
        DeRegisterValues();
        return Task.CompletedTask;
    }

    protected override Task OnKeyDown(ActionEventArgs<KeyPayload> args)
    {
        eventDispatcher.Trigger(toggleEvent);
        return Task.CompletedTask;
    }

    protected override async Task OnSendToPlugin(ActionEventArgs<JObject> args)
    {
        try
        {
            await InitializeSettingsAsync(args.Payload.ToObject<GenericGaugeSettings>());
            await UpdateImage();
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
        }
    }

    public override async Task InitializeSettingsAsync(GenericGaugeSettings? settings)
    {
        //keep constructor'd settings if the gauge is newly added.
        bool emptyPayload = settings?.IsEmptyPayload() == true;
        if (emptyPayload)
        {
            await UpdatePropertyInspector();
        }
        else
        {
            this.settings = settings;
        }

        var newToggleEvent = settings.ToggleValue;
        var newDisplayValue = simVarManager.GetRegistration(settings.DisplayValue, settings.ValueUnit);
        var newSubDisplayValue = simVarManager.GetRegistration(settings.SubDisplayValue, settings.ValueUnit);
        var newDisplayValueBottom = simVarManager.GetRegistration(settings.DisplayValueBottom, settings.ValueUnit);

        var newMinValue = simVarManager.GetRegistration(settings.MinValue);
        var newMaxValue = simVarManager.GetRegistration(settings.MaxValue);

        if (double.TryParse(string.IsNullOrWhiteSpace(settings.MinValue) ? DefaultSettings.MinValue : settings.MinValue, out var min))
        {
            currentMinValue = min;
        }
        if (double.TryParse(string.IsNullOrWhiteSpace(settings.MaxValue) ? DefaultSettings.MaxValue : settings.MaxValue, out var max))
        {
            currentMaxValue = max;
        }

        if (int.TryParse(settings.ValuePrecision, out int decimals))
        {
            customDecimals = decimals;
        }
        else
        {
            customDecimals = null;
        }

        if (newDisplayValue != displayValue || newDisplayValueBottom != displayValueBottom || newSubDisplayValue != subDisplayValue ||
            newMinValue != minValue || newMaxValue != maxValue)
        {
            DeRegisterValues();
        }

        toggleEvent = newToggleEvent;
        displayValue = newDisplayValue;
        subDisplayValue = newSubDisplayValue;
        displayValueBottom = newDisplayValueBottom;
        minValue = newMinValue;
        maxValue = newMaxValue;

        RegisterValues();
    }

    private async Task UpdatePropertyInspector()
    {
        await SendToPropertyInspectorAsync(this.settings);
    }

    private bool SetFromGenericValueStatus(Dictionary<SimVarRegistration, double> genericValueStatus, SimVarRegistration? variable, ref double currentValue)
    {
        bool isUpdated = false;
        if (variable != null)
        {
            if (genericValueStatus.ContainsKey(variable))
            {
                var newValue = genericValueStatus[variable];
                isUpdated = currentValue != newValue;
                currentValue = newValue;
            }
        }
        return isUpdated;
    }

    private async void FlightConnector_GenericValuesUpdated(object? sender, ToggleValueUpdatedEventArgs e)
    {
        if (StreamDeck == null) return;

        bool isUpdated = false;

        isUpdated |= SetFromGenericValueStatus(e.GenericValueStatus, displayValue, ref currentValue);
        isUpdated |= SetFromGenericValueStatus(e.GenericValueStatus, displayValueBottom, ref currentValueBottom);
        isUpdated |= SetFromGenericValueStatus(e.GenericValueStatus, subDisplayValue, ref currentSubValue);
        double min = 0;
        if (SetFromGenericValueStatus(e.GenericValueStatus, minValue, ref min))
        {
            currentMinValue = min;
            isUpdated = true;
        }
        double max = 0;
        if (SetFromGenericValueStatus(e.GenericValueStatus, maxValue, ref max))
        {
            currentMaxValue = max;
            isUpdated = true;
        }

        if (isUpdated)
        {
            await UpdateImage();
        }
    }

    private void RegisterValues()
    {
        eventRegistrar.RegisterEvent(toggleEvent);

        var values = new List<SimVarRegistration>();
        if (displayValue != null) values.Add(displayValue);
        if (subDisplayValue != null) values.Add(subDisplayValue);
        if (displayValueBottom != null) values.Add(displayValueBottom);
        if (minValue != null) values.Add(minValue);
        if (maxValue != null) values.Add(maxValue);

        if (values.Count > 0)
        {
            simVarManager.RegisterSimValues(values.ToArray());
        }
    }

    private void DeRegisterValues()
    {
        var values = new List<SimVarRegistration>();
        if (displayValue != null) values.Add(displayValue);
        if (subDisplayValue != null) values.Add(subDisplayValue);
        if (displayValueBottom != null) values.Add(displayValueBottom);
        if (minValue != null) values.Add(minValue);
        if (maxValue != null) values.Add(maxValue);

        if (values.Count > 0)
        {
            simVarManager.DeRegisterSimValues(values.ToArray());
        }

        currentValue = 0;
        currentValueBottom = 0;
        currentSubValue = float.MinValue;
    }

    private async Task UpdateImage()
    {
        if (settings != null &&
            currentMinValue.HasValue && currentMaxValue.HasValue)
        {
            switch (settings.Type)
            {
                case "Custom":
                    if (displayValue != null || displayValueBottom != null)
                    {
                        var chartSplit = string.IsNullOrEmpty(settings.ChartSplitValue) ? DefaultSettings.ChartSplitValue : settings.ChartSplitValue;
                        var numberFormat = $"F{KnownVariables.GetDecimals(displayValue?.variableName ?? displayValueBottom?.variableName, customDecimals)}";

                        bool.TryParse(settings.AbsValText, out bool absValueText);

                        int chartThickness = settings.ChartThicknessValue.ConvertTo<int>(DefaultSettings.ChartThicknessValue);
                        float chartChevronSize = settings.ChartChevronSizeValue.ConvertTo<int>(DefaultSettings.ChartChevronSizeValue);
                        int modifier = currentMinValue.Value > currentMaxValue.Value ? -1 : 1;

                        await SetImageSafeAsync(
                            imageLogic.GetCustomGaugeImage(
                                settings.Header,
                                settings.HeaderBottom,
                                currentValue * modifier,
                                currentValueBottom * modifier,
                                currentMinValue.Value,
                                currentMaxValue.Value,
                                numberFormat,
                                settings.DisplayHorizontalValue,
                                chartSplit?.Split(',') ?? Array.Empty<string>(),
                                chartThickness,
                                chartChevronSize,
                                absValueText,
                                settings.HideLabelOutsideMinMaxTop,
                                settings.HideLabelOutsideMinMaxBottom
                            )
                        );
                    }
                    break;
                default:
                    if (displayValue != null || displayValueBottom != null)
                    {
                        var numberFormat = "F" + (displayValue != null ? displayValue.variableName.GetDecimals(customDecimals) : 2);
                        var subValueText = subDisplayValue != null ? currentSubValue.ToString("F" + subDisplayValue.variableName.GetDecimals()) : null;
                        int? fontSize = null;
                        if (int.TryParse(settings.FontSize, out var size))
                        {
                            fontSize = size;
                        }

                        await SetImageSafeAsync(
                            imageLogic.GetGenericGaugeImage(
                                settings.Header,
                                currentValue,
                                currentMinValue.Value,
                                currentMaxValue.Value,
                                fontSize,
                                numberFormat,
                                subValueText
                            )
                        );
                    }

                    break;
            }
        }
    }
}

public static class GenericGaugeActionExtensions
{
    //https://stackoverflow.com/a/51429227
    public static T? ConvertTo<T>(this object? value, object defaultValue)
    {
        if (value is T variable) return variable;

        try
        {
            if (string.IsNullOrEmpty(value?.ToString()))
            {
                value = defaultValue;
            }

            //Handling Nullable types i.e, int?, double?, bool? .. etc
            if (Nullable.GetUnderlyingType(typeof(T)) != null)
            {
                return (T?)TypeDescriptor.GetConverter(typeof(T)).ConvertFrom(value);
            }

            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch (Exception)
        {
            return default(T);
        }
    }
}

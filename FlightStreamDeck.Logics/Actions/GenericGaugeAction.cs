using FlightStreamDeck.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SharpDeck;
using SharpDeck.Events.Received;
using SharpDeck.Manifest;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using EnumConverter = FlightStreamDeck.Core.EnumConverter;

namespace FlightStreamDeck.Logics.Actions
{
    public class GenericGaugeSettings
    {
        public string Header { get; set; }
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

        internal bool EmptyPayload
        {
            get =>
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
    }

    [StreamDeckAction("tech.flighttracker.streamdeck.generic.gauge")]
    public class GenericGaugeAction : StreamDeckAction<GenericGaugeSettings>
    {
        private readonly ILogger<GenericGaugeAction> logger;
        private readonly IFlightConnector flightConnector;
        private readonly IImageLogic imageLogic;
        private readonly EnumConverter enumConverter;

        private TOGGLE_EVENT? toggleEvent = null;
        private TOGGLE_VALUE? displayValue = null;
        private TOGGLE_VALUE? subDisplayValue = null;
        private TOGGLE_VALUE? displayValueBottom = null;
        private TOGGLE_VALUE? minValue = null;
        private TOGGLE_VALUE? maxValue = null;
        private string customUnit = null;
        private int? customDecimals = null;

        private double currentValue = 0;
        private double currentValueBottom = 0;
        private double currentSubValue = float.MinValue;

        private double? currentMinValue = null;
        private double? currentMaxValue = null;

        private static readonly GenericGaugeSettings DefaultSettings = new GenericGaugeSettings()
        {
            Type = "Generic",
            DisplayHorizontalValue = true,
            ChartSplitValue = "12:red,24:yellow,64:green",
            ChartThicknessValue = "13",
            ChartChevronSizeValue = "3",
            Header = "L",
            DisplayValue = TOGGLE_VALUE.FUEL_LEFT_QUANTITY.ToString(),
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

        private GenericGaugeSettings settings = null;

        public GenericGaugeAction(ILogger<GenericGaugeAction> logger, IFlightConnector flightConnector, IImageLogic imageLogic,
            EnumConverter enumConverter)
        {
            this.settings = DefaultSettings;
            this.logger = logger;
            this.flightConnector = flightConnector;
            this.imageLogic = imageLogic;
            this.enumConverter = enumConverter;
        }

        protected override async Task OnWillAppear(ActionEventArgs<AppearancePayload> args)
        {
            InitializeSettings(args.Payload.GetSettings<GenericGaugeSettings>());

            flightConnector.GenericValuesUpdated += FlightConnector_GenericValuesUpdated;

            RegisterValues();

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
            if (toggleEvent.HasValue) flightConnector.Trigger(toggleEvent.Value);
            return Task.CompletedTask;
        }

        protected override async Task OnSendToPlugin(ActionEventArgs<JObject> args)
        {
            try
            {
                await InitializeSettings(args.Payload.ToObject<GenericGaugeSettings>());
                await UpdateImage();
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
            }
        }

        private async Task InitializeSettings(GenericGaugeSettings settings)
        {
            //keep constructor'd settings if the gauge is newly added.
            bool emptyPayload = settings?.EmptyPayload ?? true;
            if (emptyPayload)
            {
                await this.UpdatePropertyInspector();
            }
            else
            {
                this.settings = settings;
            }

            TOGGLE_EVENT? newToggleEvent = enumConverter.GetEventEnum(this.settings.ToggleValue);
            TOGGLE_VALUE? newDisplayValue = enumConverter.GetVariableEnum(this.settings.DisplayValue);
            TOGGLE_VALUE? newSubDisplayValue = enumConverter.GetVariableEnum(this.settings.SubDisplayValue);
            TOGGLE_VALUE? newDisplayValueBottom = enumConverter.GetVariableEnum(this.settings.DisplayValueBottom);

            TOGGLE_VALUE? newMinValue = enumConverter.GetVariableEnum(this.settings.MinValue);
            TOGGLE_VALUE? newMaxValue = enumConverter.GetVariableEnum(this.settings.MaxValue);

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

            var newUnit = settings.ValueUnit?.Trim();
            if (string.IsNullOrWhiteSpace(newUnit)) newUnit = null;

            if (newDisplayValue != displayValue || newDisplayValueBottom != displayValueBottom || newSubDisplayValue != subDisplayValue ||
                newMinValue != minValue || newMaxValue != maxValue || newUnit != customUnit)
            {
                DeRegisterValues();
            }

            toggleEvent = newToggleEvent;
            displayValue = newDisplayValue;
            customUnit = newUnit;
            subDisplayValue = newSubDisplayValue;
            displayValueBottom = newDisplayValueBottom;
            minValue = newMinValue;
            maxValue = newMaxValue;

            RegisterValues();
        }

        private async Task UpdatePropertyInspector()
        {
            await this.SendToPropertyInspectorAsync(this.settings);
        }

        private bool SetFromGenericValueStatus(Dictionary<(TOGGLE_VALUE variable, string unit), double> genericValueStatus, TOGGLE_VALUE? variable, string unit, ref double currentValue)
        {
            bool isUpdated = false;
            if (variable.HasValue)
            {
                var tuple = (variable.Value, unit);
                if (genericValueStatus.ContainsKey(tuple))
                {
                    var newValue = genericValueStatus[tuple];
                    isUpdated = currentValue != newValue;
                    currentValue = newValue;
                }
            }
            return isUpdated;
        }

        private async void FlightConnector_GenericValuesUpdated(object sender, ToggleValueUpdatedEventArgs e)
        {
            if (StreamDeck == null) return;

            bool isUpdated = false;

            isUpdated |= SetFromGenericValueStatus(e.GenericValueStatus, displayValue, customUnit, ref currentValue);
            isUpdated |= SetFromGenericValueStatus(e.GenericValueStatus, displayValueBottom, customUnit, ref currentValueBottom);
            isUpdated |= SetFromGenericValueStatus(e.GenericValueStatus, subDisplayValue, null, ref currentSubValue);
            double min = 0;
            if (SetFromGenericValueStatus(e.GenericValueStatus, minValue, null, ref min))
            {
                currentMinValue = min;
                isUpdated = true;
            }
            double max = 0;
            if (SetFromGenericValueStatus(e.GenericValueStatus, maxValue, null, ref max))
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
            if (toggleEvent.HasValue) flightConnector.RegisterToggleEvent(toggleEvent.Value);

            var values = new List<(TOGGLE_VALUE variables, string unit)>();
            if (displayValue.HasValue) values.Add((displayValue.Value, customUnit));
            if (subDisplayValue.HasValue) values.Add((subDisplayValue.Value, customUnit));
            if (displayValueBottom.HasValue) values.Add((displayValueBottom.Value, customUnit));
            if (minValue.HasValue) values.Add((minValue.Value, null));
            if (maxValue.HasValue) values.Add((maxValue.Value, null));

            if (values.Count > 0)
            {
                flightConnector.RegisterSimValues(values.ToArray());
            }
        }

        private void DeRegisterValues()
        {
            var values = new List<(TOGGLE_VALUE variables, string unit)>();
            if (displayValue.HasValue) values.Add((displayValue.Value, customUnit));
            if (subDisplayValue.HasValue) values.Add((subDisplayValue.Value, customUnit));
            if (displayValueBottom.HasValue) values.Add((displayValueBottom.Value, customUnit));
            if (minValue.HasValue) values.Add((minValue.Value, null));
            if (maxValue.HasValue) values.Add((maxValue.Value, null));

            if (values.Count > 0)
            {
                flightConnector.DeRegisterSimValues(values.ToArray());
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
                        if (displayValue.HasValue || displayValueBottom.HasValue)
                        {
                            var chartSplit = string.IsNullOrEmpty(settings.ChartSplitValue) ? DefaultSettings.ChartSplitValue : settings.ChartSplitValue;
                            var numberFormat = $"F{EventValueLibrary.GetDecimals(displayValue ?? displayValueBottom.Value, customDecimals)}";

                            bool.TryParse(settings.AbsValText, out bool absValueText);

                            int chartThickness = settings.ChartThicknessValue.ConvertTo<int>(DefaultSettings.ChartThicknessValue);
                            float chartChevronSize = settings.ChartChevronSizeValue.ConvertTo<int>(DefaultSettings.ChartChevronSizeValue);
                            int modifier = currentMinValue.Value > currentMaxValue.Value ? -1 : 1;
                            await SetImageAsync(
                                imageLogic.GetCustomGaugeImage(
                                    settings.Header,
                                    settings.HeaderBottom,
                                    currentValue * modifier,
                                    currentValueBottom * modifier,
                                    currentMinValue.Value,
                                    currentMaxValue.Value,
                                    numberFormat,
                                    settings.DisplayHorizontalValue,
                                    chartSplit?.Split(','),
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
                        if (displayValue.HasValue || displayValueBottom.HasValue)
                        {
                            var numberFormat = "F" + (displayValue.HasValue ? EventValueLibrary.GetDecimals(displayValue.Value, customDecimals) : 2);
                            var subValueText = subDisplayValue.HasValue ? currentSubValue.ToString("F" + EventValueLibrary.GetDecimals(subDisplayValue.Value)) : null;
                            await SetImageAsync(
                                imageLogic.GetGenericGaugeImage(
                                    settings.Header,
                                    currentValue,
                                    currentMinValue.Value,
                                    currentMaxValue.Value,
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
        public static T ConvertTo<T>(this object? value, object defaultValue)
        {
            if (value is T variable) return variable;

            try
            {
                if (string.IsNullOrEmpty(value.ToString()) && defaultValue != null)
                {
                    value = defaultValue;
                }

                //Handling Nullable types i.e, int?, double?, bool? .. etc
                if (Nullable.GetUnderlyingType(typeof(T)) != null)
                {
                    return (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFrom(value);
                }

                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch (Exception)
            {
                return default(T);
            }
        }
    }
}

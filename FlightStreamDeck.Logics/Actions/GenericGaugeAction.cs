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
    public class GenericGaugeAction : BaseAction<GenericGaugeSettings>
    {
        private readonly ILogger<GenericGaugeAction> logger;
        private readonly IFlightConnector flightConnector;
        private readonly IImageLogic imageLogic;

        private ToggleEvent toggleEvent = null;
        private ToggleValue displayValue = null;
        private ToggleValue subDisplayValue = null;
        private ToggleValue displayValueBottom = null;
        private ToggleValue minValue = null;
        private ToggleValue maxValue = null;
        private string customUnit = null;
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
            DisplayValue = "FUEL_LEFT_QUANTITY",
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
        }

        protected override async Task OnWillAppear(ActionEventArgs<AppearancePayload> args)
        {
            await InitializeSettings(args.Payload.GetSettings<GenericGaugeSettings>());

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
            if (toggleEvent != null) flightConnector.Trigger(toggleEvent);
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

            ToggleEvent newToggleEvent = new(this.settings.ToggleValue);
            ToggleValue newDisplayValue = new(this.settings.DisplayValue);
            ToggleValue newSubDisplayValue = new(this.settings.SubDisplayValue);
            ToggleValue newDisplayValueBottom = new(this.settings.DisplayValueBottom);

            ToggleValue newMinValue = new(this.settings.MinValue);
            ToggleValue newMaxValue = new(this.settings.MaxValue);

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

        private static bool SetFromGenericValueStatus(List<ToggleValue> genericValueStatus, ToggleValue variable, ref double currentValue)
        {
            bool isUpdated = false;
            if (variable != null)
            {
                var tuple = variable;
                if (genericValueStatus.Find(x => x.Name == tuple.Name) != null)
                {
                    var newValue = genericValueStatus.Find(x => x.Name == tuple.Name).Value;
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
            if (toggleEvent != null) flightConnector.RegisterToggleEvent(toggleEvent);

            var values = new List<ToggleValue>();
            if (displayValue != null) values.Add((new ToggleValue(displayValue.Name, customUnit)));
            if (subDisplayValue != null) values.Add(new ToggleValue(subDisplayValue.Name, customUnit));
            if (displayValueBottom != null) values.Add(new ToggleValue(displayValueBottom.Name, customUnit));
            if (minValue != null) values.Add(new ToggleValue(minValue.Name));
            if (maxValue != null) values.Add(new ToggleValue(maxValue.Name));

            if (values.Count > 0)
            {
                flightConnector.RegisterSimValues(values.ToArray());
            }
        }

        private void DeRegisterValues()
        {
            var values = new List<ToggleValue>();
            if (displayValue != null) values.Add(new ToggleValue(displayValue.Name, customUnit));
            if (subDisplayValue != null) values.Add(new ToggleValue(subDisplayValue.Name, customUnit));
            if (displayValueBottom != null) values.Add(new ToggleValue(displayValueBottom.Name, customUnit));
            if (minValue != null) values.Add(new ToggleValue(minValue.Name));
            if (maxValue != null) values.Add(new ToggleValue(maxValue.Name));

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
                        if (displayValue != null || displayValueBottom != null)
                        {
                            var chartSplit = string.IsNullOrEmpty(settings.ChartSplitValue) ? DefaultSettings.ChartSplitValue : settings.ChartSplitValue;
                            var numberFormat = $"F{displayValue.Decimals ?? displayValueBottom.Decimals : customDecimals}";

                            int chartThickness = settings.ChartThicknessValue.ConvertTo<int>(DefaultSettings.ChartThicknessValue);
                            float chartChevronSize = settings.ChartChevronSizeValue.ConvertTo<int>(DefaultSettings.ChartChevronSizeValue);
                            int modifier = currentMinValue.Value > currentMaxValue.Value ? -1 : 1;
                            
                            if (bool.TryParse(settings.AbsValText, out bool absValueText))
                            {
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
                                        chartSplit?.Split(','),
                                        chartThickness,
                                        chartChevronSize,
                                        absValueText,
                                        settings.HideLabelOutsideMinMaxTop,
                                        settings.HideLabelOutsideMinMaxBottom
                                    )
                                );
                            }
                        }
                        break;
                    default:
                        if (displayValue != null || displayValueBottom != null)
                        {
                            var numberFormat = "F" + (displayValue != null ? displayValue.Decimals : 2);
                            var subValueText = subDisplayValue != null ? currentSubValue.ToString("F" + subDisplayValue.Decimals) : null;

                            await SetImageSafeAsync(
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
                return default;
            }
        }
    }
}

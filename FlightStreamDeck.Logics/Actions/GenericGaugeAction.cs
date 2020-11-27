using FlightStreamDeck.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SharpDeck;
using SharpDeck.Events.Received;
using SharpDeck.Manifest;
using System;
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

        private float currentValue = 0;
        private float currentValueBottom = 0;
        private float currentSubValue = float.MinValue;

        private GenericGaugeSettings defaultSettings => new GenericGaugeSettings()
        {
            Type = "Generic",
            DisplayHorizontalValue = true,
            ChartSplitValue = "12:red,24:yellow,64:green",
            ChartThicknessValue = "13",
            ChartChevronSizeValue = "3",
            Header = "L",
            DisplayValue = Core.TOGGLE_VALUE.FUEL_LEFT_QUANTITY.ToString(),
            HeaderBottom = string.Empty,
            DisplayValueBottom = string.Empty,
            MinValue = "0",
            MaxValue = "30",
            AbsValText = "false",
            ValuePrecision = "2",
            HideLabelOutsideMinMaxTop = false,
            HideLabelOutsideMinMaxBottom = false
        };

        private GenericGaugeSettings settings = null;

        public GenericGaugeAction(ILogger<GenericGaugeAction> logger, IFlightConnector flightConnector, IImageLogic imageLogic,
            EnumConverter enumConverter)
        {
            this.settings = this.defaultSettings;
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

            if (newDisplayValue != displayValue || newDisplayValueBottom != displayValueBottom || newSubDisplayValue != subDisplayValue)
            {
                DeRegisterValues();
            }

            toggleEvent = newToggleEvent;
            displayValue = newDisplayValue;
            subDisplayValue = newSubDisplayValue;
            displayValueBottom = newDisplayValueBottom;

            RegisterValues();
        }

        private async Task UpdatePropertyInspector()
        {
            await this.SendToPropertyInspectorAsync(this.settings);
        }

        private async void FlightConnector_GenericValuesUpdated(object sender, ToggleValueUpdatedEventArgs e)
        {
            if (StreamDeck == null) return;

            bool isUpdated = false;

            if (displayValue.HasValue && e.GenericValueStatus.ContainsKey(displayValue.Value))
            {
                float.TryParse(e.GenericValueStatus[displayValue.Value], out float newValue);
                isUpdated = currentValue != newValue;
                currentValue = newValue;
            }
            if (displayValueBottom.HasValue && e.GenericValueStatus.ContainsKey(displayValueBottom.Value))
            {
                float.TryParse(e.GenericValueStatus[displayValueBottom.Value], out float newValue);
                isUpdated |= currentValueBottom != newValue;
                currentValueBottom = newValue;
            }
            if (subDisplayValue.HasValue && e.GenericValueStatus.ContainsKey(subDisplayValue.Value))
            {
                float.TryParse(e.GenericValueStatus[subDisplayValue.Value], out float newValue);
                isUpdated |= currentSubValue != newValue;
                currentSubValue = newValue;
            }

            if (isUpdated)
            {
                await UpdateImage();
            }
        }

        private void RegisterValues()
        {
            if (toggleEvent.HasValue) flightConnector.RegisterToggleEvent(toggleEvent.Value);
            if (displayValue.HasValue) flightConnector.RegisterSimValue(displayValue.Value);
            if (subDisplayValue.HasValue) flightConnector.RegisterSimValue(subDisplayValue.Value);
            if (displayValueBottom.HasValue) flightConnector.RegisterSimValue(displayValueBottom.Value);
        }

        private void DeRegisterValues()
        {
            if (displayValue.HasValue) flightConnector.DeRegisterSimValue(displayValue.Value);
            if (subDisplayValue.HasValue) flightConnector.DeRegisterSimValue(subDisplayValue.Value);
            if (displayValueBottom.HasValue) flightConnector.DeRegisterSimValue(displayValueBottom.Value);
            currentValue = 0;
            currentValueBottom = 0;
            currentSubValue = float.MinValue;
        }

        private async Task UpdateImage()
        {
            if (settings != null)
            {
                string precision = $"F{((settings.ValuePrecision?.Length ?? 0) > 0 ? settings.ValuePrecision : this.defaultSettings.ValuePrecision)}";
                float MinValue = settings.MinValue.ConvertTo<float>(this.defaultSettings.MinValue);
                float MaxValue = settings.MaxValue.ConvertTo<float>(this.defaultSettings.MaxValue);
                string chartSplit = string.IsNullOrEmpty(settings.ChartSplitValue) ? defaultSettings.ChartSplitValue : settings.ChartSplitValue;
                if (settings.Type?.Equals("Custom", StringComparison.OrdinalIgnoreCase) ?? false)
                {
                    bool.TryParse(settings.AbsValText, out bool absValueText);

                    int chartThickness = settings.ChartThicknessValue.ConvertTo<int>(this.defaultSettings.ChartThicknessValue);
                    float chartChevronSize = settings.ChartChevronSizeValue.ConvertTo<int>(this.defaultSettings.ChartChevronSizeValue);
                    int modifier = MinValue > MaxValue ? -1 : 1;
                    await SetImageAsync(
                        imageLogic.GetCustomGaugeImage(
                            settings.Header,
                            settings.HeaderBottom,
                            (currentValue * modifier).ToString(precision),
                            (currentValueBottom * modifier).ToString(precision),
                            MinValue,
                            MaxValue,
                            settings.DisplayHorizontalValue,
                            chartSplit?.Split(','),
                            chartThickness,
                            chartChevronSize,
                            absValueText,
                            precision,
                            settings.HideLabelOutsideMinMaxTop,
                            settings.HideLabelOutsideMinMaxBottom
                        )
                    );
                }
                else
                {
                    await SetImageAsync(
                        imageLogic.GetGenericGaugeImage(
                            settings.Header,
                            currentValue,
                            MinValue,
                            MaxValue,
                            precision,
                            currentSubValue
                        )
                    );
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

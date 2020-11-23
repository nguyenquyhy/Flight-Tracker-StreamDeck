using FlightStreamDeck.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SharpDeck;
using SharpDeck.Events.Received;
using SharpDeck.Manifest;
using System;
using System.Threading.Tasks;

namespace FlightStreamDeck.Logics.Actions
{
    public class GenericGaugeSettings
    {
        public string Header { get; set; }
        public float MinValue { get; set; }
        public float MaxValue { get; set; }
        public string ToggleValue { get; set; }
        public string DisplayValue { get; set; }
        public string SubDisplayValue { get; set; }
        public string Type { get; set; }
        public string ValuePrecision { get; set; }
        public string HeaderBottom { get; set; }
        public string DisplayValueBottom { get; set; }
        public bool DisplayHorizontalValue { get; set; }
        public string ChartSplitValue { get; set; }
        public int ChartThicknessValue { get; set; }
        public int ChartChevronSizeValue { get; set; }
        public string AbsValText { get; set; }

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
                MinValue == 0 &&
                MaxValue == 0 &&
                ChartThicknessValue == 0 &&
                ChartChevronSizeValue == 0 && !DisplayHorizontalValue;
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

        private GenericGaugeSettings settings = new GenericGaugeSettings()
        {
            Type = "Custom",
            DisplayHorizontalValue = true,
            ChartSplitValue = "12:red,24:yellow,64:green",
            ChartThicknessValue = 13,
            ChartChevronSizeValue = 3,
            Header = "L",
            DisplayValue = Core.TOGGLE_VALUE.FUEL_LEFT_QUANTITY.ToString(),
            HeaderBottom = string.Empty,
            DisplayValueBottom = string.Empty,
            MinValue = 0,
            MaxValue = 30,
            AbsValText = "false",
            ValuePrecision = "2"
        };

        public GenericGaugeAction(ILogger<GenericGaugeAction> logger, IFlightConnector flightConnector, IImageLogic imageLogic,
            EnumConverter enumConverter)
        {
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
            if (toggleEvent.HasValue) flightConnector.Toggle(toggleEvent.Value);
            return Task.CompletedTask;
        }

        protected override async Task OnSendToPlugin(ActionEventArgs<JObject> args)
        {
            try
            {
                InitializeSettings(args.Payload.ToObject<GenericGaugeSettings>());
                await UpdateImage();
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
            }
        }

        private void InitializeSettings(GenericGaugeSettings settings)
        {
            this.settings = settings;

            TOGGLE_EVENT? newToggleEvent = enumConverter.GetEventEnum(settings.ToggleValue);
            TOGGLE_VALUE? newDisplayValue = enumConverter.GetVariableEnum(settings.DisplayValue);
            TOGGLE_VALUE? newSubDisplayValue = enumConverter.GetVariableEnum(settings.SubDisplayValue);
            TOGGLE_VALUE? newDisplayValueBottom = enumConverter.GetVariableEnum(settings.DisplayValueBottom);

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
                string precision = $"F{((settings.ValuePrecision?.Length ?? 0) > 0 ? settings.ValuePrecision : "2")}";
                if (settings.Type?.Equals("Custom", StringComparison.OrdinalIgnoreCase) ?? false)
                {
                    int modifier = settings.MinValue > settings.MaxValue ? -1 : 1;
                    bool.TryParse(settings.AbsValText, out bool absValueText);
                    await SetImageAsync(
                        imageLogic.GetCustomGaugeImage(
                            settings.Header,
                            settings.HeaderBottom,
                            (currentValue * modifier).ToString(precision),
                            (currentValueBottom * modifier).ToString(precision),
                            settings.MinValue,
                            settings.MaxValue,
                            settings.DisplayHorizontalValue,
                            settings.ChartSplitValue?.Split(','),
                            settings.ChartThicknessValue,
                            settings.ChartChevronSizeValue,
                            absValueText,
                            precision
                        )
                    );
                } 
                else
                {
                    await SetImageAsync(
                        imageLogic.GetGenericGaugeImage(
                            settings.Header,
                            currentValue,
                            settings.MinValue,
                            settings.MaxValue,
                            precision,
                            currentSubValue
                        )
                    );
                }
            }
        }
    }
}

using FlightStreamDeck.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SharpDeck;
using SharpDeck.Events.Received;
using SharpDeck.Manifest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlightStreamDeck.Logics.Actions
{
    public class CustomGaugeSettings
    {
        public string HeaderTop { get; set; }
        public string HeaderBottom { get; set; }
        public float MinValue { get; set; }
        public float MaxValue { get; set; }
        public string ToggleValue { get; set; }
        public string DisplayValueTop { get; set; }
        public string DisplayValueBottom { get; set; }
        public bool DisplayHorizontalValue { get; set; }
        public string ChartSplitValue { get; set; }
        public int ChartThicknessValue { get; set; }
        public int ChartChevronSizeValue { get; set; }

        internal bool EmptyPayload { get =>
                string.IsNullOrEmpty(HeaderTop) &&
                string.IsNullOrEmpty(HeaderBottom) &&
                string.IsNullOrEmpty(ToggleValue) &&
                string.IsNullOrEmpty(DisplayValueTop) &&
                string.IsNullOrEmpty(DisplayValueBottom) &&
                string.IsNullOrEmpty(ChartSplitValue) &&
                MinValue == 0 &&
                MaxValue == 0 &&
                ChartThicknessValue == 0 &&
                ChartChevronSizeValue == 0 && !DisplayHorizontalValue;
        }
    }

    [StreamDeckAction("tech.flighttracker.streamdeck.custom.gauge")]
    public class CustomGaugeAction : StreamDeckAction<CustomGaugeSettings>
    {
        private readonly ILogger<CustomGaugeSettings> logger;
        private readonly IFlightConnector flightConnector;
        private readonly IImageLogic imageLogic;
        private readonly EnumConverter enumConverter;

        private TOGGLE_EVENT? toggleEvent = null;
        private TOGGLE_VALUE? displayValueTop = null;
        private TOGGLE_VALUE? displayValueBottom = null;

        private string currentValueTop = "0";
        private string currentValueBottom = "0";

        public CustomGaugeSettings settings = new CustomGaugeSettings() { 
            DisplayHorizontalValue = true,
            ChartSplitValue = "12:red,24:yellow,64:green",
            ChartThicknessValue = 13,
            ChartChevronSizeValue = 3,
            HeaderTop = "L",
            DisplayValueTop = "FUEL LEFT QUANTITY",
            HeaderBottom = "R",
            DisplayValueBottom = "FUEL RIGHT QUANTITY",
            MinValue = 0,
            MaxValue = 30,
        };

        public CustomGaugeAction(ILogger<CustomGaugeSettings> logger, IFlightConnector flightConnector, IImageLogic imageLogic,
            EnumConverter enumConverter)
        {
            this.logger = logger;
            this.flightConnector = flightConnector;
            this.imageLogic = imageLogic;
            this.enumConverter = enumConverter;
        }

        protected override async Task OnWillAppear(ActionEventArgs<AppearancePayload> args)
        {
            var settings = args.Payload.GetSettings<CustomGaugeSettings>();

            if (settings.EmptyPayload) settings = this.settings;
            InitializeSettings(settings);

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
                InitializeSettings(args.Payload.ToObject<CustomGaugeSettings>());
                await UpdateImage();
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
            }
        }

        private void InitializeSettings(CustomGaugeSettings settings)
        {
            this.settings = settings;

            TOGGLE_EVENT? newToggleEvent = enumConverter.GetEventEnum(settings.ToggleValue);
            TOGGLE_VALUE? newDisplayValueTop = enumConverter.GetVariableEnum(settings.DisplayValueTop);
            TOGGLE_VALUE? newDisplayValueBottom = enumConverter.GetVariableEnum(settings.DisplayValueBottom);

            if (newDisplayValueTop != displayValueTop || newDisplayValueBottom != displayValueBottom)
            {
                DeRegisterValues();
            }

            toggleEvent = newToggleEvent;
            displayValueTop = newDisplayValueTop;
            displayValueBottom = newDisplayValueBottom;

            RegisterValues();
        }

        private async void FlightConnector_GenericValuesUpdated(object sender, ToggleValueUpdatedEventArgs e)
        {
            if (StreamDeck == null) return;

            bool isUpdated = false;

            if (displayValueTop.HasValue && e.GenericValueStatus.ContainsKey(displayValueTop.Value))
            {
                string newValue = e.GenericValueStatus[displayValueTop.Value];
                isUpdated |= currentValueTop != newValue;
                currentValueTop = newValue;
            }
            if (displayValueBottom.HasValue && e.GenericValueStatus.ContainsKey(displayValueBottom.Value))
            {
                string newValue = e.GenericValueStatus[displayValueBottom.Value];
                isUpdated |= currentValueBottom != newValue;
                currentValueBottom = newValue;
            }

            if (isUpdated)
            {
                await UpdateImage();
            }
        }

        private void RegisterValues()
        {
            if (toggleEvent.HasValue) flightConnector.RegisterToggleEvent(toggleEvent.Value);
            if (displayValueTop.HasValue) flightConnector.RegisterSimValue(displayValueTop.Value);
            if (displayValueBottom.HasValue) flightConnector.RegisterSimValue(displayValueBottom.Value);
        }

        private void DeRegisterValues()
        {
            if (displayValueTop.HasValue) flightConnector.DeRegisterSimValue(displayValueTop.Value);
            if (displayValueBottom.HasValue) flightConnector.DeRegisterSimValue(displayValueBottom.Value);
            currentValueTop = "0";
            currentValueBottom = "0";
        }

        private async Task UpdateImage()
        {
            if (settings != null)
            {
                await SetImageAsync(imageLogic.GetCustomGaugeImage(
                    settings.HeaderTop, settings.HeaderBottom, 
                    currentValueTop, currentValueBottom, 
                    settings.MinValue, settings.MaxValue, 
                    settings.DisplayHorizontalValue,
                    settings.ChartSplitValue?.Split(','), 
                    settings.ChartThicknessValue, 
                    settings.ChartChevronSizeValue));
            }
        }
    }
}

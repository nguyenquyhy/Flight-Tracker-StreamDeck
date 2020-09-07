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
    }

    [StreamDeckAction("tech.flighttracker.streamdeck.generic.gauge")]
    public class GenericGaugeAction : StreamDeckAction<GenericGaugeSettings>
    {
        private readonly ILogger<ApToggleAction> logger;
        private readonly IFlightConnector flightConnector;
        private readonly IImageLogic imageLogic;

        private TOGGLE_EVENT? toggleEvent = null;
        private TOGGLE_VALUE? displayValue = null;

        private float currentValue = 0;

        private GenericGaugeSettings settings;

        public GenericGaugeAction(ILogger<ApToggleAction> logger, IFlightConnector flightConnector, IImageLogic imageLogic)
        {
            this.logger = logger;
            this.flightConnector = flightConnector;
            this.imageLogic = imageLogic;
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

            TOGGLE_EVENT? newToggleEvent = GetEventValue(settings.ToggleValue);
            TOGGLE_VALUE? newDisplayValue = GetValueValue(settings.DisplayValue);

            if (newDisplayValue != displayValue)
            {
                DeRegisterValues();
            }

            toggleEvent = newToggleEvent;
            displayValue = newDisplayValue;

            RegisterValues();
        }

        private TOGGLE_EVENT? GetEventValue(string value)
        {
            TOGGLE_EVENT result;
            if (Enum.TryParse<TOGGLE_EVENT>(value, true, out result))
            {
                return result;
            }

            return null;
        }

        private TOGGLE_VALUE? GetValueValue(string value)
        {
            if (value == null) return null;

            if (Enum.TryParse<TOGGLE_VALUE>(value.Replace(":", "__").Replace(" ", "_"), true, out var result))
            {
                return result;
            }

            return null;
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

            if (isUpdated)
            {
                await UpdateImage();
            }
        }

        private void RegisterValues()
        {
            if (toggleEvent.HasValue) flightConnector.RegisterToggleEvent(toggleEvent.Value);
            if (displayValue.HasValue) flightConnector.RegisterSimValue(displayValue.Value);
        }

        private void DeRegisterValues()
        {
            if (displayValue.HasValue) flightConnector.DeRegisterSimValue(displayValue.Value);
            currentValue = 0;
        }

        private async Task UpdateImage()
        {
            if (settings != null)
            {
                await SetImageAsync(imageLogic.GetGaugeImage(settings.Header, currentValue, settings.MinValue, settings.MaxValue));
            }
        }
    }
}

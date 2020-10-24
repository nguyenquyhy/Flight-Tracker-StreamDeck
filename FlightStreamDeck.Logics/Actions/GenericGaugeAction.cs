using FlightStreamDeck.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SharpDeck;
using SharpDeck.Events.Received;
using SharpDeck.Manifest;
using System;
using System.ComponentModel.DataAnnotations;
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
        private TOGGLE_VALUE? minValue = null;
        private TOGGLE_VALUE? maxValue = null;

        private float currentValue = 0;
        private float minFloatValue = 0;
        private float maxFloatValue = 0;

        private GenericGaugeSettings settings;

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
            TOGGLE_VALUE? newMinValue = enumConverter.GetVariableEnum(settings.MinValue);
            TOGGLE_VALUE? newMaxValue = enumConverter.GetVariableEnum(settings.MaxValue);

            if (newDisplayValue != displayValue || newMinValue != minValue || newMaxValue != maxValue)
            {
                DeRegisterValues();
            }

            toggleEvent = newToggleEvent;
            displayValue = newDisplayValue;
            minValue = newMinValue;
            maxValue = newMaxValue;

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

            if (minValue.HasValue && e.GenericValueStatus.ContainsKey(minValue.Value))
            {
                float.TryParse(e.GenericValueStatus[minValue.Value], out float newValue);
                isUpdated |= minFloatValue != newValue;
                minFloatValue = newValue;
            } else
            {
                if (float.TryParse(settings.MinValue, out float result)) minFloatValue = result;
            }

            if (maxValue.HasValue && e.GenericValueStatus.ContainsKey(maxValue.Value))
            {
                float.TryParse(e.GenericValueStatus[maxValue.Value], out float newValue);
                isUpdated |= maxFloatValue != newValue;
                maxFloatValue = newValue;
            } else
            {
                if (float.TryParse(settings.MaxValue, out float result)) maxFloatValue = result;
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
            if (minValue.HasValue) flightConnector.RegisterSimValue(minValue.Value);
            if (maxValue.HasValue) flightConnector.RegisterSimValue(maxValue.Value);
        }

        private void DeRegisterValues()
        {
            if (displayValue.HasValue) flightConnector.DeRegisterSimValue(displayValue.Value);
            if (minValue.HasValue) flightConnector.DeRegisterSimValue(minValue.Value);
            if (maxValue.HasValue) flightConnector.DeRegisterSimValue(maxValue.Value);
            currentValue = 0;
            minFloatValue = 0;
            maxFloatValue = 0;
        }

        private async Task UpdateImage()
        {
            if (settings != null)
            {
                await SetImageAsync(imageLogic.GetGenericGaugeImage(settings.Header, currentValue, minFloatValue, maxFloatValue));
            }
        }
    }
}

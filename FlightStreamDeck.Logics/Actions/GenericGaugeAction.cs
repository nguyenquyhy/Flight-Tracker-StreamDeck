using FlightStreamDeck.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SharpDeck;
using SharpDeck.Events.Received;
using System;
using System.Threading.Tasks;

namespace FlightStreamDeck.Logics.Actions
{
    public class GenericGaugeAction : StreamDeckAction
    {
        private readonly ILogger<ApToggleAction> logger;
        private readonly IFlightConnector flightConnector;
        private readonly IImageLogic imageLogic;

        private string header = "";
        private TOGGLE_EVENT? toggleEvent = null;
        private TOGGLE_VALUE? displayValue = null;

        private float currentValue = 0;
        private float min = 0;
        private float max = 1;

        public GenericGaugeAction(ILogger<ApToggleAction> logger, IFlightConnector flightConnector, IImageLogic imageLogic)
        {
            this.logger = logger;
            this.flightConnector = flightConnector;
            this.imageLogic = imageLogic;
        }

        protected override async Task OnWillAppear(ActionEventArgs<AppearancePayload> args)
        {
            setValues(args.Payload.Settings);

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

        protected override Task OnSendToPlugin(ActionEventArgs<JObject> args)
        {
            try
            {
                setValues(args.Payload);
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
            }

            _= UpdateImage();
            return Task.CompletedTask;
        }

        private void setValues(JObject settings)
        {
            string newHeader = settings.Value<string>("Header");
            float newMin = settings.Value<float>("MinValue");
            float newMax = settings.Value<float>("MaxValue");
            TOGGLE_EVENT? newToggleEvent = GetEventValue(settings.Value<string>("ToggleValue"));
            TOGGLE_VALUE? newDisplayValue = GetValueValue(settings.Value<string>("DisplayValue"));

            if (newDisplayValue != displayValue)
            {
                DeRegisterValues();
            }

            header = newHeader;
            toggleEvent = newToggleEvent;
            displayValue = newDisplayValue;
            min = newMin;
            max = newMax;

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
            TOGGLE_VALUE result;
            if (Enum.TryParse<TOGGLE_VALUE>(value.Replace(":", "__").Replace(" ", "_"), true, out result))
            {
                return result;
            }

            return null;
        }

        private async void FlightConnector_GenericValuesUpdated(object sender, ToggleValueUpdatedEventArgs e)
        {
            bool isUpdated = false;

            if (e.GenericValueStatus.ContainsKey(displayValue.Value))
            {
                float newValue = 0;
                float.TryParse(e.GenericValueStatus[displayValue.Value], out newValue);
                isUpdated = currentValue != newValue;
                if (displayValue.Value == TOGGLE_VALUE.ELEVATOR_TRIM_POSITION)
                {
                    newValue = newValue * -1;
                }
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
            await SetImageAsync(imageLogic.GetGaugeImage(header, currentValue, min, max));
        }
    }
}

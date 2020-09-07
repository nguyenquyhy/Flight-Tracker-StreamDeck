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
    [StreamDeckAction("tech.flighttracker.streamdeck.generic.toggle")]
    public class GenericToggleAction : StreamDeckAction
    {
        private readonly ILogger<ApToggleAction> logger;
        private readonly IFlightConnector flightConnector;
        private readonly IImageLogic imageLogic;

        private string header = "";
        private TOGGLE_EVENT? toggleEvent = null;
        private TOGGLE_VALUE? feedbackValue = null;
        private TOGGLE_VALUE? displayValue = null;

        private string currentValue = "";
        private bool currentStatus = false;

        public GenericToggleAction(ILogger<ApToggleAction> logger, IFlightConnector flightConnector, IImageLogic imageLogic)
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

        private void setValues(JObject settings)
        {
            string newHeader = settings.Value<string>("Header");
            TOGGLE_EVENT? newToggleEvent = GetEventValue(settings.Value<string>("ToggleValue"));
            TOGGLE_VALUE? newFeedbackValue = GetValueValue(settings.Value<string>("FeedbackValue"));
            TOGGLE_VALUE? newDisplayValue = GetValueValue(settings.Value<string>("DisplayValue"));

            if (newFeedbackValue != feedbackValue || newDisplayValue != displayValue)
            {
                DeRegisterValues();
            }

            header = newHeader;
            toggleEvent = newToggleEvent;
            feedbackValue = newFeedbackValue;
            displayValue = newDisplayValue;

            RegisterValues();
        }

        private TOGGLE_EVENT? GetEventValue(string value)
        {
            if (value == null)
            {
                return null;
            }

            TOGGLE_EVENT result;
            if (Enum.TryParse(value, true, out result))
            {
                return result;
            }

            return null;
        }

        private TOGGLE_VALUE? GetValueValue(string value)
        {
            if (value == null)
            {
                return null;
            }

            TOGGLE_VALUE result;
            if (Enum.TryParse(value.Replace(":", "__").Replace(" ", "_"), true, out result))
            {
                return result;
            }

            return null;
        }

        private async void FlightConnector_GenericValuesUpdated(object sender, ToggleValueUpdatedEventArgs e)
        {
            bool isUpdated = false;

            if (feedbackValue.HasValue && e.GenericValueStatus.ContainsKey(feedbackValue.Value))
            {
                bool newStatus = e.GenericValueStatus[feedbackValue.Value] != "0";
                isUpdated = newStatus != currentStatus;
                currentStatus = newStatus;
            }
            if (displayValue.HasValue && e.GenericValueStatus.ContainsKey(displayValue.Value))
            {
                string newValue = e.GenericValueStatus[displayValue.Value];
                isUpdated |= newValue != currentValue;
                currentValue = newValue;
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

        protected override Task OnSendToPlugin(ActionEventArgs<JObject> args)
        {
            setValues(args.Payload);
            _= UpdateImage();
            return Task.CompletedTask;
        }

        private void RegisterValues()
        {
            if (toggleEvent.HasValue) flightConnector.RegisterToggleEvent(toggleEvent.Value);
            if (feedbackValue.HasValue) flightConnector.RegisterSimValue(feedbackValue.Value);
            if (displayValue.HasValue) flightConnector.RegisterSimValue(displayValue.Value);
        }

        private void DeRegisterValues()
        {
            if (feedbackValue.HasValue) flightConnector.DeRegisterSimValue(feedbackValue.Value);
            if (displayValue.HasValue) flightConnector.DeRegisterSimValue(displayValue.Value);
            currentValue = null;
        }

        protected override Task OnKeyDown(ActionEventArgs<KeyPayload> args)
        {
            if (toggleEvent.HasValue) flightConnector.Toggle(toggleEvent.Value);
            return Task.CompletedTask;
        }

        private async Task UpdateImage()
        {
            await SetImageAsync(imageLogic.GetImage(header, currentStatus, currentValue));
        }
    }
}

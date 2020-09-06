using FlightStreamDeck.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SharpDeck;
using SharpDeck.Events.Received;
using System;
using System.Threading.Tasks;

namespace FlightStreamDeck.Logics.Actions
{
    public class GenericToggleAction : StreamDeckAction
    {
        private readonly ILogger<ApToggleAction> logger;
        private readonly IFlightConnector flightConnector;
        private readonly IImageLogic imageLogic;

        private string header = "";
        private TOGGLE_EVENT? toggleEvent = null;
        private TOGGLE_VALUE? feedbackValue = null;
        private TOGGLE_VALUE? displayValue = null;
        private string alternateOnImageLocation = null;
        private string alternateOffImageLocation = null;

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
            TOGGLE_EVENT? newToggleEvent = Helpers.GetEventValue(settings.Value<string>("ToggleValue"));
            TOGGLE_VALUE? newFeedbackValue = Helpers.GetValueValue(settings.Value<string>("FeedbackValue"));
            TOGGLE_VALUE? newDisplayValue = Helpers.GetValueValue(settings.Value<string>("DisplayValue"));
            string newAltOnImage = settings.Value<string>("OverrideOnImageValue");
            string newAltOffImage = settings.Value<string>("OverrideOffImageValue");

            if (newFeedbackValue != feedbackValue || newDisplayValue != displayValue)
            {
                DeRegisterValues();
            }

            header = newHeader;
            toggleEvent = newToggleEvent;
            feedbackValue = newFeedbackValue;
            displayValue = newDisplayValue;
            alternateOnImageLocation = newAltOnImage;
            alternateOffImageLocation = newAltOffImage;

            RegisterValues();
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
            await SetImageAsync(imageLogic.GetImage(header, currentStatus, alternateOnImageLocation, alternateOffImageLocation, currentValue));
        }
    }
}

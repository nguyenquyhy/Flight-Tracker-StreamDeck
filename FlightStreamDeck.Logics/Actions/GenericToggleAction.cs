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
        private TOGGLE_VALUE? feedbackComparisonValue = null;
        private TOGGLE_VALUE? displayValue = null;
        private string feedbackComparisonStringValue = null;
        private string alternateOnImageLocation = null;
        private string alternateOffImageLocation = null;


        private string currentValue = string.Empty;
        private bool currentStatus = false;
        private string currentFeedbackValue = string.Empty;
        private string comparisonFeedbackValue = string.Empty;
        private string feedbackComparisonOperator = string.Empty;

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
            string newAltOnImage = settings.Value<string>("OverrideOnImageValue");
            string newAltOffImage = settings.Value<string>("OverrideOffImageValue");
            TOGGLE_EVENT? newToggleEvent = Helpers.GetEventValue(settings.Value<string>("ToggleValue"));
            TOGGLE_VALUE? newDisplayValue = Helpers.GetValueValue(settings.Value<string>("DisplayValue"));

            Tuple<TOGGLE_VALUE?, TOGGLE_VALUE?, string, string> comparisonFeedbackValueTuple =
                Helpers.GetValueValueComaprison(settings.Value<string>("FeedbackValue"));
            TOGGLE_VALUE? newFeedbackValue = comparisonFeedbackValueTuple.Item1;
            TOGGLE_VALUE? newFeedbackComparisonValue = comparisonFeedbackValueTuple.Item2;
            string newFeedbackComparisonStringValue = comparisonFeedbackValueTuple.Item3;
            string newFeedbackComparisonOperator = comparisonFeedbackValueTuple.Item4;                

            if (
                newFeedbackValue != feedbackValue || 
                newDisplayValue != displayValue || 
                newFeedbackComparisonValue != feedbackComparisonValue
            )
            {
                DeRegisterValues();
            }

            header = newHeader;
            toggleEvent = newToggleEvent;
            feedbackValue = newFeedbackValue;
            displayValue = newDisplayValue;
            feedbackComparisonValue = newFeedbackComparisonValue;
            feedbackComparisonStringValue = newFeedbackComparisonStringValue;
            feedbackComparisonOperator = newFeedbackComparisonOperator;
            alternateOnImageLocation = newAltOnImage;
            alternateOffImageLocation = newAltOffImage;

            //wipe stored local values so image updates accordingly
            currentFeedbackValue = string.Empty;
            comparisonFeedbackValue = string.Empty;

        RegisterValues();
        }

        private async void FlightConnector_GenericValuesUpdated(object sender, ToggleValueUpdatedEventArgs e)
        {
            if (StreamDeck == null) return;

            bool isUpdated = false;
            string newCurrentFeedbackValue = string.Empty;
            if (feedbackValue.HasValue && e.GenericValueStatus.ContainsKey(feedbackValue.Value))
            {
                newCurrentFeedbackValue = e.GenericValueStatus[feedbackValue.Value];
            }
            if (displayValue.HasValue && e.GenericValueStatus.ContainsKey(displayValue.Value))
            {
                string newValue = e.GenericValueStatus[displayValue.Value];
                isUpdated |= newValue != currentValue;
                currentValue = newValue;
            }
            if (feedbackComparisonValue.HasValue && e.GenericValueStatus.ContainsKey(feedbackComparisonValue.Value))
            {
                comparisonFeedbackValue = e.GenericValueStatus[feedbackComparisonValue.Value];
            }
            if (newCurrentFeedbackValue != currentFeedbackValue && !string.IsNullOrEmpty(newCurrentFeedbackValue) && (!string.IsNullOrEmpty(comparisonFeedbackValue) || !string.IsNullOrEmpty(feedbackComparisonStringValue)))
            {
                currentFeedbackValue = newCurrentFeedbackValue;
                bool newStatus = Helpers.CompareValues(currentFeedbackValue, !string.IsNullOrEmpty(comparisonFeedbackValue) ? comparisonFeedbackValue : feedbackComparisonStringValue, feedbackComparisonOperator);
                isUpdated |= newStatus != currentStatus;
                currentStatus = newStatus;
            }
            else if (!string.IsNullOrEmpty(currentFeedbackValue) && string.IsNullOrEmpty(comparisonFeedbackValue) && string.IsNullOrEmpty(feedbackComparisonStringValue))
            {
                bool newStatus = currentFeedbackValue != "0";
                isUpdated |= newStatus != currentStatus;
                currentStatus = newStatus;
                currentFeedbackValue = newCurrentFeedbackValue;
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
            if (feedbackComparisonValue.HasValue) flightConnector.RegisterSimValue(feedbackComparisonValue.Value);
        }

        private void DeRegisterValues()
        {
            if (feedbackValue.HasValue) flightConnector.DeRegisterSimValue(feedbackValue.Value);
            if (displayValue.HasValue) flightConnector.DeRegisterSimValue(displayValue.Value);
            if (feedbackComparisonValue.HasValue) flightConnector.DeRegisterSimValue(feedbackComparisonValue.Value);
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

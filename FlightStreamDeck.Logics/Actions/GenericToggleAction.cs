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
    public class GenericToggleSettings
    {
        public string Header { get; set; }
        public string ToggleValue { get; set; }
        public string FeedbackValue { get; set; }
        public string DisplayValue { get; set; }
        public string ImageOn { get; set; }
        public string ImageOff { get; set; }
    }

    [StreamDeckAction("tech.flighttracker.streamdeck.generic.toggle")]
    public class GenericToggleAction : StreamDeckAction<GenericToggleSettings>
    {
        private readonly ILogger<ApToggleAction> logger;
        private readonly IFlightConnector flightConnector;
        private readonly IImageLogic imageLogic;

        private GenericToggleSettings settings = null;

        private TOGGLE_EVENT? toggleEvent = null;
        private TOGGLE_VALUE? feedbackValue = null;
        private TOGGLE_VALUE? feedbackComparisonValue = null;
        private TOGGLE_VALUE? displayValue = null;
        private string feedbackComparisonStringValue = null;

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
            var settings = args.Payload.GetSettings<GenericToggleSettings>();
            InitializeSettings(settings);

            flightConnector.GenericValuesUpdated += FlightConnector_GenericValuesUpdated;

            RegisterValues();

            await UpdateImage();
        }

        private void InitializeSettings(GenericToggleSettings settings)
        {
            this.settings = settings;

            TOGGLE_EVENT? newToggleEvent = Helpers.GetEventValue(settings.ToggleValue);
            TOGGLE_VALUE? newDisplayValue = Helpers.GetValueValue(settings.DisplayValue);

            Tuple<TOGGLE_VALUE?, TOGGLE_VALUE?, string, string> comparisonFeedbackValueTuple =
                Helpers.GetValueValueComaprison(this.settings.FeedbackValue);
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

            toggleEvent = newToggleEvent;
            feedbackValue = newFeedbackValue;
            displayValue = newDisplayValue;
            feedbackComparisonValue = newFeedbackComparisonValue;
            feedbackComparisonStringValue = newFeedbackComparisonStringValue;
            feedbackComparisonOperator = newFeedbackComparisonOperator;

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

        protected override async Task OnSendToPlugin(ActionEventArgs<JObject> args)
        {
            InitializeSettings(args.Payload.ToObject<GenericToggleSettings>());
            await UpdateImage();
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
            await SetImageAsync(imageLogic.GetImage(settings.Header, currentStatus, currentValue, settings.ImageOn, settings.ImageOff));
        }
    }
}

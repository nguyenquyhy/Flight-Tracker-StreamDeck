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
            var settings = args.Payload.GetSettings<GenericToggleSettings>();
            InitializeSettings(settings);

            flightConnector.GenericValuesUpdated += FlightConnector_GenericValuesUpdated;

            RegisterValues();

            await UpdateImage();
        }

        private void InitializeSettings(GenericToggleSettings settings)
        {
            this.settings = settings;

            TOGGLE_EVENT? newToggleEvent = GetEventValue(settings.ToggleValue);
            TOGGLE_VALUE? newFeedbackValue = GetValueValue(settings.FeedbackValue);
            TOGGLE_VALUE? newDisplayValue = GetValueValue(settings.DisplayValue);

            if (newFeedbackValue != feedbackValue || newDisplayValue != displayValue)
            {
                DeRegisterValues();
            }

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

            if (Enum.TryParse(value, true, out TOGGLE_EVENT result))
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

            if (Enum.TryParse(value.Replace(":", "__").Replace(" ", "_"), true, out TOGGLE_VALUE result))
            {
                return result;
            }

            return null;
        }

        private async void FlightConnector_GenericValuesUpdated(object sender, ToggleValueUpdatedEventArgs e)
        {
            if (StreamDeck == null) return;

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
            await SetImageAsync(imageLogic.GetImage(settings.Header, currentStatus, currentValue, settings.ImageOn, settings.ImageOff));
        }
    }
}

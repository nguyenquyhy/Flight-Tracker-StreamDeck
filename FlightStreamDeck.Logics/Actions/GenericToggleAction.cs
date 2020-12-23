using FlightStreamDeck.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SharpDeck;
using SharpDeck.Events.Received;
using SharpDeck.Manifest;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlightStreamDeck.Logics.Actions
{
    public class GenericToggleSettings
    {
        public string Header { get; set; }
        public string ToggleValue { get; set; }
        public string ToggleValueData { get; set; }
        public string FeedbackValue { get; set; }
        public string DisplayValue { get; set; }
        public string ImageOn { get; set; }
        public string ImageOff { get; set; }
        public string DisplayValueUnit { get; set; }
        public string DisplayValuePrecision { get; set; }
    }

    [StreamDeckAction("tech.flighttracker.streamdeck.generic.toggle")]
    public class GenericToggleAction : StreamDeckAction<GenericToggleSettings>
    {
        private readonly ILogger<GenericToggleAction> logger;
        private readonly IFlightConnector flightConnector;
        private readonly IImageLogic imageLogic;
        private readonly IEvaluator evaluator;
        private readonly EnumConverter enumConverter;

        private GenericToggleSettings settings = null;

        private TOGGLE_EVENT? toggleEvent = null;
        private uint? toggleEventData = null;
        private IEnumerable<TOGGLE_VALUE> feedbackVariables = new List<TOGGLE_VALUE>();
        private IExpression expression;
        private TOGGLE_VALUE? displayValue = null;
        private ValueEntry displayValueEntry;

        private string currentValue = "";
        private bool currentStatus = false;

        public GenericToggleAction(ILogger<GenericToggleAction> logger, IFlightConnector flightConnector, IImageLogic imageLogic,
            IEvaluator evaluator, EnumConverter enumConverter)
        {
            this.logger = logger;
            this.flightConnector = flightConnector;
            this.imageLogic = imageLogic;
            this.evaluator = evaluator;
            this.enumConverter = enumConverter;
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

            TOGGLE_EVENT? newToggleEvent = enumConverter.GetEventEnum(settings.ToggleValue);
            if (uint.TryParse(settings.ToggleValueData, out var toggleParameter))
            {
                toggleEventData = toggleParameter;
            }
            (var newFeedbackVariables, var newExpression) = evaluator.Parse(settings.FeedbackValue);
            TOGGLE_VALUE? newDisplayValue = enumConverter.GetVariableEnum(settings.DisplayValue);

            short.TryParse(settings.DisplayValuePrecision, out short result);
            ValueEntry newDisplayValueEntry = new ValueEntry()
            {
                Unit = settings.DisplayValueUnit,
                Decimals = result
            };

            if (!newFeedbackVariables.SequenceEqual(feedbackVariables) || newDisplayValue != displayValue || Newtonsoft.Json.JsonConvert.SerializeObject(newDisplayValueEntry) != Newtonsoft.Json.JsonConvert.SerializeObject(displayValueEntry))
            {
                DeRegisterValues();
            }

            toggleEvent = newToggleEvent;
            feedbackVariables = newFeedbackVariables;
            expression = newExpression;
            displayValue = newDisplayValue;
            displayValueEntry = newDisplayValueEntry;

            RegisterValues();
        }

        private async void FlightConnector_GenericValuesUpdated(object sender, ToggleValueUpdatedEventArgs e)
        {
            if (StreamDeck == null) return;

            var newStatus = expression != null && evaluator.Evaluate(e.GenericValueStatus, expression);
            var isUpdated = newStatus != currentStatus;
            currentStatus = newStatus;

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
            foreach (var feedbackVariable in feedbackVariables) flightConnector.RegisterSimValue(feedbackVariable);
            if (displayValue.HasValue) flightConnector.RegisterSimValue(displayValue.Value, displayValueEntry);
        }
        
        private void DeRegisterValues()
        {
            foreach (var feedbackVariable in feedbackVariables) flightConnector.DeRegisterSimValue(feedbackVariable);
            if (displayValue.HasValue ) flightConnector.DeRegisterSimValue(displayValue.Value, displayValueEntry);
            currentValue = null;
        }

        protected override Task OnKeyDown(ActionEventArgs<KeyPayload> args)
        {
            if (toggleEvent.HasValue) flightConnector.Trigger(toggleEvent.Value, toggleEventData ?? 0);
            return Task.CompletedTask;
        }

        private async Task UpdateImage()
        {
            if (settings != null)
            {
                await SetImageAsync(imageLogic.GetImage(settings.Header, currentStatus, currentValue, settings.ImageOn, settings.ImageOff));
            }
        }
    }
}

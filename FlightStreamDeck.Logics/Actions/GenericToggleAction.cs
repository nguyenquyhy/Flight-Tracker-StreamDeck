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
    public class GenericToggleSettings
    {
        public string Header { get; set; }
        public string ToggleValue { get; set; }
        public string ToggleValueData { get; set; }
        public string FeedbackValue { get; set; }
        public string DisplayValue { get; set; }
        public string ImageOn { get; set; }
        public string ImageOff { get; set; }

        public uint? toggleEventDataUInt = null;
        public bool toggleParameterIsVariable = false;
        public string? toggleParameterVariable = null;

        public void ParseToggleValueData()
        {
            if (uint.TryParse(ToggleValueData, out uint result))
            {
                toggleEventDataUInt = result;
                toggleParameterIsVariable = false;
                toggleParameterVariable = null;
            }
            else
            {
                toggleEventDataUInt = null;
                toggleParameterIsVariable = !(ToggleValueData is null);
                toggleParameterVariable = ToggleValueData;
            }
        }
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
        public bool toggleParameterIsVariable = false;
        public string? toggleParameterVariable = null;
        private IEnumerable<TOGGLE_VALUE> feedbackVariables = new List<TOGGLE_VALUE>();
        private IExpression expression;
        private TOGGLE_VALUE? displayValue = null;
        private TOGGLE_VALUE? parameterVariable = null;
        private string currentParameterValue = "";

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
            settings.ParseToggleValueData();
            toggleEventData = settings.toggleEventDataUInt;
            toggleParameterIsVariable = settings.toggleParameterIsVariable;
            toggleParameterVariable = settings.toggleParameterVariable;

            (var newFeedbackVariables, var newExpression) = evaluator.Parse(settings.FeedbackValue);
            TOGGLE_VALUE? newDisplayValue = enumConverter.GetVariableEnum(settings.DisplayValue);

            TOGGLE_VALUE? newParameterVariable = null;
            if (toggleParameterIsVariable)
                newParameterVariable = enumConverter.GetVariableEnum(toggleParameterVariable);

            if (!newFeedbackVariables.SequenceEqual(feedbackVariables) || newDisplayValue != displayValue || newParameterVariable != parameterVariable)
            {
                DeRegisterValues();
            }

            toggleEvent = newToggleEvent;
            feedbackVariables = newFeedbackVariables;
            expression = newExpression;
            displayValue = newDisplayValue;
            parameterVariable = newParameterVariable;

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

            if (parameterVariable.HasValue && e.GenericValueStatus.ContainsKey(parameterVariable.Value))
            {
                string newValue = e.GenericValueStatus[parameterVariable.Value];
                currentParameterValue = newValue;
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
            if (displayValue.HasValue) flightConnector.RegisterSimValue(displayValue.Value);
            if (parameterVariable.HasValue) flightConnector.RegisterSimValue(parameterVariable.Value);
        }

        private void DeRegisterValues()
        {
            foreach (var feedbackVariable in feedbackVariables) flightConnector.DeRegisterSimValue(feedbackVariable);
            if (displayValue.HasValue) flightConnector.DeRegisterSimValue(displayValue.Value);
            if (parameterVariable.HasValue) flightConnector.DeRegisterSimValue(parameterVariable.Value);
            currentValue = null;
            currentParameterValue = null;
        }

        protected override Task OnKeyDown(ActionEventArgs<KeyPayload> args)
        {
            if (toggleEvent.HasValue)
            {
                if (toggleParameterIsVariable && !(currentParameterValue is null) && double.TryParse(currentParameterValue, out double parameterValue))
                {
                    uint parameterForSimConnect = Convert.ToUInt32(Math.Round(parameterValue));
                    flightConnector.Trigger(toggleEvent.Value, parameterForSimConnect);
                }
                else
                    flightConnector.Trigger(toggleEvent.Value, toggleEventData ?? 0);
            }
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

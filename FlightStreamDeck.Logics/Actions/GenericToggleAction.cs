using FlightStreamDeck.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SharpDeck;
using SharpDeck.Events.Received;
using SharpDeck.PropertyInspectors;
using System.Threading.Tasks;

namespace FlightStreamDeck.Logics.Actions
{
    public class GenericToggleAction : StreamDeckAction
    {
        private readonly ILogger<ApToggleAction> logger;
        private readonly IFlightConnector flightConnector;
        private readonly IImageLogic imageLogic;

        private string action;
        private string header = "YD";
        private TOGGLE_EVENT? toggleEvent = TOGGLE_EVENT.YAW_DAMPER_TOGGLE;
        private TOGGLE_VALUE? feedbackValue = TOGGLE_VALUE.AUTOPILOT_YAW_DAMPER;
        private TOGGLE_VALUE? displayValue = TOGGLE_VALUE.AUTOPILOT_MASTER;

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
            action = args.Action;
            flightConnector.GenericValuesUpdated += FlightConnector_GenericValuesUpdated;
            if (toggleEvent.HasValue) flightConnector.RegisterToggleEvent(toggleEvent.Value);
            if (feedbackValue.HasValue) flightConnector.RegisterSimValue(feedbackValue.Value);
            if (displayValue.HasValue) flightConnector.RegisterSimValue(displayValue.Value);

            await UpdateImage();
        }

        private async void FlightConnector_GenericValuesUpdated(object sender, ToggleValueUpdatedEventArgs e)
        {
            if (feedbackValue.HasValue && e.GenericValueStatus.ContainsKey(feedbackValue.Value))
            {
                currentStatus = e.GenericValueStatus[feedbackValue.Value].Contains('1');
            }
            if (displayValue.HasValue && e.GenericValueStatus.ContainsKey(displayValue.Value))
            {
                currentValue = e.GenericValueStatus[displayValue.Value];
            }

            await UpdateImage();
        }

        protected override Task OnWillDisappear(ActionEventArgs<AppearancePayload> args)
        {
            flightConnector.GenericValuesUpdated -= FlightConnector_GenericValuesUpdated;

            if (feedbackValue.HasValue) flightConnector.DeRegisterSimValue(feedbackValue.Value);
            if (displayValue.HasValue) flightConnector.DeRegisterSimValue(displayValue.Value);

            return Task.CompletedTask;
        }

        protected override Task OnSendToPlugin(ActionEventArgs<JObject> args)
        {
            logger.LogInformation("Received PI stuff {1}", args.Payload.ToString());
            return Task.CompletedTask;
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

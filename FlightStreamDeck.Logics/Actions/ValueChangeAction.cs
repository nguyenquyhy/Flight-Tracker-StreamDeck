using Microsoft.Extensions.Logging;
using SharpDeck;
using SharpDeck.Events.Received;
using SharpDeck.Manifest;
using System.Threading.Tasks;

namespace FlightStreamDeck.Logics.Actions
{
    [StreamDeckAction("ValueChange", "tech.flighttracker.streamdeck.valueChange")]
    public class ValueChangeAction : StreamDeckAction
    {
        private readonly ILogger<ApMasterAction> logger;
        private readonly IFlightConnector flightConnector;
        private readonly IImageLogic imageLogic;

        private volatile bool isEnabled = false;
        private volatile int currentHeading = 0;

        public ValueChangeAction(ILogger<ApMasterAction> logger, IFlightConnector flightConnector, IImageLogic imageLogic)
        {
            this.logger = logger;
            this.flightConnector = flightConnector;
            this.imageLogic = imageLogic;
        }

        protected override async Task OnKeyDown(ActionEventArgs<KeyPayload> args)
        {
            var actions = args.Action.Split('.');

            if (actions.Length < 2)
            {
                return;
            }

            var valueToChange = actions[actions.Length - 2];
            bool isIncrease = actions[actions.Length - 1].Contains("inc");


            switch (valueToChange)
            {
                case "heading":
                    if (isIncrease) { flightConnector.ApHdgInc(); } else { flightConnector.ApHdgDec(); }
                    break;
            }

        }

        private async Task UpdateImage()
        {
            await SetImageAsync(imageLogic.GetImage("HDG", isEnabled, currentHeading.ToString()));
        }
    }
}

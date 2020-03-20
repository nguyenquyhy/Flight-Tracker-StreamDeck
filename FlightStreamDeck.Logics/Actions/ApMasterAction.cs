using Microsoft.Extensions.Logging;
using SharpDeck;
using SharpDeck.Events.Received;
using SharpDeck.Manifest;
using System.Threading.Tasks;

namespace FlightStreamDeck.Logics.Actions
{
    [StreamDeckAction("AP Master", "tech.flighttracker.streamdeck.master.activate")]
    public class ApMasterAction : StreamDeckAction
    {
        private readonly ILogger<ApMasterAction> logger;
        private readonly IFlightConnector flightConnector;
        private readonly IImageLogic imageLogic;

        public ApMasterAction(ILogger<ApMasterAction> logger, IFlightConnector flightConnector, IImageLogic imageLogic)
        {
            this.logger = logger;
            this.flightConnector = flightConnector;
            this.imageLogic = imageLogic;
        }

        protected override async Task OnKeyDown(ActionEventArgs<KeyPayload> args)
        {
            logger.LogInformation("Toggle AP Master. Current state: {state}.", args.Payload.State);

            if (args.Payload.State == 0)
            {
                // Turn on
                flightConnector.ApOn();
            }
            else
            {
                // Turn off
                flightConnector.ApOff();
            }
            var image = imageLogic.DrawText(args.Payload.State == 0 ? "Images/button_active.png" : "Images/button.png", "AP");
            await SetImageAsync(image);
        }
    }
}

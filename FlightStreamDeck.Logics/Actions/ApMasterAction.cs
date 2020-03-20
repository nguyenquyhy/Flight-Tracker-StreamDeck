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

        private volatile bool isEnabled = false;

        public ApMasterAction(ILogger<ApMasterAction> logger, IFlightConnector flightConnector, IImageLogic imageLogic)
        {
            this.logger = logger;
            this.flightConnector = flightConnector;
            this.imageLogic = imageLogic;
            this.flightConnector.AircraftStatusUpdated += FlightConnector_AircraftStatusUpdated;
        }

        private async void FlightConnector_AircraftStatusUpdated(object sender, AircraftStatusUpdatedEventArgs e)
        {

            if (e.AircraftStatus.IsAutopilotOn != isEnabled)
            {
                logger.LogInformation("Received AP update: {state}", e.AircraftStatus.IsAutopilotOn);
                isEnabled = e.AircraftStatus.IsAutopilotOn;
                await UpdateImage();
            }
        }

        protected override async Task OnKeyDown(ActionEventArgs<KeyPayload> args)
        {
            logger.LogInformation("Toggle AP Master. Current state: {state}.", isEnabled);

            if (!isEnabled)
            {
                // Turn on
                flightConnector.ApOn();
            }
            else
            {
                // Turn off
                flightConnector.ApOff();
            }
        }

        private async Task UpdateImage()
        {
            await SetImageAsync(imageLogic.GetImage("AP", isEnabled));
        }
    }
}

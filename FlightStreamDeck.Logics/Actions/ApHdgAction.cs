using Microsoft.Extensions.Logging;
using SharpDeck;
using SharpDeck.Events.Received;
using SharpDeck.Manifest;
using System.Threading.Tasks;

namespace FlightStreamDeck.Logics.Actions
{
    [StreamDeckAction("AP HDG", "tech.flighttracker.streamdeck.heading.activate")]
    public class ApHdgAction : StreamDeckAction
    {
        private readonly ILogger<ApMasterAction> logger;
        private readonly IFlightConnector flightConnector;
        private readonly IImageLogic imageLogic;

        private volatile bool isEnabled = false;
        private volatile int currentHeading = 0;

        public ApHdgAction(ILogger<ApMasterAction> logger, IFlightConnector flightConnector, IImageLogic imageLogic)
        {
            this.logger = logger;
            this.flightConnector = flightConnector;
            this.imageLogic = imageLogic;
            this.flightConnector.AircraftStatusUpdated += FlightConnector_AircraftStatusUpdated;
        }

        private async void FlightConnector_AircraftStatusUpdated(object sender, AircraftStatusUpdatedEventArgs e)
        {

            if (e.AircraftStatus.ApHeading != currentHeading || e.AircraftStatus.IsApHdgOn != isEnabled)
            {
                logger.LogInformation("Received HDG update: {state} {state2}", e.AircraftStatus.IsAutopilotOn, e.AircraftStatus.ApHeading);
                isEnabled = e.AircraftStatus.IsApHdgOn;
                currentHeading = (int)e.AircraftStatus.ApHeading;
                await UpdateImage();
            }
        }

        protected override async Task OnKeyDown(ActionEventArgs<KeyPayload> args)
        {
            logger.LogInformation("Toggle AP HDG. Current state: {state}.", isEnabled);
            flightConnector.ApHdgToggle();
        }

        private async Task UpdateImage()
        {
            await SetImageAsync(imageLogic.GetImage("HDG", isEnabled, currentHeading.ToString()));
        }
    }
}

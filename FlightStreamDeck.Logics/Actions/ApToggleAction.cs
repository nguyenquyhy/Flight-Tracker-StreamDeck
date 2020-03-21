using Microsoft.Extensions.Logging;
using SharpDeck;
using SharpDeck.Events.Received;
using System.Threading.Tasks;

namespace FlightStreamDeck.Logics.Actions
{
    public class ApToggleAction : StreamDeckAction
    {
        private readonly ILogger<ApMasterAction> logger;
        private readonly IFlightConnector flightConnector;
        private readonly IImageLogic imageLogic;

        private AircraftStatus status = null;
        private string action;

        public ApToggleAction(ILogger<ApMasterAction> logger, IFlightConnector flightConnector, IImageLogic imageLogic)
        {
            this.logger = logger;
            this.flightConnector = flightConnector;
            this.imageLogic = imageLogic;
        }

        private async void FlightConnector_AircraftStatusUpdated(object sender, AircraftStatusUpdatedEventArgs e)
        {
            var lastStatus = status;
            status = e.AircraftStatus;

            switch (action)
            {
                case "tech.flighttracker.streamdeck.heading.activate":
                    if (e.AircraftStatus.ApHeading != lastStatus?.ApHeading || e.AircraftStatus.IsApHdgOn != lastStatus?.IsApHdgOn)
                    {
                        logger.LogInformation("Received HDG update: {IsApHdgOn} {ApHeading}", e.AircraftStatus.IsApHdgOn, e.AircraftStatus.ApHeading);
                        await UpdateImage();
                    }
                    break;
                case "tech.flighttracker.streamdeck.altitude.activate":
                    if (e.AircraftStatus.ApAltitude != lastStatus?.ApAltitude || e.AircraftStatus.IsApAltOn != lastStatus?.IsApAltOn)
                    {
                        logger.LogInformation("Received ALT update: {IsApHdgOn} {ApHeading}", e.AircraftStatus.IsApAltOn, e.AircraftStatus.ApAltitude);
                        await UpdateImage();
                    }
                    break;
            }
        }

        protected override Task OnWillAppear(ActionEventArgs<AppearancePayload> args)
        {
            action = args.Action;

            this.flightConnector.AircraftStatusUpdated += FlightConnector_AircraftStatusUpdated;

            return Task.CompletedTask;
        }

        protected override Task OnWillDisappear(ActionEventArgs<AppearancePayload> args)
        {
            this.flightConnector.AircraftStatusUpdated -= FlightConnector_AircraftStatusUpdated;

            return base.OnWillDisappear(args);
        }

        protected override async Task OnKeyDown(ActionEventArgs<KeyPayload> args)
        {
            if (status != null)
            {
                switch (action)
                {
                    case "tech.flighttracker.streamdeck.heading.activate":
                        logger.LogInformation("Toggle AP HDG. Current state: {state}.", status?.IsApHdgOn);
                        flightConnector.ApHdgToggle();
                        break;
                    case "tech.flighttracker.streamdeck.altitude.activate":
                        logger.LogInformation("Toggle AP ALT. Current state: {state}.", status?.IsApAltOn);
                        flightConnector.ApAltToggle();
                        break;

                }
            }
        }

        private async Task UpdateImage()
        {
            var currentStatus = status;
            if (currentStatus != null)
            {
                switch (action)
                {
                    case "tech.flighttracker.streamdeck.heading.activate":
                        await SetImageAsync(imageLogic.GetImage("HDG", currentStatus.IsApHdgOn, currentStatus.ApHeading.ToString()));
                        break;

                    case "tech.flighttracker.streamdeck.altitude.activate":
                        await SetImageAsync(imageLogic.GetImage("ALT", currentStatus.IsApAltOn, currentStatus.ApAltitude.ToString()));
                        break;
                }
            }
        }
    }
}

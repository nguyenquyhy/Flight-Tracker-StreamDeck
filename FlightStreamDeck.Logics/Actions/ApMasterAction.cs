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

        public ApMasterAction(ILogger<ApMasterAction> logger)
        {
            this.logger = logger;
        }

        protected override async Task OnKeyDown(ActionEventArgs<KeyPayload> args)
        {
            logger.LogInformation("Toggle AP Master. Current state: {state}.", args.Payload.State);

            if (args.Payload.State == 0)
            {
                // Turn on
            }
            else
            {
                // Turn off
            }
        }
    }
}

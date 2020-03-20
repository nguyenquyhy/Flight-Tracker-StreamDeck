using Microsoft.Extensions.Logging;
using StreamDeckLib;
using StreamDeckLib.Messages;
using System.Threading.Tasks;

namespace FlightStreamDeck.Logics.Actions
{
    [ActionUuid(Uuid = "tech.flighttracker.streamdeck.master.activate")]
    public class ApMasterAction : BaseStreamDeckAction
    {
        public override async Task OnKeyDown(StreamDeckEventPayload args)
        {
            Logger.LogInformation("Toggle AP Master. Current state: {state}.", args.payload.state);

            if (args.payload.state == 0)
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

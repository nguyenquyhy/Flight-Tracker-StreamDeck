using SharpDeck;
using SharpDeck.Events.Received;
using System;
using System.Threading.Tasks;

namespace FlightStreamDeck.Logics.Actions
{
    public class EnterAction : StreamDeckAction
    {
        protected override async Task OnKeyDown(ActionEventArgs<KeyPayload> args)
        {
            var param = RegistrationParameters.Parse(Environment.GetCommandLineArgs()[1..]);
            await StreamDeck.SwitchToProfileAsync(param.PluginUUID, args.Device, null);
        }
    }
}

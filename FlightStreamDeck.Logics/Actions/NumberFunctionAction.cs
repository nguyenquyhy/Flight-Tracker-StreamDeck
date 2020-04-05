using SharpDeck;
using SharpDeck.Events.Received;
using System;
using System.Threading.Tasks;

namespace FlightStreamDeck.Logics.Actions
{
    public class NumberFunctionAction : StreamDeckAction
    {
        protected override async Task OnKeyDown(ActionEventArgs<KeyPayload> args)
        {
            var param = RegistrationParameters.Parse(Environment.GetCommandLineArgs()[1..]);
            switch (args.Action)
            {
                case "tech.flighttracker.streamdeck.number.enter":
                    if (DeckLogic.NumpadTcs != null)
                    {
                        DeckLogic.NumpadTcs.SetResult((DeckLogic.NumpadParams.Value, false));
                    }
                    await StreamDeck.SwitchToProfileAsync(param.PluginUUID, args.Device, null);
                    break;
                case "tech.flighttracker.streamdeck.number.cancel":
                    if (DeckLogic.NumpadTcs != null)
                    {
                        DeckLogic.NumpadTcs.SetResult((null, false));
                    }
                    await StreamDeck.SwitchToProfileAsync(param.PluginUUID, args.Device, null);
                    break;
                case "tech.flighttracker.streamdeck.number.transfer":
                    if (DeckLogic.NumpadTcs != null)
                    {
                        DeckLogic.NumpadTcs.SetResult((DeckLogic.NumpadParams.Value, true));
                    }
                    await StreamDeck.SwitchToProfileAsync(param.PluginUUID, args.Device, null);
                    break;
                case "tech.flighttracker.streamdeck.number.backspace":
                    if (DeckLogic.NumpadParams.Value.Length > 1)
                    {
                        DeckLogic.NumpadParams.Value = DeckLogic.NumpadParams.Value[..^1];
                    }
                    break;
            }

        }
    }
}

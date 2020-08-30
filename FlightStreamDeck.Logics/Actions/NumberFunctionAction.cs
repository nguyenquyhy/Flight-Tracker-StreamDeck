using SharpDeck;
using SharpDeck.Events.Received;
using SixLabors.ImageSharp.Formats.Bmp;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FlightStreamDeck.Logics.Actions
{
    public class NumberFunctionAction : StreamDeckAction
    {
        private readonly IImageLogic imageLogic;
        private Dictionary<string, string> buttonText = new Dictionary<string, string>();

        public NumberFunctionAction(IImageLogic imageLogic)
        {
            this.imageLogic = imageLogic;
            buttonText.Add("tech.flighttracker.streamdeck.number.enter", "Enter");
            buttonText.Add("tech.flighttracker.streamdeck.number.cancel", "Cancel");
            buttonText.Add("tech.flighttracker.streamdeck.number.transfer", "Xfer");
            buttonText.Add("tech.flighttracker.streamdeck.number.backspace", "Bksp");
        }

        protected override async Task OnKeyDown(ActionEventArgs<KeyPayload> args)
        {
            switch (args.Action)
            {
                case "tech.flighttracker.streamdeck.number.enter":
                    await handleErroredValueAction(
                        args, 
                        () => { return DeckLogic.NumpadTcs != null; }, 
                        async (ActionEventArgs<KeyPayload> args) => { await SetImageAsync(imageLogic.GetNavComActionLabel(buttonText[args.Action], true)); },
                        async (ActionEventArgs<KeyPayload> args) => { await Task.Run(() => { DeckLogic.NumpadTcs.SetResult((DeckLogic.NumpadParams.Value, false)); }); }
                    );
                    break;
                case "tech.flighttracker.streamdeck.number.cancel":
                    await handleErroredValueAction(
                        args,
                        () => { return DeckLogic.NumpadTcs != null; },
                        async (ActionEventArgs<KeyPayload> args) => { await Task.Run(() => { }); },
                        async (ActionEventArgs<KeyPayload> args) => { await Task.Run(() => { DeckLogic.NumpadTcs.SetResult((null, false)); }); },
                        skipMinMaxCheck: true
                    );
                    break;
                case "tech.flighttracker.streamdeck.number.transfer":
                    await handleErroredValueAction(
                        args,
                        () => { return DeckLogic.NumpadTcs != null; },
                        async (ActionEventArgs<KeyPayload> args) => { await SetImageAsync(imageLogic.GetNavComActionLabel(buttonText[args.Action], true)); },
                        async (ActionEventArgs<KeyPayload> args) => { await Task.Run(() => { DeckLogic.NumpadTcs.SetResult((DeckLogic.NumpadParams.Value, true)); }); }
                    );
                    break;
                case "tech.flighttracker.streamdeck.number.backspace":
                    await handleErroredValueAction(
                        args,
                        () => { return DeckLogic.NumpadParams.ValueUnpadded.Length > 1; },
                        async (ActionEventArgs<KeyPayload> args) => { await SetImageAsync(imageLogic.GetNavComActionLabel(buttonText[args.Action], true)); },
                        async (ActionEventArgs<KeyPayload> args) => { await Task.Run(() => { DeckLogic.NumpadParams.Value = DeckLogic.NumpadParams.ValueUnpadded[..^1]; }); },
                        skipMinMaxCheck: true,
                        fireProfileSwitch: false
                    );
                    break;
            }

        }

        protected override async Task OnWillAppear(ActionEventArgs<AppearancePayload> args)
        {
            await SetImageAsync(imageLogic.GetNavComActionLabel(buttonText[args.Action]));
        }

        private async Task handleErroredValueAction<T>(
            ActionEventArgs<T> args,
            Func<bool> validationCheck,
            Func<ActionEventArgs<T>, Task> notValidSub,
            Func<ActionEventArgs<T>, Task> validSub,
            bool skipMinMaxCheck = false,
            bool fireProfileSwitch = true
        ) {
            bool valid = validationCheck();
            bool minMaxRegexValid = DeckLogic.NumpadParams.MinMaxRegexValid(skipMinMaxCheck);
            var param = RegistrationParameters.Parse(Environment.GetCommandLineArgs()[1..]); 

            if (valid && minMaxRegexValid)
            {
                await validSub(args);
                if (fireProfileSwitch) await StreamDeck.SwitchToProfileAsync(param.PluginUUID, args.Device, null);
            } else
            {
                //usually setting label to red
                await notValidSub(args);

                //set back to white after 2 seconds
                await Task.Run(async () => {
                    await Task.Delay(500);
                    await SetImageAsync(imageLogic.GetNavComActionLabel(buttonText[args.Action], false));
                });
            }
        }
    }
}

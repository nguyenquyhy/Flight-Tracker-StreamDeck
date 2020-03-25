using Newtonsoft.Json.Linq;
using SharpDeck;
using SharpDeck.Events.Received;
using System;
using System.Threading.Tasks;
using System.Timers;

namespace FlightStreamDeck.Logics.Actions
{
    class NavComAction : StreamDeckAction
    {
        private readonly IImageLogic imageLogic;
        private readonly Timer timer;
        private string device;

        public NavComAction(IImageLogic imageLogic)
        {
            this.imageLogic = imageLogic;
            timer = new Timer { Interval = 1000 };
            timer.Elapsed += Timer_Elapsed;
        }

        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timer.Stop();

            var param = RegistrationParameters.Parse(Environment.GetCommandLineArgs()[1..]);
            await StreamDeck.SwitchToProfileAsync(param.PluginUUID, device, "Profiles/Numpad");
        }

        protected override async Task OnWillAppear(ActionEventArgs<AppearancePayload> args)
        {
            device = args.Device;

            var type = args.Payload.Settings.Value<string>("Type");
            await SetImageAsync(imageLogic.GetNavComImage(type));
        }

        protected override Task OnKeyDown(ActionEventArgs<KeyPayload> args)
        {
            timer.Start();
            return Task.CompletedTask;
        }

        protected override Task OnKeyUp(ActionEventArgs<KeyPayload> args)
        {
            if (timer.Enabled)
            {
                timer.Stop();
                // Transfer
            }
            return Task.CompletedTask;
        }

        protected override async Task OnSendToPlugin(ActionEventArgs<JObject> args)
        {
            var type = args.Payload.Value<string>("Type");
            await SetImageAsync(imageLogic.GetNavComImage(type));
        }
    }
}

using SharpDeck;
using SharpDeck.Events.Received;
using SharpDeck.Manifest;
using System.Threading.Tasks;
using System.Timers;

namespace FlightStreamDeck.Logics.Actions
{
    [StreamDeckAction("tech.flighttracker.streamdeck.number.display")]
    public class NumberDisplayAction : BaseAction
    {
        private readonly Timer timer;
        private readonly IImageLogic imageLogic;
        private string lastValue;

        public NumberDisplayAction(IImageLogic imageLogic)
        {
            timer = new Timer { Interval = 100 };
            timer.Elapsed += Timer_Elapsed;
            this.imageLogic = imageLogic;
        }

        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (lastValue != DeckLogic.NumpadParams.Value)
            {
                lastValue = DeckLogic.NumpadParams.Value;

                var value = DeckLogic.NumpadParams.Value;
                var decIndex = DeckLogic.NumpadParams.Mask.IndexOf(".");

                if (value.Length > decIndex && decIndex >= 0)
                {
                    value = value.Insert(decIndex, ".");
                }

                await SetImageSafeAsync(imageLogic.GetNavComImage(DeckLogic.NumpadParams.Type, true, "", value, imageOnFilePath: DeckLogic.NumpadParams.ImageBackgroundFilePath, imageOnBytes: DeckLogic.NumpadParams.ImageBackground_base64));
            }
        }

        protected override async Task OnWillAppear(ActionEventArgs<AppearancePayload> args)
        {
            if (DeckLogic.NumpadParams == null)
            {
                // User open the profile on their own
                await ShowAlertAsync();
            }
            else
            {
                lastValue = null;
                timer.Start();
            }
        }

        protected override Task OnWillDisappear(ActionEventArgs<AppearancePayload> args)
        {
            timer.Stop();
            return Task.CompletedTask;
        }
    }
}

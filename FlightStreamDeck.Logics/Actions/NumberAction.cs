using SharpDeck;
using SharpDeck.Events.Received;
using System.Threading.Tasks;

namespace FlightStreamDeck.Logics.Actions
{
    public class NumberAction : StreamDeckAction
    {
        private readonly IImageLogic imageLogic;

        public NumberAction(IImageLogic imageLogic)
        {
            this.imageLogic = imageLogic;
        }

        protected override async Task OnWillAppear(ActionEventArgs<AppearancePayload> args)
        {
            var tokens = args.Action.Split(".");
            var number = int.Parse(tokens[^1]);
            await SetImageAsync(imageLogic.GetNumberImage(number));
        }
    }
}

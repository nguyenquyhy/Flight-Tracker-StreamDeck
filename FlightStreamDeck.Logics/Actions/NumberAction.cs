using SharpDeck;
using SharpDeck.Events.Received;
using SharpDeck.Manifest;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace FlightStreamDeck.Logics.Actions
{
    #region

    [StreamDeckAction("tech.flighttracker.streamdeck.number.0")]
    public class Number0Action : NumberAction { public Number0Action(IImageLogic imageLogic) : base(imageLogic) { } }
    [StreamDeckAction("tech.flighttracker.streamdeck.number.1")]
    public class Number1Action : NumberAction { public Number1Action(IImageLogic imageLogic) : base(imageLogic) { } }
    [StreamDeckAction("tech.flighttracker.streamdeck.number.2")]
    public class Number2Action : NumberAction { public Number2Action(IImageLogic imageLogic) : base(imageLogic) { } }
    [StreamDeckAction("tech.flighttracker.streamdeck.number.3")]
    public class Number3Action : NumberAction { public Number3Action(IImageLogic imageLogic) : base(imageLogic) { } }
    [StreamDeckAction("tech.flighttracker.streamdeck.number.4")]
    public class Number4Action : NumberAction { public Number4Action(IImageLogic imageLogic) : base(imageLogic) { } }
    [StreamDeckAction("tech.flighttracker.streamdeck.number.5")]
    public class Number5Action : NumberAction { public Number5Action(IImageLogic imageLogic) : base(imageLogic) { } }
    [StreamDeckAction("tech.flighttracker.streamdeck.number.6")]
    public class Number6Action : NumberAction { public Number6Action(IImageLogic imageLogic) : base(imageLogic) { } }
    [StreamDeckAction("tech.flighttracker.streamdeck.number.7")]
    public class Number7Action : NumberAction { public Number7Action(IImageLogic imageLogic) : base(imageLogic) { } }
    [StreamDeckAction("tech.flighttracker.streamdeck.number.8")]
    public class Number8Action : NumberAction
    {
        public Number8Action(IImageLogic imageLogic) : base(imageLogic) { }

        protected override async Task OnWillAppear(ActionEventArgs<AppearancePayload> args)
        {
            if (DeckLogic.NumpadParams?.Type == "XPDR")
            {
                try
                {
                    await SetImageAsync(null);
                }
                catch (WebSocketException)
                {
                    // Ignore as we can't really do anything here
                }
            }
            else
            {
                await base.OnWillAppear(args);
            }
        }
    }
    [StreamDeckAction("tech.flighttracker.streamdeck.number.9")]
    public class Number9Action : NumberAction
    {
        public Number9Action(IImageLogic imageLogic) : base(imageLogic) { }

        protected override async Task OnWillAppear(ActionEventArgs<AppearancePayload> args)
        {
            if (DeckLogic.NumpadParams?.Type == "XPDR")
            {
                try
                {
                    await SetImageAsync(null);
                }
                catch (WebSocketException)
                {
                    // Ignore as we can't really do anything here
                }
            }
            else
            {
                await base.OnWillAppear(args);
            }
        }
    }

    #endregion

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
            try
            {
                await SetImageAsync(imageLogic.GetNumberImage(number));
            }
            catch (WebSocketException)
            {
                // Ignore as we can't really do anything here
            }
        }

        protected override Task OnKeyDown(ActionEventArgs<KeyPayload> args)
        {
            if (DeckLogic.NumpadParams != null)
            {
                var tokens = args.Action.Split(".");
                var number = int.Parse(tokens[^1]);

                if (DeckLogic.NumpadParams.Type == "XPDR" && number > 7) return Task.CompletedTask;

                if (DeckLogic.NumpadParams.Value.Length < DeckLogic.NumpadParams.MinPattern.Length)
                {
                    DeckLogic.NumpadParams.Value += number.ToString();
                }
            }

            return Task.CompletedTask;
        }
    }
}

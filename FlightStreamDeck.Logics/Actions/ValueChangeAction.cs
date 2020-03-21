using Microsoft.Extensions.Logging;
using SharpDeck;
using SharpDeck.Events.Received;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Timers;

namespace FlightStreamDeck.Logics.Actions
{
    public class ValueChangeAction : StreamDeckAction
    {
        private readonly ILogger<ValueChangeAction> logger;
        private readonly IFlightConnector flightConnector;

        private Timer timer;
        private string action;
        private Stopwatch stopwatch = new Stopwatch();

        public ValueChangeAction(ILogger<ValueChangeAction> logger, IFlightConnector flightConnector)
        {
            this.logger = logger;
            this.flightConnector = flightConnector;
            timer = new Timer { Interval = 300 };
            timer.Elapsed += Timer_Elapsed;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Process();
        }

        private void Process()
        {
            if (string.IsNullOrEmpty(action)) return;

            var actions = action.Split('.');

            if (actions.Length < 2)
            {
                return;
            }

            var valueToChange = actions[^2];
            var change = actions[^1];
            var increment = stopwatch.ElapsedMilliseconds < 2000 ? 1 : 10;

            switch (valueToChange)
            {
                case "heading":
                        //var newHeading = currentValue + change switch
                        //{
                        //    "increase" => increment,
                        //    "decrease" => -increment,
                        //    _ => 0
                        //};
                        //flightConnector.ApHdgSet((uint)(newHeading + 360) % 360);
                        // Workaround
                        switch (change)
                        {
                            case "increase":
                                for (int i = 0; i < increment; i++)
                                {
                                    flightConnector.ApHdgInc();
                                }
                                break;
                            case "decrease":
                                for (int i = 0; i < increment; i++)
                                {
                                    flightConnector.ApHdgDec();
                                }
                                break;
                        }

                    break;

                case "altitude":
                    increment = 1;
                    switch (change)
                    {
                        case "increase":
                            for (int i = 0; i < increment; i++)
                            {
                                flightConnector.ApAltInc();
                            }
                            break;
                        case "decrease":
                            for (int i = 0; i < increment; i++)
                            {
                                flightConnector.ApAltDec();
                            }
                            break;
                    }

                    break;

            }
        }

        protected override Task OnKeyDown(ActionEventArgs<KeyPayload> args)
        {
            action = args.Action;
            stopwatch.Restart();
            Process();
            timer.Start();
            return Task.CompletedTask;

        }

        protected override Task OnKeyUp(ActionEventArgs<KeyPayload> args)
        {
            action = null;
            timer.Stop();
            stopwatch.Stop();
            return Task.CompletedTask;
        }
    }
}

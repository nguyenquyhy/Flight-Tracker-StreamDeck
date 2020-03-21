using Microsoft.Extensions.Logging;
using SharpDeck;
using SharpDeck.Events.Received;
using SharpDeck.Manifest;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Timers;

namespace FlightStreamDeck.Logics.Actions
{
    [StreamDeckAction("ValueChange", "tech.flighttracker.streamdeck.valueChange")]
    public class ValueChangeAction : StreamDeckAction
    {
        private readonly ILogger<ApMasterAction> logger;
        private readonly IFlightConnector flightConnector;

        private int currentValue;
        private Timer timer;
        private string action;
        private Stopwatch stopwatch = new Stopwatch();

        public ValueChangeAction(ILogger<ApMasterAction> logger, IFlightConnector flightConnector)
        {
            this.logger = logger;
            this.flightConnector = flightConnector;
            timer = new Timer { Interval = 300 };
            timer.Elapsed += Timer_Elapsed;

            this.flightConnector.AircraftStatusUpdated += FlightConnector_AircraftStatusUpdated;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Process();
        }

        private void FlightConnector_AircraftStatusUpdated(object sender, AircraftStatusUpdatedEventArgs e)
        {
            currentValue = e.AircraftStatus.ApHeading;
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

            switch (valueToChange)
            {
                case "heading":
                    var increment = stopwatch.ElapsedMilliseconds < 2000 ? 1 : 10;
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
            }
        }

        protected override Task OnKeyDown(ActionEventArgs<KeyPayload> args)
        {
            action = args.Action;
            Process();
            stopwatch.Restart();
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

using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SharpDeck;
using SharpDeck.Events.Received;
using SharpDeck.Manifest;
using System;
using System.Threading.Tasks;
using System.Timers;

namespace FlightStreamDeck.Logics.Actions
{
    #region Action Registration

    [StreamDeckAction("tech.flighttracker.streamdeck.preset.increase")]
    public class ValueIncreaseAction : PresetChangeAction
    {
        public ValueIncreaseAction(ILogger<ValueIncreaseAction> logger, IFlightConnector flightConnector)
            : base(logger, flightConnector) { }
    }
    [StreamDeckAction("tech.flighttracker.streamdeck.preset.decrease")]
    public class ValueDecreaseAction : PresetChangeAction
    {
        public ValueDecreaseAction(ILogger<ValueDecreaseAction> logger, IFlightConnector flightConnector)
            : base(logger, flightConnector) { }
    }

    #endregion

    public class ValueChangeFunction
    {
        public const string Heading = "Heading";
        public const string Altitude = "Altitude";
        public const string VerticalSpeed = "VerticalSpeed";
        public const string AirSpeed = "AirSpeed";
        public const string VerticalSpeedAirSpeed = "VerticalSpeedAirSpeed";
    }

    public class ValueChangeSettings
    {
        public string Type { get; set; }
    }

    public abstract class PresetChangeAction : StreamDeckAction
    {
        private readonly ILogger logger;
        private readonly IFlightConnector flightConnector;

        private Timer timer;
        private string action;
        private bool timerHaveTick = false;
        private uint? originalValue = null;
        private AircraftStatus status;
        private ValueChangeSettings settings;

        public PresetChangeAction(ILogger logger, IFlightConnector flightConnector)
        {
            this.logger = logger;
            this.flightConnector = flightConnector;
            timer = new Timer { Interval = 400 };
            timer.Elapsed += Timer_Elapsed;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timerHaveTick = true;
            Process(false);
        }

        private void Process(bool isUp)
        {
            if (string.IsNullOrEmpty(action) || status == null) return;
            if (isUp && timerHaveTick) return;

            var actions = action.Split('.');

            if (actions.Length < 2)
            {
                return;
            }

            var change = actions[^1];
            var sign = change == "increase" ? 1 : -1;
            var increment = isUp ? 1 : 10;

            var buttonType = settings?.Type;
            if (string.IsNullOrWhiteSpace(buttonType))
            {
                return;
            }

            if (originalValue == null) originalValue = buttonType switch
            {
                ValueChangeFunction.Heading => (uint)status.ApHeading,
                ValueChangeFunction.Altitude => (uint)status.ApAltitude,
                ValueChangeFunction.VerticalSpeed => (uint)status.ApVs,
                ValueChangeFunction.AirSpeed => (uint)status.IndicatedAirSpeed,
                ValueChangeFunction.VerticalSpeedAirSpeed => status.IsApFlcOn ? (uint)status.IndicatedAirSpeed : (uint)status.ApVs,
                _ => throw new NotImplementedException($"Value type: {buttonType}")
            };

            switch (buttonType)
            {
                case ValueChangeFunction.Heading:
                    originalValue = (uint)(originalValue + 360 + sign * increment) % 360;
                    flightConnector.ApHdgSet(originalValue.Value);
                    break;

                case ValueChangeFunction.Altitude:
                    originalValue = (uint)(originalValue + 100 * sign * increment);
                    flightConnector.ApAltSet(originalValue.Value);
                    break;

                case ValueChangeFunction.VerticalSpeed:
                    ChangeVerticalSpeed(sign);
                    break;

                case ValueChangeFunction.AirSpeed:
                    ChangeAirSpeed(sign);
                    break;

                case ValueChangeFunction.VerticalSpeedAirSpeed:
                    if (status.IsApFlcOn)
                    {
                        ChangeAirSpeed(sign);
                    }
                    else
                    {
                        ChangeVerticalSpeed(sign);
                    }
                    break;

            }
        }

        private void FlightConnector_AircraftStatusUpdated(object sender, AircraftStatusUpdatedEventArgs e)
        {
            status = e.AircraftStatus;
        }

        protected override Task OnKeyDown(ActionEventArgs<KeyPayload> args)
        {
            action = args.Action;
            timerHaveTick = false;
            timer.Start();
            return Task.CompletedTask;

        }

        protected override Task OnKeyUp(ActionEventArgs<KeyPayload> args)
        {
            timer.Stop();
            Process(true);
            action = null;
            originalValue = null;
            timerHaveTick = false;
            return Task.CompletedTask;
        }

        protected override Task OnWillAppear(ActionEventArgs<AppearancePayload> args)
        {
            settings = args.Payload.GetSettings<ValueChangeSettings>();
            status = null;
            this.flightConnector.AircraftStatusUpdated += FlightConnector_AircraftStatusUpdated;
            return Task.CompletedTask;
        }

        protected override Task OnWillDisappear(ActionEventArgs<AppearancePayload> args)
        {
            status = null;
            this.flightConnector.AircraftStatusUpdated -= FlightConnector_AircraftStatusUpdated;
            return Task.CompletedTask;
        }

        protected override Task OnSendToPlugin(ActionEventArgs<JObject> args)
        {
            this.settings = args.Payload.ToObject<ValueChangeSettings>();
            return Task.CompletedTask;
        }

        private void ChangeVerticalSpeed(int sign)
        {
            originalValue = (uint)(originalValue + 100 * sign);
            flightConnector.ApVsSet(originalValue.Value);
        }

        private void ChangeAirSpeed(int sign)
        {
            if (sign == 1)
            {
                flightConnector.ApAirSpeedInc();
            }
            else
            {
                flightConnector.ApAirSpeedDec();
            }
        }
    }
}

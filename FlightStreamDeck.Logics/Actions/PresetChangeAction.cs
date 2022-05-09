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
        public ValueIncreaseAction(ILogger<ValueIncreaseAction> logger, IFlightConnector flightConnector, IEventRegistrar eventRegistrar, IEventDispatcher eventDispatcher)
            : base(logger, flightConnector, eventRegistrar, eventDispatcher) { }
    }
    [StreamDeckAction("tech.flighttracker.streamdeck.preset.decrease")]
    public class ValueDecreaseAction : PresetChangeAction
    {
        public ValueDecreaseAction(ILogger<ValueDecreaseAction> logger, IFlightConnector flightConnector, IEventRegistrar eventRegistrar, IEventDispatcher eventDispatcher)
            : base(logger, flightConnector, eventRegistrar, eventDispatcher) { }
    }

    #endregion

    public class ValueChangeFunction
    {
        public const string Heading = "Heading";
        public const string Altitude = "Altitude";
        public const string VerticalSpeed = "VerticalSpeed";
        public const string AirSpeed = "AirSpeed";
        public const string VerticalSpeedAirSpeed = "VerticalSpeedAirSpeed";
        public const string VOR1 = "VOR1";
        public const string VOR2 = "VOR2";
        public const string ADF = "ADF";
        public const string QNH = "QNH";
    }

    public class ValueChangeSettings
    {
        public string Type { get; set; }
    }

    public abstract class PresetChangeAction : StreamDeckAction
    {
        private readonly ILogger logger;
        private readonly IFlightConnector flightConnector;
        private readonly IEventDispatcher eventDispatcher;
        private Timer timer;
        private string? action;
        private bool timerHaveTick = false;
        private uint? originalValue = null;
        private AircraftStatus? status;
        private ValueChangeSettings? settings;

        public PresetChangeAction(
            ILogger logger, 
            IFlightConnector flightConnector,
            IEventRegistrar eventRegistrar,
            IEventDispatcher eventDispatcher)
        {
            this.logger = logger;
            this.flightConnector = flightConnector;
            this.eventDispatcher = eventDispatcher;

            timer = new Timer { Interval = 400 };
            timer.Elapsed += Timer_Elapsed;
            eventRegistrar.RegisterEvent(KnownEvents.VOR1_SET.ToString());
            eventRegistrar.RegisterEvent(KnownEvents.VOR2_SET.ToString());
            eventRegistrar.RegisterEvent(KnownEvents.ADF_SET.ToString());
            eventRegistrar.RegisterEvent(KnownEvents.KOHLSMAN_SET.ToString());
        }

        private void Timer_Elapsed(object? sender, ElapsedEventArgs e)
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
                // NOTE: switch to AP ALT:1 due to current issue with the WT NXi
                ValueChangeFunction.Altitude => (uint)status.ApAltitude1,
                ValueChangeFunction.VerticalSpeed => (uint)status.ApVs,
                ValueChangeFunction.AirSpeed => (uint)status.ApAirspeed,
                ValueChangeFunction.VerticalSpeedAirSpeed => status.IsApFlcOn ? (uint)status.ApAirspeed : (uint)status.ApVs,
                ValueChangeFunction.VOR1 => (uint)status.Nav1OBS,
                ValueChangeFunction.VOR2 => (uint)status.Nav2OBS,
                ValueChangeFunction.ADF => (uint)status.ADFCard,
                ValueChangeFunction.QNH => (uint)status.QNHMbar,

                _ => throw new NotImplementedException($"Value type: {buttonType}")
            };

            switch (buttonType)
            {
                case ValueChangeFunction.Heading:
                    ChangeHeading(originalValue.Value, sign, increment);
                    break;

                case ValueChangeFunction.Altitude:
                    if (status.ApAltitude0 == 99000)
                    {
                        // HACK: workaround since right now WT NXi doesn't recognize AP_ALT_VAR_SET_ENGLISH
                        if (sign == 1)
                        {
                            flightConnector.ApAltInc();
                        }
                        else
                        {
                            flightConnector.ApAltDec();
                        }
                    }
                    else
                    {
                        originalValue = (uint)(originalValue + 100 * sign * increment);
                        flightConnector.ApAltSet(originalValue.Value);
                    }
                    break;

                case ValueChangeFunction.VerticalSpeed:
                    ChangeVerticalSpeed(originalValue.Value, sign);
                    break;

                case ValueChangeFunction.AirSpeed:
                    ChangeAirSpeed(originalValue.Value, sign, increment);
                    break;

                case ValueChangeFunction.VerticalSpeedAirSpeed:
                    if (status.IsApFlcOn)
                    {
                        ChangeAirSpeed(originalValue.Value, sign, increment);
                    }
                    else
                    {
                        ChangeVerticalSpeed(originalValue.Value, sign);
                    }
                    break;
                case ValueChangeFunction.QNH:
                    double newValue = (double)originalValue + (sign * increment * 50);  // Value is in nanobar, increment per 50 nanobar (0.5 mbar)
                    flightConnector.QNHSet((uint)(newValue * .16));                     // Custom factor of 16, because SimConnect ;)
                    break;
                case ValueChangeFunction.VOR1:
                    ChangeSphericalValue(originalValue.Value, sign, increment, KnownEvents.VOR1_SET);
                    break;
                case ValueChangeFunction.VOR2:
                    ChangeSphericalValue(originalValue.Value, sign, increment, KnownEvents.VOR2_SET);
                    break;
                case ValueChangeFunction.ADF:
                    ChangeSphericalValue(originalValue.Value, sign, increment, KnownEvents.ADF_SET);
                    break;

            }
        }

        private void FlightConnector_AircraftStatusUpdated(object? sender, AircraftStatusUpdatedEventArgs e)
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

        private void ChangeVerticalSpeed(uint originalValue, int sign)
        {
            originalValue = (uint)(originalValue + 100 * sign);
            flightConnector.ApVsSet(originalValue);
        }

        private void ChangeAirSpeed(uint value, int sign, int increment)
        {
            flightConnector.ApAirSpeedSet((uint)Math.Max(0, value + increment * sign));
        }

        private void ChangeHeading(uint value, int sign, int increment)
        {
            value = CalculateSphericalIncrement(value, sign, increment);
            originalValue = value;
            flightConnector.ApHdgSet(value);
        }

        private void ChangeSphericalValue(uint value, int sign, int increment, KnownEvents evt)
        {
            value = CalculateSphericalIncrement(value, sign, increment);
            originalValue = value; 
            eventDispatcher.Trigger(evt.ToString(), value);
        }

        private uint CalculateSphericalIncrement(uint originalValue, int sign, int increment)
            => (uint)(originalValue + 360 + sign * increment) % 360;
    }
}

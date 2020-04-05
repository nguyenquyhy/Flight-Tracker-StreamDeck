using FlightStreamDeck.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SharpDeck;
using SharpDeck.Enums;
using SharpDeck.Events.Received;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace FlightStreamDeck.Logics.Actions
{
    class NavComAction : StreamDeckAction
    {
        private const int HOLD_DURATION_MILLISECONDS = 1000;

        private readonly RegistrationParameters registration;
        private readonly ILogger<NavComAction> logger;
        private readonly IImageLogic imageLogic;
        private readonly IFlightConnector flightConnector;
        private readonly Timer timer;

        private IdentifiableDeviceInfo device;
        private string type;
        private TOGGLE_VALUE? active;
        private TOGGLE_VALUE? standby;
        private TOGGLE_EVENT? toggle;
        private SET_EVENT? set;
        private string mask;

        public NavComAction(ILogger<NavComAction> logger, IImageLogic imageLogic, IFlightConnector flightConnector)
        {
            registration = RegistrationParameters.Parse(Environment.GetCommandLineArgs()[1..]);

            this.logger = logger;
            this.imageLogic = imageLogic;
            this.flightConnector = flightConnector;
            timer = new Timer { Interval = HOLD_DURATION_MILLISECONDS };
            timer.Elapsed += Timer_Elapsed;

        }

        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timer.Stop();

            if (type != null && set != null && mask != null)
            {
                var set = this.set;
                var mask = this.mask;
                var min = type switch
                {
                    "NAV1" => "10800",
                    "NAV2" => "10800",
                    "COM1" => "11800",
                    "COM2" => "11800",
                    _ => throw new ArgumentException($"{type} is not supported for numpad")
                };
                DeckLogic.NumpadParams = new NumpadParams(
                    type,
                    min,
                    type switch
                    {
                        "NAV1" => "11795",
                        "NAV2" => "11795",
                        "COM1" => "13697",
                        "COM2" => "13697",
                        _ => throw new ArgumentException($"{type} is not supported for numpad")
                    }
                );
                DeckLogic.NumpadTcs = new TaskCompletionSource<(string, bool)>();

                var toggle = this.toggle;

                this.initializationTcs = new TaskCompletionSource<bool>();

                await StreamDeck.SwitchToProfileAsync(registration.PluginUUID,
                    device.Id,
                    device.Type == DeviceType.StreamDeckXL ? "Profiles/Numpad_XL" : "Profiles/Numpad");

                await initializationTcs.Task;

                var (value, swap) = await DeckLogic.NumpadTcs.Task;
                if (!string.IsNullOrEmpty(value))
                {
                    value += min.Substring(value.Length);

                    if (type == "NAV1" || type == "NAV2" || type == "COM1" || type == "COM2")
                    {
                        // NOTE: SimConnect ignore first 1
                        value = value[1..];
                    }

                    // BCD encode
                    uint data = 0;
                    for (var i = 0; i < value.Length; i++)
                    {
                        uint digit = (byte)value[i] - (uint)48;
                        data = data * 16 + digit;
                    }
                    flightConnector.Set(set.Value, data);

                    if (toggle != null && swap)
                    {
                        await Task.Delay(500);
                        flightConnector.Toggle(toggle.Value);
                    }
                }
            }
        }

        protected override async Task OnWillAppear(ActionEventArgs<AppearancePayload> args)
        {
            flightConnector.GenericValuesUpdated += FlightConnector_GenericValuesUpdated;

            type = args.Payload.Settings.Value<string>("Type");
            await SetImageAsync(imageLogic.GetNavComImage(type));

            SwitchTo(type);

            if (initializationTcs != null)
            {
                logger.LogDebug("Trigger Task completion for initialization");
                initializationTcs.SetResult(true);
            }
        }

        protected override Task OnWillDisappear(ActionEventArgs<AppearancePayload> args)
        {
            flightConnector.GenericValuesUpdated -= FlightConnector_GenericValuesUpdated;
            SwitchTo(null);

            return Task.CompletedTask;
        }

        protected override Task OnKeyDown(ActionEventArgs<KeyPayload> args)
        {
            var device = registration.Info.Devices.FirstOrDefault(o => o.Id == args.Device);
            if (device.Type != DeviceType.StreamDeckMini)
            {
                this.device = device;
                timer.Start();
            }
            return Task.CompletedTask;
        }

        protected override Task OnKeyUp(ActionEventArgs<KeyPayload> args)
        {
            var device = registration.Info.Devices.FirstOrDefault(o => o.Id == args.Device);
            if (timer.Enabled || device.Type == DeviceType.StreamDeckMini)
            {
                timer.Stop();

                // Transfer
                if (toggle != null)
                {
                    flightConnector.Toggle(toggle.Value);
                }
            }
            return Task.CompletedTask;
        }

        protected override async Task OnSendToPlugin(ActionEventArgs<JObject> args)
        {
            type = args.Payload.Value<string>("Type");
            await SetImageAsync(imageLogic.GetNavComImage(type));
        }

        string lastValue1 = null;
        string lastValue2 = null;
        private TaskCompletionSource<bool> initializationTcs;

        private async void FlightConnector_GenericValuesUpdated(object sender, ToggleValueUpdatedEventArgs e)
        {
            string value1 = null, value2 = null;
            if (active != null && e.GenericValueStatus.ContainsKey(active.Value))
            {
                value1 = e.GenericValueStatus[active.Value];
            }
            if (standby != null && e.GenericValueStatus.ContainsKey(standby.Value))
            {
                value2 = e.GenericValueStatus[standby.Value];
            }

            if (lastValue1 != value1 || lastValue2 != value2)
            {
                lastValue1 = value1;
                lastValue2 = value2;
                await SetImageAsync(imageLogic.GetNavComImage(type, value1, value2));
            }
        }

        private void SwitchTo(string type)
        {
            var existing = new List<TOGGLE_VALUE>();
            if (active != null)
            {
                existing.Add(active.Value);
            }
            if (standby != null)
            {
                existing.Add(standby.Value);
            }
            if (existing.Count > 0)
            {
                flightConnector.DeRegisterSimValues(existing.ToArray());
            }
            switch (type)
            {
                case "NAV1":
                    active = TOGGLE_VALUE.NAV_ACTIVE_FREQUENCY__1;
                    standby = TOGGLE_VALUE.NAV_STANDBY_FREQUENCY__1;
                    toggle = TOGGLE_EVENT.NAV1_RADIO_SWAP;
                    set = SET_EVENT.NAV1_STBY_SET;
                    mask = "108.00";
                    break;
                case "NAV2":
                    active = TOGGLE_VALUE.NAV_ACTIVE_FREQUENCY__2;
                    standby = TOGGLE_VALUE.NAV_STANDBY_FREQUENCY__2;
                    toggle = TOGGLE_EVENT.NAV2_RADIO_SWAP;
                    set = SET_EVENT.NAV2_STBY_SET;
                    mask = "108.00";
                    break;
                case "COM1":
                    active = TOGGLE_VALUE.COM_ACTIVE_FREQUENCY__1;
                    standby = TOGGLE_VALUE.COM_STANDBY_FREQUENCY__1;
                    toggle = TOGGLE_EVENT.COM_STBY_RADIO_SWAP;
                    set = SET_EVENT.COM_STBY_RADIO_SET;
                    mask = "118.00";
                    break;
                case "COM2":
                    active = TOGGLE_VALUE.COM_ACTIVE_FREQUENCY__2;
                    standby = TOGGLE_VALUE.COM_STANDBY_FREQUENCY__2;
                    toggle = TOGGLE_EVENT.COM2_RADIO_SWAP;
                    set = SET_EVENT.COM2_STBY_RADIO_SET;
                    mask = "118.00";
                    break;
                default:
                    active = null;
                    standby = null;
                    toggle = null;
                    set = null;
                    lastValue1 = null;
                    lastValue2 = null;
                    break;
            }
            if (type != null)
            {
                flightConnector.RegisterSimValues(active.Value, standby.Value);
            }
            if (toggle != null)
            {
                flightConnector.RegisterToggleEvent(toggle.Value);
            }
            if (set != null)
            {
                flightConnector.RegisterSetEvent(set.Value);
            }
        }
    }
}

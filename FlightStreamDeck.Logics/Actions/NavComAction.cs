using FlightStreamDeck.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SharpDeck;
using SharpDeck.Enums;
using SharpDeck.Events.Received;
using SharpDeck.Manifest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace FlightStreamDeck.Logics.Actions
{
    [StreamDeckAction("tech.flighttracker.streamdeck.generic.navcom")]
    public class NavComAction : StreamDeckAction
    {
        private const int HOLD_DURATION_MILLISECONDS = 1000;
        private const string minNavVal = "10800";
        private const string maxNavVal = "11795";
        private const string minComVal = "11800";
        private const string maxComVal = "13697";
        private const string minXpdrVal = "0000";
        private const string maxXpdrVal = "7777";

        private readonly RegistrationParameters registration;
        private readonly ILogger<NavComAction> logger;
        private readonly IImageLogic imageLogic;
        private readonly IFlightConnector flightConnector;
        private readonly EnumConverter enumConverter;

        private readonly Timer timer;

        private IdentifiableDeviceInfo device;
        private string type;
        private TOGGLE_VALUE? active;
        private TOGGLE_VALUE? standby;
        private TOGGLE_EVENT? toggle;
        private SET_EVENT? set;
        private TOGGLE_VALUE? dependantOnAvionics;
        private TOGGLE_VALUE? dependantOnBatt;
        private bool hideBasedOnDependantValue;
        private string mask;

        public NavComAction(ILogger<NavComAction> logger, IImageLogic imageLogic, IFlightConnector flightConnector, EnumConverter enumConverter)
        {
            registration = new RegistrationParameters(Environment.GetCommandLineArgs()[1..]);

            this.logger = logger;
            this.imageLogic = imageLogic;
            this.flightConnector = flightConnector;
            this.enumConverter = enumConverter;
            timer = new Timer { Interval = HOLD_DURATION_MILLISECONDS };
            timer.Elapsed += Timer_Elapsed;
        }

        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timer.Stop();

            if (type != null && set != null && mask != null && lastDependant)
            {
                var set = this.set;
                var mask = this.mask;
                var min = type switch
                {
                    "NAV1" => minNavVal,
                    "NAV2" => minNavVal,
                    "COM1" => minComVal,
                    "COM2" => minComVal,
                    "XPDR" => minXpdrVal,
                    _ => throw new ArgumentException($"{type} is not supported for numpad")
                };
                DeckLogic.NumpadParams = new NumpadParams(
                    type,
                    min,
                    type switch
                    {
                        "NAV1" => maxNavVal,
                        "NAV2" => maxNavVal,
                        "COM1" => maxComVal,
                        "COM2" => maxComVal,
                        "XPDR" => maxXpdrVal,
                        _ => throw new ArgumentException($"{type} is not supported for numpad")
                    },
                    mask
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
            dependantOnAvionics = enumConverter.GetVariableEnum(args.Payload.Settings.Value<string>("AvionicsValue"));
            dependantOnBatt = enumConverter.GetVariableEnum(args.Payload.Settings.Value<string>("BattMasterValue"));
            hideBasedOnDependantValue = args.Payload.Settings.Value<string>("DependantValueHide")?.ToLower() == "yes";
            await SetImageAsync(imageLogic.GetNavComImage(type, hideBasedOnDependantValue));

            lastDependant = !lastDependant;
            lastValue1 = null;
            lastValue2 = null;
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
            dependantOnAvionics = enumConverter.GetVariableEnum(args.Payload.Value<string>("AvionicsValue"));
            dependantOnBatt = enumConverter.GetVariableEnum(args.Payload.Value<string>("BattMasterValue"));
            hideBasedOnDependantValue = args.Payload.Value<string>("DependantValueHide")?.ToLower() == "yes";
            lastDependant = !lastDependant;
            lastValue1 = null;
            lastValue2 = null;
            SwitchTo(type);
            await SetImageAsync(imageLogic.GetNavComImage(type, false));
        }

        string lastValue1 = null;
        string lastValue2 = null;
        bool lastDependant = false;

        private TaskCompletionSource<bool> initializationTcs;

        private async void FlightConnector_GenericValuesUpdated(object sender, ToggleValueUpdatedEventArgs e)
        {
            string value1 = null, value2 = null;
            bool dependant = true;
            bool showMainOnly = false;

            if (hideBasedOnDependantValue && dependantOnBatt != null && e.GenericValueStatus.ContainsKey(dependantOnBatt.Value))
            {
                dependant = e.GenericValueStatus[dependantOnBatt.Value] != "0";
            }
            if (hideBasedOnDependantValue && dependantOnAvionics != null && e.GenericValueStatus.ContainsKey(dependantOnAvionics.Value))
            {
                dependant = dependant && e.GenericValueStatus[dependantOnAvionics.Value] != "0";
            }
            if (active != null && e.GenericValueStatus.ContainsKey(active.Value))
            {
                showMainOnly = true;
                value1 = (hideBasedOnDependantValue && dependant) || !hideBasedOnDependantValue ? e.GenericValueStatus[active.Value] : string.Empty;
                if (type == "XPDR" && value1 != string.Empty) value1 = value1.PadLeft(4, '0');
            }
            if (standby != null && e.GenericValueStatus.ContainsKey(standby.Value))
            {
                value2 = (hideBasedOnDependantValue && dependant) || !hideBasedOnDependantValue ? e.GenericValueStatus[standby.Value]: string.Empty;
                showMainOnly = active != null && active.Value == standby.Value;
            }

            if (lastValue1 != value1 || lastValue2 != value2 || lastDependant != dependant)
            {
                lastValue1 = value1;
                lastValue2 = value2;
                lastDependant = dependant;
                await SetImageAsync(imageLogic.GetNavComImage(type, dependant, value1, value2, showMainOnly: showMainOnly));
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
                case "XPDR":
                    active = TOGGLE_VALUE.TRANSPONDER_CODE__1;
                    standby = TOGGLE_VALUE.TRANSPONDER_CODE__1;
                    toggle = null;
                    set = SET_EVENT.XPNDR_SET;
                    mask = "1200";
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
            if (dependantOnAvionics != null)
            {
                flightConnector.RegisterSimValues(dependantOnAvionics.Value);
            }
            if (dependantOnBatt != null)
            {
                flightConnector.RegisterSimValues(dependantOnBatt.Value);
            }
        }
    }
}

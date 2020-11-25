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
    public class NavComSettings
    {
        public string Type { get; set; }
        public string AvionicsValue { get; set; }
        public string BattMasterValue { get; set; }
    }

    [StreamDeckAction("tech.flighttracker.streamdeck.generic.navcom")]
    public class NavComAction : StreamDeckAction<NavComSettings>
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
        
        private NavComSettings settings;

        private TOGGLE_VALUE? dependantOnAvionics;
        private TOGGLE_VALUE? dependantOnBatt;

        private TOGGLE_VALUE? active;
        private TOGGLE_VALUE? standby;
        private TOGGLE_EVENT? toggle;
        private TOGGLE_EVENT? set;
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

            if (settings?.Type != null && set != null && mask != null && lastDependant)
            {
                var set = this.set;
                var mask = this.mask;
                var min = settings.Type switch
                {
                    "NAV1" => minNavVal,
                    "NAV2" => minNavVal,
                    "COM1" => minComVal,
                    "COM2" => minComVal,
                    "XPDR" => minXpdrVal,
                    _ => throw new ArgumentException($"{settings.Type} is not supported for numpad")
                };
                DeckLogic.NumpadParams = new NumpadParams(
                    settings.Type,
                    min,
                    settings.Type switch
                    {
                        "NAV1" => maxNavVal,
                        "NAV2" => maxNavVal,
                        "COM1" => maxComVal,
                        "COM2" => maxComVal,
                        "XPDR" => maxXpdrVal,
                        _ => throw new ArgumentException($"{settings.Type} is not supported for numpad")
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

                    if (settings.Type == "NAV1" || settings.Type == "NAV2" || settings.Type == "COM1" || settings.Type == "COM2")
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
                    flightConnector.Trigger(set.Value, data);

                    if (toggle != null && swap)
                    {
                        await Task.Delay(500);
                        flightConnector.Trigger(toggle.Value);
                    }
                }
            }
        }

        protected override async Task OnWillAppear(ActionEventArgs<AppearancePayload> args)
        {
            flightConnector.GenericValuesUpdated += FlightConnector_GenericValuesUpdated;

            var settings = args.Payload.GetSettings<NavComSettings>();
            InitializeSettings(settings);

            await SetImageAsync(imageLogic.GetNavComImage(settings.Type, false));

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
            if (lastDependant)
            {
                var device = registration.Info.Devices.FirstOrDefault(o => o.Id == args.Device);
                if (device.Type != DeviceType.StreamDeckMini)
                {
                    this.device = device;
                    timer.Start();
                }
            }
            return Task.CompletedTask;
        }

        protected override Task OnKeyUp(ActionEventArgs<KeyPayload> args)
        {
            if (lastDependant)
            {
                var device = registration.Info.Devices.FirstOrDefault(o => o.Id == args.Device);
                if (timer.Enabled || device.Type == DeviceType.StreamDeckMini)
                {
                    timer.Stop();

                    // Transfer
                    if (toggle != null)
                    {
                        flightConnector.Trigger(toggle.Value);
                    }
                }
            }
            return Task.CompletedTask;
        }

        protected override async Task OnSendToPlugin(ActionEventArgs<JObject> args)
        {
            var settings = args.Payload.ToObject<NavComSettings>();
            InitializeSettings(settings);

            await SetImageAsync(imageLogic.GetNavComImage(settings.Type, false));
        }

        private void InitializeSettings(NavComSettings settings)
        {
            this.settings = settings;
            dependantOnAvionics = enumConverter.GetVariableEnum(settings.AvionicsValue);
            dependantOnBatt = enumConverter.GetVariableEnum(settings.BattMasterValue);

            lastDependant = !lastDependant;
            lastValue1 = null;
            lastValue2 = null;

            SwitchTo(settings.Type);
        }

        string lastValue1 = null;
        string lastValue2 = null;
        bool lastDependant = false;

        private TaskCompletionSource<bool> initializationTcs;

        private async void FlightConnector_GenericValuesUpdated(object sender, ToggleValueUpdatedEventArgs e)
        {
            var settings = this.settings;

            if (settings != null)
            {
                string value1 = null, value2 = null;
                bool dependant = true;
                bool showMainOnly = false;

                if (dependantOnBatt != null && e.GenericValueStatus.ContainsKey(dependantOnBatt.Value))
                {
                    dependant = e.GenericValueStatus[dependantOnBatt.Value] != "0";
                }
                if (dependantOnAvionics != null && e.GenericValueStatus.ContainsKey(dependantOnAvionics.Value))
                {
                    dependant = dependant && e.GenericValueStatus[dependantOnAvionics.Value] != "0";
                }

                if (active != null && e.GenericValueStatus.ContainsKey(active.Value))
                {
                    showMainOnly = true;
                    value1 = dependant ? e.GenericValueStatus[active.Value] : string.Empty;
                    if (settings.Type == "XPDR" && value1 != string.Empty) value1 = value1.PadLeft(4, '0');
                }
                if (standby != null && e.GenericValueStatus.ContainsKey(standby.Value))
                {
                    value2 = dependant ? e.GenericValueStatus[standby.Value] : string.Empty;
                    showMainOnly = active != null && active.Value == standby.Value;
                }

                if (lastValue1 != value1 || lastValue2 != value2 || lastDependant != dependant)
                {
                    lastValue1 = value1;
                    lastValue2 = value2;
                    lastDependant = dependant;
                    await SetImageAsync(imageLogic.GetNavComImage(settings.Type, dependant, value1, value2, showMainOnly: showMainOnly));
                }
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
                    set = TOGGLE_EVENT.NAV1_STBY_SET;
                    mask = "108.00";
                    break;
                case "NAV2":
                    active = TOGGLE_VALUE.NAV_ACTIVE_FREQUENCY__2;
                    standby = TOGGLE_VALUE.NAV_STANDBY_FREQUENCY__2;
                    toggle = TOGGLE_EVENT.NAV2_RADIO_SWAP;
                    set = TOGGLE_EVENT.NAV2_STBY_SET;
                    mask = "108.00";
                    break;
                case "COM1":
                    active = TOGGLE_VALUE.COM_ACTIVE_FREQUENCY__1;
                    standby = TOGGLE_VALUE.COM_STANDBY_FREQUENCY__1;
                    toggle = TOGGLE_EVENT.COM_STBY_RADIO_SWAP;
                    set = TOGGLE_EVENT.COM_STBY_RADIO_SET;
                    mask = "118.00";
                    break;
                case "COM2":
                    active = TOGGLE_VALUE.COM_ACTIVE_FREQUENCY__2;
                    standby = TOGGLE_VALUE.COM_STANDBY_FREQUENCY__2;
                    toggle = TOGGLE_EVENT.COM2_RADIO_SWAP;
                    set = TOGGLE_EVENT.COM2_STBY_RADIO_SET;
                    mask = "118.00";
                    break;
                case "XPDR":
                    active = TOGGLE_VALUE.TRANSPONDER_CODE__1;
                    standby = TOGGLE_VALUE.TRANSPONDER_CODE__1;
                    toggle = null;
                    set = TOGGLE_EVENT.XPNDR_SET;
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
                flightConnector.RegisterToggleEvent(set.Value);
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

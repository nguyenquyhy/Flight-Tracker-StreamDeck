using FlightStreamDeck.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpDeck;
using SharpDeck.Enums;
using SharpDeck.Events.Received;
using SharpDeck.Manifest;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace FlightStreamDeck.Logics.Actions
{
    public class NavComSettings
    {
        public string Type { get; set; }
        public string HoldFunction { get; set; }
        public string AvionicsValue { get; set; }
        public string BattMasterValue { get; set; }
        [JsonProperty(nameof(ImageBackground))]
        public string ImageBackground { get; set; }
        [JsonProperty(nameof(ImageBackground_base64))]
        public string ImageBackground_base64 { get; set; }
    }

    [StreamDeckAction("tech.flighttracker.streamdeck.generic.navcom")]
    public class NavComAction : BaseAction<NavComSettings>
    {
        private AircraftStatus status;
        private const int HOLD_DURATION_MILLISECONDS = 1000;
        private const string minNavVal = "10800";
        private const string maxNavVal = "11795";
        private const string minComVal = "118000";
        private const string maxComVal = "136990";
        private const string minXpdrVal = "0000";
        private const string maxXpdrVal = "7777";

        private readonly RegistrationParameters registration;
        private readonly ILogger<NavComAction> logger;
        private readonly IImageLogic imageLogic;
        private readonly IFlightConnector flightConnector;

        private readonly Timer timer;

        private IdentifiableDeviceInfo device;

        private NavComSettings settings;

        private ToggleValue dependantOnAvionics;
        private ToggleValue dependantOnBatt;

        private ToggleValue active;
        private ToggleValue standby;
        private ToggleEvent toggle;
        private ToggleEvent set;
        private string mask;

        string lastValue1 = null;
        string lastValue2 = null;
        bool lastDependant = false;
        bool forceRegen = false;

        private TaskCompletionSource<bool> initializationTcs;

        public NavComAction(ILogger<NavComAction> logger, IImageLogic imageLogic, IFlightConnector flightConnector)
        {
            registration = new RegistrationParameters(Environment.GetCommandLineArgs()[1..]);

            this.logger = logger;
            this.imageLogic = imageLogic;
            this.flightConnector = flightConnector;
            timer = new Timer { Interval = HOLD_DURATION_MILLISECONDS };
            timer.Elapsed += Timer_Elapsed;
        }

        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timer.Stop();

            // Handle hold
            if (settings.HoldFunction != "Swap")
            {
                await SwitchToNumpad();
            }
            else
            {
                SwapFrequencies();
            }
        }

        protected override async Task OnWillAppear(ActionEventArgs<AppearancePayload> args)
        {
            flightConnector.GenericValuesUpdated += FlightConnector_GenericValuesUpdated;
            flightConnector.AircraftStatusUpdated += new EventHandler<AircraftStatusUpdatedEventArgs>(async (s, e) => await FlightConnector_AircraftStatusUpdatedAsync(s, e));

            var settings = args.Payload.GetSettings<NavComSettings>();
            InitializeSettings(settings);

            await UpdateImage(dependant: false, value1: null, value2: null, showMainOnly: false);

            var tcs = initializationTcs;
            if (tcs != null)
            {
                logger.LogDebug("Trigger Task completion for initialization");
                tcs.SetResult(true);
            }
        }

        protected override Task OnWillDisappear(ActionEventArgs<AppearancePayload> args)
        {
            flightConnector.GenericValuesUpdated -= FlightConnector_GenericValuesUpdated;
            flightConnector.AircraftStatusUpdated -= new EventHandler<AircraftStatusUpdatedEventArgs>(async (s, e) => await FlightConnector_AircraftStatusUpdatedAsync(s, e));
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

                    // Click
                    if (settings.HoldFunction != "Swap")
                    {
                        SwapFrequencies();
                    }
                    else
                    {
                        return SwitchToNumpad();
                    }
                }
            }
            return Task.CompletedTask;
        }

        protected override async Task OnSendToPlugin(ActionEventArgs<JObject> args)
        {

            if (args.Payload.TryGetValue("convertToEmbed", out JToken fileKeyObject))
            {
                var fileKey = fileKeyObject.Value<string>();
                await ConvertLinkToEmbed(fileKey);
            }
            else if (args.Payload.TryGetValue("convertToLink", out fileKeyObject))
            {
                var fileKey = fileKeyObject.Value<string>();

                await System.Windows.Application.Current.Dispatcher.Invoke(() => ConvertEmbedToLink(fileKey));
            }
            else
            {
                InitializeSettings(args.Payload.ToObject<NavComSettings>());
            }

            await UpdateImage(dependant: false, value1: null, value2: null, showMainOnly: false);
        }

        private void InitializeSettings(NavComSettings settings)
        {
            this.settings = settings;
            dependantOnAvionics = new(settings.AvionicsValue);
            dependantOnBatt = new(settings.BattMasterValue);

            lastDependant = !lastDependant;
            lastValue1 = null;
            lastValue2 = null;

            SwitchTo(settings.Type);
        }

        private async Task FlightConnector_AircraftStatusUpdatedAsync(object sender, AircraftStatusUpdatedEventArgs e)
        {
            status = e.AircraftStatus;
            // Update ADF image here since ADF frequencies aren't easily "converted" with mhz/khz/hz from simconnect
            if (settings.Type != null && settings.Type.StartsWith("ADF"))
            {
                string active = FormatFrequency(settings.Type.Equals("ADF1") ? status?.ADFActiveFrequency1 : status?.ADFActiveFrequency2);
                string standby = FormatFrequency(settings.Type.Equals("ADF1") ? status?.ADFStandbyFrequency1 : status?.ADFStandbyFrequency2);

                if (!string.IsNullOrEmpty(active) || !string.IsNullOrEmpty(standby))
                {
                    string value = lastDependant ? active : string.Empty;
                    string value2 = lastDependant ? standby : string.Empty;
                    if (forceRegen || lastValue1 != value || lastValue2 != value2)
                    {
                        forceRegen = false;
                        lastValue1 = value;
                        lastValue2 = value2;
                        await UpdateImage(lastDependant, value, value2, false);
                    }
                }
            }
        }

        private async void FlightConnector_GenericValuesUpdated(object sender, ToggleValueUpdatedEventArgs e)
        {
            var settings = this.settings;

            if (settings != null)
            {
                string value1 = null, value2 = null;
                bool dependant = true;
                bool showMainOnly = false;

                if (dependantOnBatt != null && (e.GenericValueStatus.Find(x => x == dependantOnBatt) != null))
                {
                    dependant = string.IsNullOrEmpty(e.GenericValueStatus.Find(x => x == dependantOnBatt).Name);
                }
                if (dependantOnBatt != null && (e.GenericValueStatus.Find(x => x == dependantOnAvionics) != null))
                {
                    dependant = string.IsNullOrEmpty(e.GenericValueStatus.Find(x => x == dependantOnAvionics).Name);
                }

                if (active != null && (e.GenericValueStatus.Find(x => x.Name == active.Name) != null))
                {
                    showMainOnly = true;
                    value1 = dependant ? e.GenericValueStatus.Find(x => x.Name == active.Name).Value.ToString("F" + active.Decimals.ToString()) : string.Empty;
                    //value1 = value1.Substring(0, value1.Length - 3);
                    //value1 = value1.Insert(3, ".");
                    if (settings.Type == "XPDR" && value1 != string.Empty) value1 = value1.PadLeft(4, '0');
                }
                if (standby != null && (e.GenericValueStatus.Find(x => x.Name.Contains("STANDBY")) != null))
                {
                    //value2 = dependant ? e.GenericValueStatus.Find(x => x == standby).Value.ToString("F" + standby.Decimals.ToString()) : string.Empty;
                    value2 = dependant ? e.GenericValueStatus.Find(x => x.Name.Contains ("STANDBY")).Value.ToString("F" + standby.Decimals.ToString()) : string.Empty;
                    //value2 = value2.Substring(0, value2.Length - 3);
                    //value2 = value2.Insert(3, ".");
                    showMainOnly = false;
                    //showMainOnly = active != null && active.Value == standby.Value;
                }

                if (settings.Type != null && settings.Type.StartsWith("ADF"))
                {
                    // For ADF, the update happens in FlightConnector_AircraftStatusUpdatedAsync
                    if (lastDependant != dependant)
                    {
                        forceRegen = true;
                        lastDependant = dependant;
                    }
                }
                else
                {
                    if (lastValue1 != value1 || lastValue2 != value2 || lastDependant != dependant)
                    {
                        lastValue1 = value1;
                        lastValue2 = value2;
                        lastDependant = dependant;
                        await UpdateImage(dependant, value1, value2, showMainOnly);
                    }
                }
            }
        }

        private async Task UpdateImage(bool dependant, string value1, string value2, bool showMainOnly)
        {
            await SetImageSafeAsync(imageLogic.GetNavComImage(settings.Type, dependant, value1, value2, showMainOnly: showMainOnly, settings.ImageBackground, GetImageBytes()));
        }

        private byte[] GetImageBytes()
        {
            byte[] imageBackgroundBytes = null;
            if (settings.ImageBackground_base64 != null)
            {
                var s = settings.ImageBackground_base64;
                s = s.Replace('-', '+').Replace('_', '/').PadRight(4 * ((s.Length + 3) / 4), '=');
                imageBackgroundBytes = Convert.FromBase64String(s);
            }

            return imageBackgroundBytes;
        }

        private void SwitchTo(string type)
        {
            var existing = new List<ToggleValue>();
            if (active != null)
            {
                existing.Add(new ToggleValue(active.Name));
            }
            if (standby != null)
            {
                existing.Add(new ToggleValue(standby.Name));
            }
            if (existing.Count > 0)
            {
                flightConnector.DeRegisterSimValues(existing);
            }
            switch (type)
            {
                case "NAV1":
                    active = new ToggleValue("NAV_ACTIVE_FREQUENCY__1");
                    standby = new ToggleValue("NAV_STANDBY_FREQUENCY__1");
                    toggle = new ToggleEvent("NAV1_RADIO_SWAP");
                    set = new ToggleEvent("NAV1_STBY_SET");
                    mask = "108.00";
                    break;
                case "NAV2":
                    active = new ToggleValue("NAV_ACTIVE_FREQUENCY__2");
                    standby = new ToggleValue("NAV_STANDBY_FREQUENCY__2");
                    toggle = new ToggleEvent("NAV2_RADIO_SWAP");
                    set = new ToggleEvent("NAV2_STBY_SET");
                    mask = "108.00";
                    break;
                case "COM1":
                    active = new ToggleValue("COM_ACTIVE_FREQUENCY__1");
                    standby = new ToggleValue("COM_STANDBY_FREQUENCY__1");
                    toggle = new ToggleEvent("COM_STBY_RADIO_SWAP");
                    set = new ToggleEvent("COM_STBY_RADIO_SET");
                    mask = "118.000";
                    break;
                case "COM2":
                    active = new ToggleValue("COM_ACTIVE_FREQUENCY__2");
                    standby = new ToggleValue("COM_STANDBY_FREQUENCY__2");
                    toggle = new ToggleEvent("COM2_RADIO_SWAP");
                    set = new ToggleEvent("COM2_STBY_RADIO_SET");
                    mask = "118.00";
                    break;
                case "XPDR":
                    active = new ToggleValue("TRANSPONDER_CODE__1");
                    standby = new ToggleValue("TRANSPONDER_CODE__1");
                    toggle = null;
                    set = new ToggleEvent("XPNDR_SET");
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
            var values = new List<ToggleValue>();
            if (type != null && !type.StartsWith("ADF"))
            {
                values.Add(new ToggleValue(active.Name));
                values.Add(new ToggleValue(standby.Name));
            }
            if (toggle != null)
            {
                flightConnector.RegisterToggleEvent(toggle);
            }
            if (set != null)
            {
                flightConnector.RegisterToggleEvent(set);
            }
            if (dependantOnAvionics != null)
            {
                values.Add(new ToggleValue(dependantOnAvionics.Name));
            }
            if (dependantOnBatt != null)
            {
                values.Add(new ToggleValue(dependantOnBatt.Name));
            }
            if (values.Count > 0)
            {
                flightConnector.RegisterSimValues(values);
            }
        }

        private void SwapFrequencies()
        {
            if (toggle != null)
            {
                flightConnector.Trigger(toggle);
            }
        }

        private async Task SwitchToNumpad()
        {
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
                    mask,
                    settings.ImageBackground,
                    GetImageBytes()
                );
                DeckLogic.NumpadTcs = new TaskCompletionSource<(string, bool)>();

                var toggle = this.toggle;

                this.initializationTcs = new TaskCompletionSource<bool>();

                await StreamDeck.SwitchToProfileAsync(registration.PluginUUID,
                    device.Id,
                    device.Type == DeviceType.StreamDeckXL ? "Profiles/Numpad_XL" : "Profiles/Numpad");

                try
                {
                    await initializationTcs.Task;
                }
                finally
                {
                    initializationTcs = null;
                }

                var (value, swap) = await DeckLogic.NumpadTcs.Task;
                if (!string.IsNullOrEmpty(value))
                {
                    if (value.Length < min.Length)
                    {
                        // Add the default mask
                        value += min[value.Length..];
                    }

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
                    flightConnector.Trigger(set, data);

                    if (toggle != null && swap)
                    {
                        await Task.Delay(500);
                        flightConnector.Trigger(toggle);
                    }
                }
            }
        }

        private async Task ConvertLinkToEmbed(string fileKey)
        {
            switch (fileKey)
            {
                case "ImageBackground":
                    settings.ImageBackground_base64 = Convert.ToBase64String(File.ReadAllBytes(settings.ImageBackground));
                    break;
            }

            await SetSettingsAsync(settings);
            await SendToPropertyInspectorAsync(new
            {
                Action = "refresh",
                Settings = settings
            });
            InitializeSettings(settings);
        }

        private async Task ConvertEmbedToLink(string fileKey)
        {
            var dialog = new SaveFileDialog
            {
                FileName = fileKey switch
                {
                    "ImageBackground" => Path.GetFileName(settings.ImageBackground),
                    _ => "image.png"
                },
                Filter = "Images|*.jpg;*.jpeg;*.png"
            };
            if (dialog.ShowDialog() == true)
            {
                var bytes = fileKey switch
                {
                    "ImageBackground" => Convert.FromBase64String(settings.ImageBackground_base64),
                    _ => null
                };
                if (bytes != null)
                {
                    File.WriteAllBytes(dialog.FileName, bytes);
                }
                switch (fileKey)
                {
                    case "ImageOn":
                        settings.ImageBackground_base64 = null;
                        settings.ImageBackground = dialog.FileName.Replace("\\", "/");
                        break;
                }
            }

            await SetSettingsAsync(settings);
            await SendToPropertyInspectorAsync(new
            {
                Action = "refresh",
                Settings = settings
            });
            InitializeSettings(settings);
        }

        private static string FormatFrequency(int? frequency) => (frequency == null) ? null : (frequency.Value / 1000).ToString();
    }
}

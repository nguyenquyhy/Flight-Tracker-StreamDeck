using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpDeck;
using SharpDeck.Events.Received;
using SharpDeck.Manifest;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Timers;

namespace FlightStreamDeck.Logics.Actions
{
    /// <summary>
    /// Note: We need to fix the JSON property names to avoid conversion to camel case
    /// </summary>
    public class PresetToggleSettings
    {
        [JsonProperty(nameof(Type))]
        public string Type { get; set; }
        [JsonProperty(nameof(HideHeader))]
        public bool HideHeader { get; set; }
        [JsonProperty(nameof(ImageOn))]
        public string ImageOn { get; set; }
        [JsonProperty(nameof(ImageOn_base64))]
        public string ImageOn_base64 { get; set; }
        [JsonProperty(nameof(ImageOff))]
        public string ImageOff { get; set; }
        [JsonProperty(nameof(ImageOff_base64))]
        public string ImageOff_base64 { get; set; }
    }

    public class PresetFunction
    {
        public const string Avionics = "Avionics";
        public const string ApMaster = "ApMaster";
        public const string Heading = "Heading";
        public const string Nav = "Nav";
        public const string Altitude = "Altitude";
        public const string VerticalSpeed = "VerticalSpeed";
        public const string Approach = "Approach";
        public const string FLC = "FLC";
    }

    [StreamDeckAction("tech.flighttracker.streamdeck.preset.toggle")]
    public class PresetToggleAction : StreamDeckAction<PresetToggleSettings>
    {
        private readonly ILogger logger;
        private readonly IFlightConnector flightConnector;
        private readonly IImageLogic imageLogic;
        private readonly Timer timer;
        private AircraftStatus status = null;
        private bool timerHasTick;
        private PresetToggleSettings settings;

        public PresetToggleAction(ILogger<PresetToggleAction> logger, IFlightConnector flightConnector, IImageLogic imageLogic)
        {
            this.logger = logger;
            this.flightConnector = flightConnector;
            this.imageLogic = imageLogic;
            timer = new Timer { Interval = 1000 };
            timer.Elapsed += Timer_Elapsed;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timerHasTick = true;
            timer.Stop();

            var currentStatus = status;
            if (currentStatus != null)
            {
                switch (settings?.Type)
                {
                    case PresetFunction.Heading:
                        logger.LogInformation("Toggle AP HDG. Current state: {state}.", currentStatus.IsApHdgOn);
                        flightConnector.ApHdgSet((uint)currentStatus.Heading);
                        break;
                }
            }
        }

        private async void FlightConnector_AircraftStatusUpdated(object sender, AircraftStatusUpdatedEventArgs e)
        {
            if (StreamDeck == null) return;

            var lastStatus = status;
            status = e.AircraftStatus;

            switch (settings?.Type)
            {
                case PresetFunction.Avionics:
                    if (e.AircraftStatus.IsAvMasterOn != lastStatus?.IsAvMasterOn)
                    {
                        logger.LogInformation("Received AV Master update: {state}", e.AircraftStatus.IsAvMasterOn);
                        await UpdateImage();
                    }
                    break;
                case PresetFunction.ApMaster:
                    if (e.AircraftStatus.IsAutopilotOn != lastStatus?.IsAutopilotOn)
                    {
                        logger.LogInformation("Received AP update: {state}", e.AircraftStatus.IsAutopilotOn);
                        await UpdateImage();
                    }
                    break;
                case PresetFunction.Heading:
                    if (e.AircraftStatus.ApHeading != lastStatus?.ApHeading || e.AircraftStatus.IsApHdgOn != lastStatus?.IsApHdgOn)
                    {
                        logger.LogInformation("Received HDG update: {IsApHdgOn} {ApHeading}", e.AircraftStatus.IsApHdgOn, e.AircraftStatus.ApHeading);
                        await UpdateImage();
                    }
                    break;
                case PresetFunction.Nav:
                    if (e.AircraftStatus.IsApNavOn != lastStatus?.IsApNavOn)
                    {
                        logger.LogInformation("Received NAV update: {IsApNavOn}", e.AircraftStatus.IsApNavOn);
                        await UpdateImage();
                    }
                    break;
                case PresetFunction.Altitude:
                    if (e.AircraftStatus.ApAltitude != lastStatus?.ApAltitude || e.AircraftStatus.IsApAltOn != lastStatus?.IsApAltOn)
                    {
                        logger.LogInformation("Received ALT update: {IsApAltOn} {ApAltitude}", e.AircraftStatus.IsApAltOn, e.AircraftStatus.ApAltitude);
                        await UpdateImage();
                    }
                    break;
                case PresetFunction.VerticalSpeed:
                    if (e.AircraftStatus.ApVs != lastStatus?.ApVs || e.AircraftStatus.IsApVsOn != lastStatus?.IsApVsOn)
                    {
                        logger.LogInformation("Received VS update: {IsApVsOn} {ApVerticalSpeed}", e.AircraftStatus.IsApVsOn, e.AircraftStatus.ApVs);
                        await UpdateImage();
                    }
                    break;
                case PresetFunction.FLC:
                    if (e.AircraftStatus.ApAirspeed != lastStatus?.ApAirspeed || e.AircraftStatus.IsApFlcOn != lastStatus?.IsApFlcOn)
                    {
                        logger.LogInformation("Received FLC update: {IsApFlcOn}", e.AircraftStatus.IsApFlcOn);
                        await UpdateImage();
                    }
                    break;
                case PresetFunction.Approach:
                    if (e.AircraftStatus.IsApAprOn != lastStatus?.IsApAprOn)
                    {
                        logger.LogInformation("Received APR update: {IsApAprOn}", e.AircraftStatus.IsApAprOn);
                        await UpdateImage();
                    }
                    break;
            }
        }

        protected override Task OnWillAppear(ActionEventArgs<AppearancePayload> args)
        {
            var settings = args.Payload.GetSettings<PresetToggleSettings>();
            InitializeSettings(settings);

            status = null;
            this.flightConnector.AircraftStatusUpdated += FlightConnector_AircraftStatusUpdated;

            return Task.CompletedTask;
        }

        protected override Task OnWillDisappear(ActionEventArgs<AppearancePayload> args)
        {
            this.flightConnector.AircraftStatusUpdated -= FlightConnector_AircraftStatusUpdated;
            status = null;

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

                System.Windows.Application.Current.Dispatcher.Invoke(() => ConvertEmbedToLink(fileKey));
            }
            else
            {
                InitializeSettings(args.Payload.ToObject<PresetToggleSettings>());
            }
            await UpdateImage();
        }
        private async Task ConvertLinkToEmbed(string fileKey)
        {
            switch (fileKey)
            {
                case "ImageOn":
                    settings.ImageOn_base64 = Convert.ToBase64String(File.ReadAllBytes(settings.ImageOn));
                    break;
                case "ImageOff":
                    settings.ImageOff_base64 = Convert.ToBase64String(File.ReadAllBytes(settings.ImageOff));
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
                    "ImageOn" => Path.GetFileName(settings.ImageOn),
                    "ImageOff" => Path.GetFileName(settings.ImageOff),
                    _ => "image.png"
                },
                Filter = "Images|*.jpg;*.jpeg;*.png"
            };
            if (dialog.ShowDialog() == true)
            {
                var bytes = fileKey switch
                {
                    "ImageOn" => Convert.FromBase64String(settings.ImageOn_base64),
                    "ImageOff" => Convert.FromBase64String(settings.ImageOff_base64),
                    _ => null
                };
                if (bytes != null)
                {
                    File.WriteAllBytes(dialog.FileName, bytes);
                }
                switch (fileKey)
                {
                    case "ImageOn":
                        settings.ImageOn_base64 = null;
                        settings.ImageOn = dialog.FileName.Replace("\\", "/");
                        break;
                    case "ImageOff":
                        settings.ImageOff_base64 = null;
                        settings.ImageOff = dialog.FileName.Replace("\\", "/");
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

        protected override Task OnKeyDown(ActionEventArgs<KeyPayload> args)
        {
            timerHasTick = false;
            timer.Start();
            return Task.CompletedTask;
        }

        protected override Task OnKeyUp(ActionEventArgs<KeyPayload> args)
        {
            timer.Stop();
            if (!timerHasTick)
            {
                var currentStatus = status;
                if (currentStatus != null)
                {
                    switch (settings?.Type)
                    {
                        case PresetFunction.Avionics:
                            logger.LogInformation("Toggle AV Master. Current state: {state}.", currentStatus.IsAvMasterOn);
                            uint off = 0;
                            uint on = 1;
                            flightConnector.AvMasterToggle(currentStatus.IsAvMasterOn ? off : on);
                            break;

                        case PresetFunction.ApMaster:
                            logger.LogInformation("Toggle AP Master. Current state: {state}.", currentStatus.IsAutopilotOn);
                            flightConnector.ApToggle();
                            break;

                        case PresetFunction.Heading:
                            logger.LogInformation("Toggle AP HDG. Current state: {state}.", currentStatus.IsApHdgOn);
                            flightConnector.ApHdgToggle();
                            break;

                        case PresetFunction.Nav:
                            logger.LogInformation("Toggle AP NAV. Current state: {state}.", currentStatus.IsApNavOn);
                            flightConnector.ApNavToggle();
                            break;

                        case PresetFunction.Altitude:
                            logger.LogInformation("Toggle AP ALT. Current state: {state}.", currentStatus.IsApAltOn);
                            flightConnector.ApAltToggle();
                            break;

                        case PresetFunction.VerticalSpeed:
                            logger.LogInformation("Toggle AP VS. Current state: {state}.", currentStatus.IsApVsOn);
                            flightConnector.ApVsToggle();
                            break;

                        case PresetFunction.FLC:
                            logger.LogInformation("Toggle AP FLC. Current state: {state}.", currentStatus.IsApFlcOn);
                            if (currentStatus.IsApFlcOn)
                            {
                                flightConnector.ApFlcOff();
                            }
                            else
                            {
                                flightConnector.ApAirSpeedSet((uint)Math.Round(currentStatus.IndicatedAirSpeed));
                                flightConnector.ApFlcOn();
                            }
                            break;

                        case PresetFunction.Approach:
                            logger.LogInformation("Toggle AP APR. Current state: {state}.", currentStatus.IsApAprOn);
                            flightConnector.ApAprToggle();
                            break;

                    }
                }
            }
            timerHasTick = false;
            return Task.CompletedTask;
        }

        private void InitializeSettings(PresetToggleSettings settings)
        {
            this.settings = settings;
        }

        private async Task UpdateImage()
        {
            var currentStatus = status;
            if (currentStatus != null && settings != null)
            {
                byte[] imageOnBytes = null;
                byte[] imageOffBytes = null;
                if (settings.ImageOn_base64 != null)
                {
                    var s = settings.ImageOn_base64;
                    s = s.Replace('-', '+').Replace('_', '/').PadRight(4 * ((s.Length + 3) / 4), '=');
                    imageOnBytes = Convert.FromBase64String(s);
                }
                if (settings.ImageOff_base64 != null)
                {
                    var s = settings.ImageOff_base64;
                    s = s.Replace('-', '+').Replace('_', '/').PadRight(4 * ((s.Length + 3) / 4), '=');
                    imageOffBytes = Convert.FromBase64String(s);
                }

                try
                {
                    switch (settings.Type)
                    {
                        case PresetFunction.Avionics:
                            await SetImageAsync(imageLogic.GetImage(settings.HideHeader ? "" : "AV", currentStatus.IsAvMasterOn,
                                imageOnFilePath: settings.ImageOn, imageOnBytes: imageOnBytes,
                                imageOffFilePath: settings.ImageOff, imageOffBytes: imageOffBytes));
                            break;

                        case PresetFunction.ApMaster:
                            await SetImageAsync(imageLogic.GetImage(settings.HideHeader ? "" : "AP", currentStatus.IsAutopilotOn,
                                imageOnFilePath: settings.ImageOn, imageOnBytes: imageOnBytes,
                                imageOffFilePath: settings.ImageOff, imageOffBytes: imageOffBytes));
                            break;

                        case PresetFunction.Heading:
                            await SetImageAsync(imageLogic.GetImage(settings.HideHeader ? "" : "HDG", currentStatus.IsApHdgOn, currentStatus.ApHeading.ToString(),
                                imageOnFilePath: settings.ImageOn, imageOnBytes: imageOnBytes,
                                imageOffFilePath: settings.ImageOff, imageOffBytes: imageOffBytes));
                            break;

                        case PresetFunction.Nav:
                            await SetImageAsync(imageLogic.GetImage(settings.HideHeader ? "" : "NAV", currentStatus.IsApNavOn,
                                imageOnFilePath: settings.ImageOn, imageOnBytes: imageOnBytes,
                                imageOffFilePath: settings.ImageOff, imageOffBytes: imageOffBytes));
                            break;

                        case PresetFunction.Altitude:
                            await SetImageAsync(imageLogic.GetImage(settings.HideHeader ? "" : "ALT", currentStatus.IsApAltOn, currentStatus.ApAltitude.ToString(),
                                imageOnFilePath: settings.ImageOn, imageOnBytes: imageOnBytes,
                                imageOffFilePath: settings.ImageOff, imageOffBytes: imageOffBytes));
                            break;

                        case PresetFunction.VerticalSpeed:
                            await SetImageAsync(imageLogic.GetImage(settings.HideHeader ? "" : "VS", currentStatus.IsApVsOn, currentStatus.ApVs.ToString(),
                                imageOnFilePath: settings.ImageOn, imageOnBytes: imageOnBytes,
                                imageOffFilePath: settings.ImageOff, imageOffBytes: imageOffBytes));
                            break;

                        case PresetFunction.FLC:
                            await SetImageAsync(imageLogic.GetImage(settings.HideHeader ? "" : "FLC", currentStatus.IsApFlcOn,
                                value: currentStatus.IsApFlcOn ? currentStatus.ApAirspeed.ToString() : null,
                                imageOnFilePath: settings.ImageOn, imageOnBytes: imageOnBytes,
                                imageOffFilePath: settings.ImageOff, imageOffBytes: imageOffBytes));
                            break;

                        case PresetFunction.Approach:
                            await SetImageAsync(imageLogic.GetImage(settings.HideHeader ? "" : "APR", currentStatus.IsApAprOn,
                                imageOnFilePath: settings.ImageOn, imageOnBytes: imageOnBytes,
                                imageOffFilePath: settings.ImageOff, imageOffBytes: imageOffBytes));
                            break;
                    }
                }
                catch (WebSocketException)
                {
                    // Ignore as we can't really do anything here
                }
            }
        }
    }
}

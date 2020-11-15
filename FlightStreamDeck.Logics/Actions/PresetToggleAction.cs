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
    public class PresetToggleSettings
    {
        public string Type { get; set; }
        public bool HideHeader { get; set; }
        public string ImageOn { get; set; }
        public string ImageOff { get; set; }
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
            InitializeSettings(args.Payload.ToObject<PresetToggleSettings>());
            await UpdateImage();
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
            if (currentStatus != null)
            {
                switch (settings.Type)
                {
                    case PresetFunction.Avionics:
                        await SetImageAsync(imageLogic.GetImage("", currentStatus.IsAvMasterOn, customActiveBackground: settings.ImageOn, customBackground: settings.ImageOff));
                        break;

                    case PresetFunction.ApMaster:
                        await SetImageAsync(imageLogic.GetImage(settings.HideHeader ? "" : "AP", currentStatus.IsAutopilotOn, customActiveBackground: settings.ImageOn, customBackground: settings.ImageOff));
                        break;

                    case PresetFunction.Heading:
                        await SetImageAsync(imageLogic.GetImage(settings.HideHeader ? "" : "HDG", currentStatus.IsApHdgOn, currentStatus.ApHeading.ToString(), customActiveBackground: settings.ImageOn, customBackground: settings.ImageOff));
                        break;

                    case PresetFunction.Nav:
                        await SetImageAsync(imageLogic.GetImage(settings.HideHeader ? "" : "NAV", currentStatus.IsApNavOn, customActiveBackground: settings.ImageOn, customBackground: settings.ImageOff));
                        break;

                    case PresetFunction.Altitude:
                        await SetImageAsync(imageLogic.GetImage(settings.HideHeader ? "" : "ALT", currentStatus.IsApAltOn, currentStatus.ApAltitude.ToString(), customActiveBackground: settings.ImageOn, customBackground: settings.ImageOff));
                        break;

                    case PresetFunction.VerticalSpeed:
                        await SetImageAsync(imageLogic.GetImage(settings.HideHeader ? "" : "VS", currentStatus.IsApVsOn, currentStatus.ApVs.ToString(), customActiveBackground: settings.ImageOn, customBackground: settings.ImageOff));
                        break;

                    case PresetFunction.FLC:
                        await SetImageAsync(imageLogic.GetImage(settings.HideHeader ? "" : "FLC", currentStatus.IsApFlcOn, customActiveBackground: settings.ImageOn, customBackground: settings.ImageOff, value: currentStatus.IsApFlcOn ? currentStatus.ApAirspeed.ToString() : null));
                        break;

                    case PresetFunction.Approach:
                        await SetImageAsync(imageLogic.GetImage(settings.HideHeader ? "" : "APR", currentStatus.IsApAprOn, customActiveBackground: settings.ImageOn, customBackground: settings.ImageOff));
                        break;
                }
            }
        }
    }
}

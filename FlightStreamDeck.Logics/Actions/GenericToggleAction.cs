using FlightStreamDeck.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpDeck;
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
    /// <summary>
    /// Note: We need to fix the JSON property names to avoid conversion to camel case
    /// </summary>
    public class GenericToggleSettings
    {
        [JsonProperty(nameof(Header))]
        public string Header { get; set; }

        [JsonProperty(nameof(ToggleValue))]
        public string ToggleValue { get; set; }
        [JsonProperty(nameof(ToggleValueData))]
        public string ToggleValueData { get; set; }

        [JsonProperty(nameof(HoldValue))]
        public string HoldValue { get; set; }
        [JsonProperty(nameof(HoldValueData))]
        public string HoldValueData { get; set; }
        [JsonProperty(nameof(HoldValueRepeat))]
        public bool HoldValueRepeat { get; set; }
        [JsonProperty(nameof(HoldValueSuppressToggle))]
        public bool HoldValueSuppressToggle { get; set; }

        [JsonProperty(nameof(FeedbackValue))]
        public string FeedbackValue { get; set; }
        [JsonProperty(nameof(DisplayValue))]
        public string DisplayValue { get; set; }
        [JsonProperty(nameof(DisplayValueUnit))]
        public string DisplayValueUnit { get; set; }
        [JsonProperty(nameof(DisplayValuePrecision))]
        public string DisplayValuePrecision { get; set; }
        [JsonProperty(nameof(ImageOn))]
        public string ImageOn { get; set; }
        [JsonProperty(nameof(ImageOn_base64))]
        public string ImageOn_base64 { get; set; }
        [JsonProperty(nameof(ImageOff))]
        public string ImageOff { get; set; }
        [JsonProperty(nameof(ImageOff_base64))]
        public string ImageOff_base64 { get; set; }
    }

    [StreamDeckAction("tech.flighttracker.streamdeck.generic.toggle")]
    public class GenericToggleAction : BaseAction<GenericToggleSettings>
    {
        private readonly ILogger<GenericToggleAction> logger;
        private readonly IFlightConnector flightConnector;
        private readonly IImageLogic imageLogic;
        private readonly IEvaluator evaluator;

        private Timer timer = null;

        private GenericToggleSettings settings = null;

        private ToggleEvent toggleEvent = null;
        private uint? toggleEventDataUInt = null;
        private ToggleValue toggleEventDataVariable = null;
        private double? toggleEventDataVariableValue = null;
        private ToggleEvent holdEvent = null;
        private uint? holdEventDataUInt = null;
        private ToggleValue holdEventDataVariable = null;
        private double? holdEventDataVariableValue = null;

        private IEnumerable<ToggleValue> feedbackVariables = new List<ToggleValue>();
        private IExpression expression;
        private ToggleValue displayValue = null;

        private double? currentValue = null;
        private string? currentValueTime = null;
        private bool currentStatus = false;

        private bool holdEventTriggerred = false;

        public GenericToggleAction(ILogger<GenericToggleAction> logger, IFlightConnector flightConnector, IImageLogic imageLogic,
            IEvaluator evaluator)
        {
            this.logger = logger;
            this.flightConnector = flightConnector;
            this.imageLogic = imageLogic;
            this.evaluator = evaluator;
        }

        protected override async Task OnWillAppear(ActionEventArgs<AppearancePayload> args)
        {
            var settings = args.Payload.GetSettings<GenericToggleSettings>();
            InitializeSettings(settings);

            flightConnector.GenericValuesUpdated += FlightConnector_GenericValuesUpdated;

            await UpdateImage();
        }

        private void InitializeSettings(GenericToggleSettings settings)
        {
            this.settings = settings;

            ToggleEvent newToggleEvent = string.IsNullOrWhiteSpace(settings.ToggleValue) ? null : new(settings.ToggleValue);
            ToggleEvent newHoldEvent = string.IsNullOrWhiteSpace(settings.HoldValue) ? null : new(settings.HoldValue);

            ToggleValue newToggleEventDataVariable = null, newHoldEventDataVariable = null;

            var newUnit = settings.DisplayValueUnit?.Trim();
            if (string.IsNullOrWhiteSpace(newUnit)) newUnit = null;
            var customDecimals = int.TryParse(settings.DisplayValuePrecision, out int decimals) ? decimals : 0;
            var valueLibrary = EventValueLibrary.AvailableValues.Find(x => string.IsNullOrWhiteSpace(settings.DisplayValue) && x.Name == settings.DisplayValue);
            ToggleValue newDisplayValue = valueLibrary ?? (string.IsNullOrWhiteSpace(settings.DisplayValue) ? null : new(settings.DisplayValue, newUnit, customDecimals));

            bool isToggleEventUint = uint.TryParse(settings.ToggleValueData, out uint newToggleEventDataUInt);
            bool isHoldEventUint = uint.TryParse(settings.HoldValueData, out uint newHoldEventDataUInt);
            if (!isToggleEventUint)
            {
                newToggleEventDataVariable = string.IsNullOrWhiteSpace(settings.ToggleValueData) ? null : new(settings.ToggleValueData);
            }
            if (!isHoldEventUint)
            {
                newHoldEventDataVariable = string.IsNullOrWhiteSpace(settings.HoldValueData) ? null : new(settings.HoldValueData);
            }

            (var newFeedbackVariables, var newExpression) = evaluator.Parse(settings.FeedbackValue);


            if (!newFeedbackVariables.SequenceEqual(feedbackVariables) || newDisplayValue != displayValue
                || newToggleEventDataVariable != toggleEventDataVariable
                || newHoldEventDataVariable != holdEventDataVariable
                )
            {
                DeRegisterValues();
            }

            toggleEvent = newToggleEvent;
            toggleEventDataUInt = newToggleEventDataUInt;
            toggleEventDataVariable = newToggleEventDataVariable;
            holdEvent = newHoldEvent;
            holdEventDataUInt = newHoldEventDataUInt;
            holdEventDataVariable = newHoldEventDataVariable;
            feedbackVariables = newFeedbackVariables;
            expression = newExpression;
            displayValue = newDisplayValue;

            RegisterValues();
        }

        private async void FlightConnector_GenericValuesUpdated(object sender, ToggleValueUpdatedEventArgs e)
        {
            if (StreamDeck == null) return;

            List<ToggleValue> valuesWithDefaultUnits = e.GenericValueStatus.Where(o => o.Unit == ToggleValue.DEFAULT_UNIT).ToList();
            var newStatus = expression != null && evaluator.Evaluate(valuesWithDefaultUnits, expression);
            var isUpdated = newStatus != currentStatus;
            currentStatus = newStatus;

            if (displayValue != null && e.GenericValueStatus.Find(x => x.Name == displayValue.Name) != null)
            {
                var newValue = e.GenericValueStatus.Find(x => x.Name == displayValue.Name);
                isUpdated |= newValue.Value != currentValue;
                currentValue = newValue.Value;

                if (displayValue.Name == "ZULU_TIME"
                    || displayValue.Name == "LOCAL_TIME")
                {
                    string hours = Math.Floor(newValue.Value / 3600).ToString().PadLeft(2, '0');
                    newValue.Value %= newValue.Value % 3600;

                    string minutes = Math.Floor(newValue.Value / 60).ToString().PadLeft(2, '0');
                    newValue.Value %= newValue.Value % 60;

                    string seconds = Math.Floor(newValue.Value).ToString().PadLeft(2, '0');

                    switch (displayValue.Decimals)
                    {
                        case 0: //HH:MM:SS
                            currentValueTime = $"{hours}:{minutes}:{seconds}{(displayValue.Name == "ZULU_TIME" ? "Z" : String.Empty)}";
                            currentValue = e.GenericValueStatus.Find(x => x.Name == displayValue.Name).Value;
                            break;
                        case 1: //HH:MM
                            currentValueTime = $"{hours}:{minutes}{(displayValue.Name == "ZULU_TIME" ? "Z" : String.Empty)}";
                            currentValue = e.GenericValueStatus.Find(x => x.Name == displayValue.Name).Value;
                            break;
                        default:
                            currentValueTime = string.Empty;
                            currentValue = e.GenericValueStatus.Find(x => x.Name == displayValue.Name).Value;
                            break;
                    }
                }
            }

            if (toggleEventDataVariable != null && e.GenericValueStatus.Find(x => x.Name == toggleEventDataVariable.Name) != null)
            {
                toggleEventDataVariableValue = e.GenericValueStatus.Find(x => x.Name == toggleEventDataVariable.Name).Value;
            }

            if (holdEventDataVariable != null && e.GenericValueStatus.Find(x => x.Name == holdEventDataVariable.Name) != null)
            {
                holdEventDataVariableValue = e.GenericValueStatus.Find(x => x.Name == holdEventDataVariable.Name).Value;
            }

            if (isUpdated)
            {
                await UpdateImage();
            }
        }

        protected override Task OnWillDisappear(ActionEventArgs<AppearancePayload> args)
        {
            flightConnector.GenericValuesUpdated -= FlightConnector_GenericValuesUpdated;
            DeRegisterValues();
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
                InitializeSettings(args.Payload.ToObject<GenericToggleSettings>());
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

        private void RegisterValues()
        {
            if (toggleEvent != null) flightConnector.RegisterToggleEvent(toggleEvent);
            if (holdEvent != null) flightConnector.RegisterToggleEvent(holdEvent);

            var values = new List<ToggleValue>();
            foreach (var feedbackVariable in feedbackVariables) values.Add(feedbackVariable);
            if (displayValue != null) values.Add(displayValue);
            if (toggleEventDataVariable != null) values.Add(toggleEventDataVariable);
            if (holdEventDataVariable != null) values.Add(holdEventDataVariable);

            if (values.Count > 0)
            {
                flightConnector.RegisterSimValues(values);
            }
        }

        private void DeRegisterValues()
        {
            var values = new List<ToggleValue>();
            foreach (var feedbackVariable in feedbackVariables) values.Add(feedbackVariable);
            if (displayValue != null) values.Add(displayValue);
            if (toggleEventDataVariable != null) values.Add(toggleEventDataVariable);
            if (holdEventDataVariable != null) values.Add(holdEventDataVariable);

            if (values.Count > 0)
            {
                flightConnector.DeRegisterSimValues(values);
            }

            currentValue = null;
            currentValueTime = string.Empty;
            toggleEventDataVariableValue = null;
            holdEventDataVariableValue = null;
        }

        protected override Task OnKeyDown(ActionEventArgs<KeyPayload> args)
        {
            holdEventTriggerred = false;

            if (holdEvent == null || !settings.HoldValueSuppressToggle)
            {
                TriggerToggleEvent();
            }

            if (holdEvent != null)
            {
                timer = new Timer { Interval = settings.HoldValueRepeat ? 400 : 1000 };
                timer.Elapsed += Timer_Elapsed;
                timer.Start();
            }

            return Task.CompletedTask;
        }

        protected override Task OnKeyUp(ActionEventArgs<KeyPayload> args)
        {
            if (settings.HoldValueSuppressToggle && !holdEventTriggerred)
            {
                TriggerToggleEvent();
            }

            var localTimer = timer;
            if (localTimer != null)
            {
                localTimer.Elapsed -= Timer_Elapsed;
                localTimer.Stop();
                localTimer = null;
            }

            return Task.CompletedTask;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (holdEvent != null)
            {
                holdEventTriggerred = true;

                flightConnector.Trigger(holdEvent,
                    !(holdEventDataVariable is null) && holdEventDataVariableValue.HasValue ?
                        Convert.ToUInt32(Math.Round(holdEventDataVariableValue.Value)) :
                        (holdEventDataUInt ?? 0)
                );

                if (!settings.HoldValueRepeat && timer != null)
                {
                    timer?.Stop();
                    timer = null;
                }
            }
        }

        private void TriggerToggleEvent()
        {
            if (toggleEvent != null)
            {
                flightConnector.Trigger(toggleEvent,
                    !(toggleEventDataVariable is null) && toggleEventDataVariableValue.HasValue ?
                        Convert.ToUInt32(Math.Round(toggleEventDataVariableValue.Value)) :
                        (toggleEventDataUInt ?? 0)
                );
            }
        }

        private async Task UpdateImage()
        {
            if (settings != null)
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

                var valueToShow = !string.IsNullOrWhiteSpace(currentValueTime) ?
                    currentValueTime :
                    (displayValue != null && currentValue.HasValue) ? currentValue.Value.ToString("F" + displayValue.Decimals) : "";

                await SetImageSafeAsync(imageLogic.GetImage(settings.Header, currentStatus,
                    value: valueToShow,
                    imageOnFilePath: settings.ImageOn, imageOnBytes: imageOnBytes,
                    imageOffFilePath: settings.ImageOff, imageOffBytes: imageOffBytes));
            }
        }
    }
}

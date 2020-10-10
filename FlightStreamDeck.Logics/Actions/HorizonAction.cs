using FlightStreamDeck.Core;
using Microsoft.Extensions.Logging;
using SharpDeck;
using SharpDeck.Events.Received;
using SharpDeck.Manifest;
using System.Threading.Tasks;

namespace FlightStreamDeck.Logics.Actions
{
    [StreamDeckAction("tech.flighttracker.streamdeck.artificial.horizon")]
    public class HorizonAction : StreamDeckAction
    {
        private readonly ILogger<HorizonAction> logger;
        private readonly IFlightConnector flightConnector;
        private readonly IImageLogic imageLogic;

        private TOGGLE_VALUE bankValue = TOGGLE_VALUE.PLANE_BANK_DEGREES;
        private TOGGLE_VALUE pitchValue = TOGGLE_VALUE.PLANE_PITCH_DEGREES;
        private TOGGLE_VALUE headingValue = TOGGLE_VALUE.PLANE_HEADING_DEGREES_MAGNETIC;

        private string lastRawHeading = "";
        private string lastRawBank = "";
        private string lastRawPitch = "";

        private float currentHeadingValue = 0.0f;
        private float currentBankValue = 0.0f;
        private float currentPitchValue = 0.0f;

        public HorizonAction(ILogger<HorizonAction> logger, IFlightConnector flightConnector, IImageLogic imageLogic)
        {
            this.logger = logger;
            this.flightConnector = flightConnector;
            this.imageLogic = imageLogic;
        }

        protected override async Task OnWillAppear(ActionEventArgs<AppearancePayload> args)
        {
            flightConnector.GenericValuesUpdated += FlightConnector_GenericValuesUpdated;

            RegisterValues();

            await UpdateImage();
        }

        private async void FlightConnector_GenericValuesUpdated(object sender, ToggleValueUpdatedEventArgs e)
        {
            bool isUpdated = false;

            if (e.GenericValueStatus.ContainsKey(bankValue) && lastRawBank != e.GenericValueStatus[bankValue])
            {
                lastRawBank = e.GenericValueStatus[bankValue];
                float.TryParse(lastRawBank, out currentBankValue);
                isUpdated = true;
            }
            if (e.GenericValueStatus.ContainsKey(headingValue) && lastRawHeading != e.GenericValueStatus[headingValue])
            {
                lastRawHeading = e.GenericValueStatus[headingValue];
                float.TryParse(lastRawHeading, out currentHeadingValue);
                isUpdated = true;
            }
            if (e.GenericValueStatus.ContainsKey(pitchValue) && lastRawPitch != e.GenericValueStatus[pitchValue])
            {
                lastRawPitch = e.GenericValueStatus[pitchValue];
                float.TryParse(lastRawPitch, out currentPitchValue);
                isUpdated = true;
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

        private void RegisterValues()
        {
            flightConnector.RegisterSimValue(bankValue);
            flightConnector.RegisterSimValue(pitchValue);
        }

        private void DeRegisterValues()
        {
            flightConnector.DeRegisterSimValue(bankValue);
            flightConnector.DeRegisterSimValue(pitchValue);
        }

        protected override Task OnKeyDown(ActionEventArgs<KeyPayload> args)
        {
            bankValue = 0;
            _ = UpdateImage();
            return Task.CompletedTask;
        }

        private async Task UpdateImage()
        {
            await SetImageAsync(imageLogic.GetHorizonImage(currentPitchValue, currentBankValue, currentHeadingValue));
        }
    }
}

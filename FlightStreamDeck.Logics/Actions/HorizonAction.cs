using FlightStreamDeck.Core;
using Microsoft.Extensions.Logging;
using SharpDeck;
using SharpDeck.Events.Received;
using SharpDeck.Manifest;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlightStreamDeck.Logics.Actions
{
    [StreamDeckAction("tech.flighttracker.streamdeck.artificial.horizon")]
    public class HorizonAction : BaseAction
    {
        private readonly ILogger<HorizonAction> Logger;
        private readonly IFlightConnector flightConnector;
        private readonly IImageLogic imageLogic;

        private ToggleValue bankValue = new("PLANE_BANK_DEGREES");
        private readonly ToggleValue pitchValue = new("PLANE_PITCH_DEGREES");
        private readonly ToggleValue headingValue = new("PLANE_HEADING_DEGREES_MAGNETIC");

        private double currentHeadingValue = 0;
        private double currentBankValue = 0;
        private double currentPitchValue = 0;

        public HorizonAction(ILogger<HorizonAction> logger, IFlightConnector flightConnector, IImageLogic imageLogic)
        {
            Logger = logger;
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

            var bank = e.GenericValueStatus.Find(x => x.Name == bankValue.Name);
            var heading = e.GenericValueStatus.Find(x => x.Name == headingValue.Name);
            var pitch = e.GenericValueStatus.Find(x => x.Name == pitchValue.Name);
            if (bank != null && currentBankValue != bank.Value)
            {
                currentBankValue = bank.Value;
                isUpdated = true;
            }
            if (heading != null && currentHeadingValue != heading.Value)
            {
                currentHeadingValue = heading.Value;
                isUpdated = true;
            }
            if (pitch != null && currentHeadingValue != pitch.Value)
            {
                currentPitchValue = pitch.Value;
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
            flightConnector.RegisterSimValues(new() { bankValue, pitchValue });
        }

        private void DeRegisterValues()
        {
            flightConnector.DeRegisterSimValues(new() { bankValue, pitchValue });
        }

        protected override Task OnKeyDown(ActionEventArgs<KeyPayload> args)
        {
            bankValue = null;
            _ = UpdateImage();
            return Task.CompletedTask;
        }

        private async Task UpdateImage()
        {
            await SetImageSafeAsync(imageLogic.GetHorizonImage(currentPitchValue, currentBankValue, currentHeadingValue));
        }
    }
}

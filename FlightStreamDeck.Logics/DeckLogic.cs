using OpenMacroBoard.SDK;
using StreamDeckSharp;
using System;

namespace FlightStreamDeck.Logics
{
    public class DeckLogic
    {
        public event EventHandler KeyPressed;

        public void Initialize()
        {
            var device = StreamDeck.OpenDevice();
            device.KeyStateChanged += Device_KeyStateChanged;
        }

        private void Device_KeyStateChanged(object sender, KeyEventArgs e)
        {
            KeyPressed?.Invoke(this, new EventArgs());
        }
    }
}

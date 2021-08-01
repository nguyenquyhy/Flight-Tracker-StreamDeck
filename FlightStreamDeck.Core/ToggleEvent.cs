using System;
using System.Reflection;
using System.Reflection.Emit;

namespace FlightStreamDeck.Core
{
    public class ToggleEvent: BaseToggle
    {
        public GenericEvents GenericEvent { get; set; }

        public ToggleEvent(string name): base(name)
        {
        }
    }
}
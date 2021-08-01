using System;

namespace FlightStreamDeck.Core
{
    public class BaseToggle
    {
        private string _name;
        public string Name
        {
            get => _name != null ? _name.Replace("MOBIFLIGHT_", "MobiFlight.") : string.Empty;
            set => _name = value;
        }
        public BaseToggle(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Name cannot be null or empty");
            }
            Name = name;
        }

    }
}

using System;

namespace FlightStreamDeck.Logics
{
    public interface IFlightConnector
    {
        event EventHandler<AircraftStatusUpdatedEventArgs> AircraftStatusUpdated;
        void ApOff();
        void ApOn();
        void ApToggle();
        void ApHdgToggle();
        void ApNavToggle();
        void ApAprToggle();
        void ApAltToggle();
        void ApHdgSet(uint heading);
        void ApHdgInc();
        void ApHdgDec();
        void ApAltSet(int altitude);
        void ApAltInc();
        void ApAltDec();
    }

    public class AircraftStatusUpdatedEventArgs : EventArgs
    {
        public AircraftStatusUpdatedEventArgs(AircraftStatus aircraftStatus)
        {
            AircraftStatus = aircraftStatus;
        }

        public AircraftStatus AircraftStatus { get; }
    }

    public class AircraftStatus
    {
        public string Callsign { get; set; }

        public double SimTime { get; set; }
        public int? LocalTime { get; set; }
        public int? ZuluTime { get; set; }
        public long? AbsoluteTime { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Altitude { get; set; }
        public double AltitudeAboveGround { get; set; }

        public double Heading { get; set; }
        public double TrueHeading { get; set; }

        public double GroundSpeed { get; set; }
        public double IndicatedAirSpeed { get; set; }
        public double VerticalSpeed { get; set; }

        public double FuelTotalQuantity { get; set; }

        public double Pitch { get; set; }
        public double Bank { get; set; }

        public bool IsOnGround { get; set; }
        public bool StallWarning { get; set; }
        public bool OverspeedWarning { get; set; }

        public bool IsAutopilotOn { get; set; }

        public bool IsApHdgOn { get; set; }
        public int ApHeading { get; set; }

        public bool IsApNavOn { get; set; }

        public bool IsApAprOn { get; set; }

        public bool IsApAltOn { get; set; }
        public int ApAltitude { get; set; }

        public string Transponder { get; set; }
        public int FreqencyCom1 { get; set; }
        public int FreqencyCom2 { get; set; }
    }
}

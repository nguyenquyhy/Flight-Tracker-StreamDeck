using FlightStreamDeck.Core;
using System;
using System.Collections.Generic;

namespace FlightStreamDeck.Logics
{
    public interface IFlightConnector
    {
        event EventHandler<AircraftStatusUpdatedEventArgs> AircraftStatusUpdated;
        event EventHandler<ToggleValueUpdatedEventArgs> GenericValuesUpdated;
        event EventHandler<InvalidEventRegisteredEventArgs> InvalidEventRegistered;

        void ApOff();
        void ApOn();
        void ApToggle();
        void ApHdgToggle();
        void ApNavToggle();
        void ApAprToggle();
        void ApAltToggle();
        void ApVsToggle();
        void ApFlcOn();
        void ApFlcOff();

        /// <param name="heading">In Degree</param>
        void ApHdgSet(uint heading);
        void ApHdgInc();
        void ApHdgDec();

        /// <param name="altitude">In Feet</param>
        void ApAltSet(uint altitude);
        void ApAltInc();
        void ApAltDec();

        /// <param name="speed">In Feet per min</param>
        void ApVsSet(uint speed);

        void ApAirSpeedSet(uint speed);
        void ApAirSpeedInc();
        void ApAirSpeedDec();
        void QNHSet(uint qnh);
        void QNHInc();
        void QNHDec();

        void AvMasterToggle(uint state);

        void Trigger(Enum eventEnum, uint data);

        uint? RegisterToggleEvent(Enum eventEnum, string eventName);

        void RegisterSimValues(params (TOGGLE_VALUE variables, string? unit)[] simValues);
        void DeRegisterSimValues(params (TOGGLE_VALUE variables, string? unit)[] simValues);
    }

    public class AircraftStatusUpdatedEventArgs : EventArgs
    {
        public AircraftStatusUpdatedEventArgs(AircraftStatus aircraftStatus)
        {
            AircraftStatus = aircraftStatus;
        }

        public AircraftStatus AircraftStatus { get; }
    }

    public class ToggleValueUpdatedEventArgs : EventArgs
    {
        public ToggleValueUpdatedEventArgs(Dictionary<(TOGGLE_VALUE variable, string? unit), double> genericValueStatus)
        {
            GenericValueStatus = genericValueStatus;
        }

        public Dictionary<(TOGGLE_VALUE variable, string? unit), double> GenericValueStatus { get; }
    }

    public class InvalidEventRegisteredEventArgs : EventArgs
    {
        public InvalidEventRegisteredEventArgs(uint sendID)
        {
            SendID = sendID;
        }

        public uint SendID { get; }
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

        public double WindDirection { get; set; }
        public double WindVelocity { get; set; }

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

        public bool IsApVsOn { get; set; }
        public int ApVs { get; set; }

        public bool IsApFlcOn { get; set; }
        public int ApAirspeed { get; set; }

        public int QNHMbar { get; set; }

        public string Transponder { get; set; }
        public int FreqencyCom1 { get; set; }
        public int FreqencyCom2 { get; set; }
        public bool IsAvMasterOn { get; set; }
        public double Nav1OBS { get; set; }
        public double Nav2OBS { get; set; }
        public double ADFCard { get; set; }
        public int ADFActiveFrequency1 { get; set; }
        public int ADFStandbyFrequency1 { get; set; }
        public int ADFActiveFrequency2 { get; set; }
        public int ADFStandbyFrequency2 { get; set; }
    }
}

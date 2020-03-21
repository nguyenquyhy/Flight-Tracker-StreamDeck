using System.Runtime.InteropServices;

namespace FlightStreamDeck.SimConnectFSX
{
    enum GROUPID
    {
        FLAG = 2000000000,
        MAX = 1,
    };

    enum DEFINITIONS
    {
        AircraftData,
        FlightStatus
    }

    internal enum DATA_REQUESTS
    {
        NONE,
        SUBSCRIBE_GENERIC,
        AIRCRAFT_DATA,
        FLIGHT_STATUS,
        ENVIRONMENT_DATA,
        FLIGHT_PLAN
    }

    internal enum EVENTS
    {
        MESSAGE_RECEIVED,
        AUTOPILOT_ON,
        AUTOPILOT_OFF,
        AP_HDG_TOGGLE,
        AP_ALT_TOGGLE,
        AP_HDG_SET,
        AP_HDG_INC,
        AP_HDG_DEC
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    struct AircraftDataStruct
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string Type;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string Model;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string Title;
        public double EstimatedCruiseSpeed;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    struct FlightStatusStruct
    {
        public int SimRate;

        public double Latitude;
        public double Longitude;
        public double Altitude;
        public double AltitudeAboveGround;
        public double Pitch;
        public double Bank;
        public double TrueHeading;
        public double MagneticHeading;
        public double GroundAltitude;
        public double GroundSpeed;
        public double IndicatedAirSpeed;
        public double VerticalSpeed;

        public double FuelTotalQuantity;

        public double WindVelocity;
        public double WindDirection;

        public int IsOnGround;
        public int StallWarning;
        public int OverspeedWarning;

        public int IsAutopilotOn;
        public int IsApHdgOn;
        public int ApHdg;
        public int IsApAltOn;
        public int ApAlt;

        public int Transponder;
        public int Com1;
        public int Com2;
    }
}

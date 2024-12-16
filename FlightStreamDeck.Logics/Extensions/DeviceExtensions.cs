using SharpDeck.Enums;

namespace FlightStreamDeck.Logics;

public static class DeviceExtensions
{
    public static bool IsHighResolution(this IdentifiableDeviceInfo? device) => device != null && device.Type == (DeviceType)7;
}

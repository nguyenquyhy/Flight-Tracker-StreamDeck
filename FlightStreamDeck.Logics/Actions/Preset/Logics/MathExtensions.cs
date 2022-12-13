using System.Numerics;

namespace FlightStreamDeck.Logics.Actions.Preset;

public static class MathExtensions
{
    public static uint IncreaseSpherical(this uint value, int amount)
        => (uint)(value + 360 + amount) % 360;
}

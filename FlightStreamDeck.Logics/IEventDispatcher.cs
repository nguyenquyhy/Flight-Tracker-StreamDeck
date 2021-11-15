using System.Diagnostics.CodeAnalysis;

namespace FlightStreamDeck.Logics
{
    public interface IEventDispatcher
    {
        bool IsValid([NotNullWhen(true)] string? eventName);
        bool Trigger(string? eventName, uint value = 0);
    }
}

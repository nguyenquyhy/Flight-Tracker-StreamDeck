using static FlightStreamDeck.Logics.SimEventManager;

namespace FlightStreamDeck.Logics
{
    public interface IEventRegistrar
    {
        (EventEnum eventEnum, bool isValid)? RegisterEvent(string? eventName);
        void ReInitializeEvents();
    }
}

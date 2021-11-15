namespace FlightStreamDeck.Logics
{
    public interface IEventRegistrar
    {
        void RegisterEvent(string? eventName);
        void ReInitializeEvents();
    }
}

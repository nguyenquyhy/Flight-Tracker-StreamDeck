using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace FlightStreamDeck.Logics
{
    public class SimEventManager : IEventRegistrar, IEventDispatcher
    {
        public enum EventEnum
        {
            Start = 1000
        };

        private readonly ConcurrentDictionary<string, (EventEnum eventEnum, bool isValid)> registeredEvents = new();
        private readonly ConcurrentDictionary<uint, string> sendIDs = new();

        private readonly ILogger<SimEventManager> logger;
        private readonly IFlightConnector flightConnector;

        private int currentCount = (int)EventEnum.Start;

        public SimEventManager(ILogger<SimEventManager> logger, IFlightConnector flightConnector)
        {
            this.logger = logger;
            this.flightConnector = flightConnector;

            flightConnector.InvalidEventRegistered += FlightConnector_InvalidEventRegistered;
        }

        public void RegisterEvent(string? eventName)
        {
            if (IsValid(eventName))
            {
                eventName = NormalizeEventName(eventName);
                lock (registeredEvents)
                {
                    registeredEvents.AddOrUpdate(
                        eventName,
                        addValueFactory: eventName =>
                        {
                            var eventEnum = (EventEnum)currentCount++;
                            var sendId = flightConnector.RegisterToggleEvent(eventEnum, eventName);
                            if (sendId.HasValue)
                            {
                                sendIDs.TryAdd(sendId.Value, eventName);
                                logger.LogInformation("Event {eventName} is registered to enum {eventEnum} with sendID {sendID}.", eventName, (int)eventEnum, sendId);
                                return (eventEnum, true);
                            }
                            else
                            {
                                logger.LogWarning("Event {eventName} cannot be registered to enum {eventEnum}!", eventName, (int)eventEnum);
                                return (eventEnum, true);
                            }
                        },
                        updateValueFactory: (eventName, registration) =>
                        {
                            if (registration.isValid)
                            {
                                logger.LogDebug("Skipped registration of {eventName} because it is already registered to enum {eventEnum}!", eventName, (int)registration.eventEnum);
                                return registration;
                            }
                            else
                            {
                                logger.LogInformation("Event {eventName} is registered again to enum {eventEnum}.", eventName, (int)registration.eventEnum);
                                return (registration.eventEnum, true);
                            }
                        }
                    );
                }
            }
        }

        public void ReInitializeEvents()
        {
            foreach ((var eventName, var registration) in registeredEvents)
            {
                var sendId = flightConnector.RegisterToggleEvent(registration.eventEnum, eventName);
                if (sendId.HasValue)
                {
                    sendIDs.TryAdd(sendId.Value, eventName);
                    logger.LogInformation("Event {eventName} is re-registered to enum {eventEnum} with sendID {sendID}.", eventName, (int)registration.eventEnum, sendId);
                    registeredEvents[eventName] = (registration.eventEnum, true);
                }
                else
                {
                    logger.LogInformation("Event {eventName} cannot be re-registered to enum {eventEnum}.", eventName, (int)registration.eventEnum, sendId);
                    registeredEvents[eventName] = (registration.eventEnum, false);
                }
            }
        }

        public bool Trigger(string? eventName, uint value = 0)
        {
            if (IsValid(eventName))
            {
                eventName = NormalizeEventName(eventName);

                if (registeredEvents.TryGetValue(eventName, out var registration))
                {
                    if (registration.isValid)
                    {
                        logger.LogInformation("Event {eventName} [{eventEnum}] is triggred with value {value}.", eventName, (int)registration.eventEnum, value);
                        flightConnector.Trigger(registration.eventEnum, value);
                        return true;
                    }
                    else
                    {
                        logger.LogWarning("Event {eventName} cannot be triggered because it is invalid!", eventName);
                    }
                }
                else
                {
                    logger.LogWarning("Event {eventName} cannot be triggered because it is not registered!", eventName);
                }
            }
            return false;
        }

        public bool IsValid([NotNullWhen(true)] string? eventName) => !string.IsNullOrWhiteSpace(eventName);

        private string NormalizeEventName(string eventName) => 
            eventName
                .Replace(":", "__").Replace(" ", "_")
                .Replace("MOBIFLIGHT_", "MobiFlight.")
                .Trim();

        private void FlightConnector_InvalidEventRegistered(object? sender, InvalidEventRegisteredEventArgs e)
        {
            if (sendIDs.TryGetValue(e.SendID, out var eventName) && registeredEvents.TryGetValue(eventName, out var registration))
            {
                logger.LogWarning("Event {eventName} is invalid!", eventName);
                registeredEvents[eventName] = (registration.eventEnum, false);
            }
        }
    }
}

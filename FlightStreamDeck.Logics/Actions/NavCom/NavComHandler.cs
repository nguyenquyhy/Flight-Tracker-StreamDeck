using FlightStreamDeck.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlightStreamDeck.Logics.Actions.NavCom
{
    public abstract class NavComHandler
    {
        private readonly IFlightConnector flightConnector;
        private readonly IEventRegistrar eventRegistrar;
        private readonly IEventDispatcher eventDispatcher;
        private readonly TOGGLE_VALUE active;
        private readonly TOGGLE_VALUE? standby;
        private readonly TOGGLE_VALUE? batteryVariable;
        private readonly TOGGLE_VALUE? avionicsVariable;
        private readonly KnownEvents? toggle;
        private readonly KnownEvents? set;

        public string MinPattern { get; }
        public string Mask { get; }

        public bool IsSettable => set != null;

        public NavComHandler(
            IFlightConnector flightConnector,
            IEventRegistrar eventRegistrar,
            IEventDispatcher eventDispatcher,
            TOGGLE_VALUE active,
            TOGGLE_VALUE? standby,
            TOGGLE_VALUE? batteryVariable,
            TOGGLE_VALUE? avionicsVariable,
            KnownEvents? toggle,
            KnownEvents? set,
            string minPattern,
            string mask
        )
        {
            this.flightConnector = flightConnector;
            this.eventRegistrar = eventRegistrar;
            this.eventDispatcher = eventDispatcher;
            this.active = active;
            this.standby = standby;
            this.batteryVariable = batteryVariable;
            this.avionicsVariable = avionicsVariable;
            this.toggle = toggle;
            this.set = set;
            MinPattern = minPattern;
            Mask = mask;
        }

        public void RegisterSimValuesAndEvents()
        {
            if (toggle != null)
            {
                eventRegistrar.RegisterEvent(toggle.Value.ToString());
            }
            eventRegistrar.RegisterEvent(set.ToString());

            var values = new List<(TOGGLE_VALUE variable, string? unit)>();
            values.Add((active, null));
            if (standby != null)
            {
                values.Add((standby.Value, null));
            }
            if (batteryVariable != null)
            {
                values.Add((batteryVariable.Value, null));
            }
            if (avionicsVariable != null)
            {
                values.Add((avionicsVariable.Value, null));
            }
            if (values.Count > 0)
            {
                flightConnector.RegisterSimValues(values.ToArray());
            }
        }

        public void DeRegisterSimValues()
        {
            var existing = new List<(TOGGLE_VALUE variables, string? unit)>();
            existing.Add((active, null));
            if (standby != null)
            {
                existing.Add((standby.Value, null));
            }
            flightConnector.DeRegisterSimValues(existing.ToArray());
        }

        public async Task TriggerAsync(string value, bool swap)
        {
            if (value.Length < MinPattern.Length)
            {
                // Add the default mask
                value += MinPattern.Substring(value.Length);
            }

            uint data = FormatValueForSimConnect(value);
            eventDispatcher.Trigger(set.ToString(), data);

            if (toggle != null && swap)
            {
                await Task.Delay(500);
                eventDispatcher.Trigger(toggle.Value.ToString());
            }
        }

        public (string activeString, string standbyString, bool showActiveOnly, bool dependant) GetDisplayValues(Dictionary<(TOGGLE_VALUE variable, string? unit), double> genericValues)
        {
            bool dependant = true;
            if (batteryVariable != null && genericValues.TryGetValue((batteryVariable.Value, null), out var batteryValue))
            {
                dependant = dependant && batteryValue != 0;
            }
            if (avionicsVariable != null && genericValues.TryGetValue((avionicsVariable.Value, null), out var avionicsValue))
            {
                dependant = dependant && avionicsValue != 0;
            }

            var value1 = string.Empty;
            var value2 = string.Empty;
            if (dependant)
            {
                if (genericValues.TryGetValue((active, null), out var doubleValue1))
                {
                    value1 = FormatValueForDisplay(doubleValue1, active);
                }
                if (standby != null && genericValues.TryGetValue((standby.Value, null), out var doubleValue2))
                {
                    value2 = FormatValueForDisplay(doubleValue2, standby.Value);
                }
            }
            return (value1, value2, standby == null, dependant);
        }

        public void SwapFrequencies()
        {
            if (toggle != null)
            {
                eventDispatcher.Trigger(toggle.Value.ToString());
            }
        }

        protected abstract uint FormatValueForSimConnect(string value);
        protected virtual string FormatValueForDisplay(double value, TOGGLE_VALUE simvar)
        {
            return value.ToString("F" + EventValueLibrary.GetDecimals(simvar));
        }
    }
}

using FlightStreamDeck.Logics.Actions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpDeck;
using SharpDeck.Events.Received;
using System;
using System.Threading.Tasks;

namespace FlightStreamDeck.Logics
{
    public class NumpadParams
    {
        public NumpadParams(string type, string min, string max)
        {
            Type = type;
            MinPattern = min;
            MaxPattern = max;
        }

        public string Type { get; }
        public string MinPattern { get; }
        public string MaxPattern { get; }
        public string Value { get; set; } = "";
    }

    public class DeckLogic
    {
        public static NumpadParams NumpadParams { get; set; }
        public static TaskCompletionSource<(string value, bool swap)> NumpadTcs { get; set; }

        private readonly ILoggerFactory loggerFactory;
        private readonly IServiceProvider serviceProvider;
        private StreamDeckClient client;

        public DeckLogic(ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
        {
            this.loggerFactory = loggerFactory;
            this.serviceProvider = serviceProvider;
        }

        public void Initialize()
        {
            var args = Environment.GetCommandLineArgs();
            loggerFactory.CreateLogger<DeckLogic>().LogInformation("Initialize with args: {args}", string.Join("|", args));

            client = new StreamDeckClient(args[1..], loggerFactory.CreateLogger<StreamDeckClient>());

            client.RegisterAction("tech.flighttracker.streamdeck.generic.toggle", () => (GenericToggleAction)ActivatorUtilities.CreateInstance(serviceProvider, typeof(GenericToggleAction)));
            client.RegisterAction("tech.flighttracker.streamdeck.generic.gauge", () => (GenericGaugeAction)ActivatorUtilities.CreateInstance(serviceProvider, typeof(GenericGaugeAction)));
            client.RegisterAction("tech.flighttracker.streamdeck.artificial.horizon", () => (HorizonAction)ActivatorUtilities.CreateInstance(serviceProvider, typeof(HorizonAction)));

            client.RegisterAction("tech.flighttracker.streamdeck.master.activate", () => (ApToggleAction)ActivatorUtilities.CreateInstance(serviceProvider, typeof(ApToggleAction)));
            client.RegisterAction("tech.flighttracker.streamdeck.heading.activate", () => (ApToggleAction)ActivatorUtilities.CreateInstance(serviceProvider, typeof(ApToggleAction)));
            client.RegisterAction("tech.flighttracker.streamdeck.nav.activate", () => (ApToggleAction)ActivatorUtilities.CreateInstance(serviceProvider, typeof(ApToggleAction))); 
            client.RegisterAction("tech.flighttracker.streamdeck.approach.activate", () => (ApToggleAction)ActivatorUtilities.CreateInstance(serviceProvider, typeof(ApToggleAction)));
            client.RegisterAction("tech.flighttracker.streamdeck.altitude.activate", () => (ApToggleAction)ActivatorUtilities.CreateInstance(serviceProvider, typeof(ApToggleAction)));
            client.RegisterAction("tech.flighttracker.streamdeck.heading.increase", () => (ValueChangeAction)ActivatorUtilities.CreateInstance(serviceProvider, typeof(ValueChangeAction)));
            client.RegisterAction("tech.flighttracker.streamdeck.heading.decrease", () => (ValueChangeAction)ActivatorUtilities.CreateInstance(serviceProvider, typeof(ValueChangeAction)));
            client.RegisterAction("tech.flighttracker.streamdeck.altitude.increase", () => (ValueChangeAction)ActivatorUtilities.CreateInstance(serviceProvider, typeof(ValueChangeAction)));
            client.RegisterAction("tech.flighttracker.streamdeck.altitude.decrease", () => (ValueChangeAction)ActivatorUtilities.CreateInstance(serviceProvider, typeof(ValueChangeAction)));
            
            client.RegisterAction("tech.flighttracker.streamdeck.generic.navcom", () => (NavComAction)ActivatorUtilities.CreateInstance(serviceProvider, typeof(NavComAction)));

            client.RegisterAction("tech.flighttracker.streamdeck.number.enter", () => (NumberFunctionAction)ActivatorUtilities.CreateInstance(serviceProvider, typeof(NumberFunctionAction)));
            client.RegisterAction("tech.flighttracker.streamdeck.number.backspace", () => (NumberFunctionAction)ActivatorUtilities.CreateInstance(serviceProvider, typeof(NumberFunctionAction)));
            client.RegisterAction("tech.flighttracker.streamdeck.number.cancel", () => (NumberFunctionAction)ActivatorUtilities.CreateInstance(serviceProvider, typeof(NumberFunctionAction)));
            client.RegisterAction("tech.flighttracker.streamdeck.number.transfer", () => (NumberFunctionAction)ActivatorUtilities.CreateInstance(serviceProvider, typeof(NumberFunctionAction)));
            client.RegisterAction("tech.flighttracker.streamdeck.number.display", () => (NumberDisplayAction)ActivatorUtilities.CreateInstance(serviceProvider, typeof(NumberDisplayAction)));
            for (var i = 0; i <= 9; i++)
            {
                client.RegisterAction("tech.flighttracker.streamdeck.number." + i, () => (NumberAction)ActivatorUtilities.CreateInstance(serviceProvider, typeof(NumberAction)));
            }


            client.KeyDown += Client_KeyDown;
            
            Task.Run(() =>
            {
                client.Start(); // continuously listens until the connection closes
            });
        }

        private void Client_KeyDown(object sender, ActionEventArgs<KeyPayload> e)
        {
            //client.SetTitleAsync(e.Context, "Hello world");
        }
    }
}

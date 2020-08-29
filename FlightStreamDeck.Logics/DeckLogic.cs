using Microsoft.Extensions.Logging;
using SharpDeck;
using SharpDeck.Events.Received;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace FlightStreamDeck.Logics
{
    public class NumpadParams
    {
        public NumpadParams(string type, string min, string max, string mask)
        {
            Type = type;
            MinPattern = min;
            MaxPattern = max;
            Mask = mask;
        }

        public string Type { get; }
        public string MinPattern { get; }
        public string MaxPattern { get; }
        public string Value { get; set; } = "";
        public string Mask { get; set; } = "xxx.xx";
    }

    public class DeckLogic
    {
        public static NumpadParams NumpadParams { get; set; }
        public static TaskCompletionSource<(string value, bool swap)> NumpadTcs { get; set; }

        private readonly ILoggerFactory loggerFactory;
        private readonly IServiceProvider serviceProvider;

        public DeckLogic(ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
        {
            this.loggerFactory = loggerFactory;
            this.serviceProvider = serviceProvider;
        }

        public void Initialize()
        {
            var args = Environment.GetCommandLineArgs();
            loggerFactory.CreateLogger<DeckLogic>().LogInformation("Initialize with args: {args}", string.Join("|", args));

            var plugin = StreamDeckPlugin.Create(args[1..], Assembly.GetAssembly(GetType())).WithServiceProvider(serviceProvider);
            
            Task.Run(() =>
            {
                plugin.Run(); // continuously listens until the connection closes
            });
        }
    }
}

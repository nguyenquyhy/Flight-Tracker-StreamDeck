using Microsoft.Extensions.Logging;
using StreamDeckLib;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FlightStreamDeck.Logics
{
    public class DeckLogic
    {
        private readonly ILoggerFactory loggerFactory;

        public DeckLogic(ILoggerFactory loggerFactory)
        {
            this.loggerFactory = loggerFactory;
        }

        public async Task InitializeAsync()
        {
            var args = Environment.GetCommandLineArgs().ToList();
            args.RemoveAt(0);
            loggerFactory.CreateLogger<DeckLogic>().LogInformation("Initialize with args: {args}", string.Join("|", args));
            await ConnectionManager.Initialize(args.ToArray(), loggerFactory)
                .RegisterAllActions(typeof(DeckLogic).Assembly)
                .StartAsync();
        }
    }
}

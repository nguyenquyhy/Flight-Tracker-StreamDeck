using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FlightStreamDeck.Logics
{
    public class ThrottlingLogic
    {
        private const int MinMilliseconds = 2000;

        private readonly ILogger<ThrottlingLogic> logger;

        public ThrottlingLogic(ILogger<ThrottlingLogic> logger)
        {
            this.logger = logger;
        }

        private readonly SemaphoreSlim sm = new SemaphoreSlim(1);

        public async Task RunAsync(Func<Task> action)
        {
            logger.LogInformation("Run action is triggered.");
            try
            {
                await sm.WaitAsync();

                logger.LogInformation("Run action is executing...");
                await action();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Cannot execute function!");
            }
            finally
            {
                await Task.Delay(MinMilliseconds);
                sm.Release();
            }
        }
    }
}

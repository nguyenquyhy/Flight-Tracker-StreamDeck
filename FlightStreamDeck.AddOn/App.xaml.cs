using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.IO;
using System.Windows;

namespace FlightStreamDeck.AddOn
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public ServiceProvider ServiceProvider { get; private set; }
        public IConfigurationRoot Configuration { get; private set; }

        private MainWindow mainWindow = null;

        protected override void OnStartup(StartupEventArgs e)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            Configuration = builder.Build();

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            ServiceProvider = serviceCollection.BuildServiceProvider();

            mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private void ConfigureServices(ServiceCollection services)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Debug()
                .MinimumLevel.Information()
                .WriteTo.File("flightstreamdeck.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 3)
                .CreateLogger();

            //services.AddOptions<AppSettings>().Bind(Configuration).ValidateDataAnnotations();

            services.AddLogging(configure =>
            {
                configure.AddSerilog();
            });

            //services.AddSingleton<IFlightConnector, SimConnectFlightConnector>();
            //services.AddSingleton(new UserPreferencesLoader("preferences.json"));

            services.AddTransient(typeof(MainWindow));
        }
    }
}

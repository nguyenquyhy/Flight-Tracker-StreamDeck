using FlightStreamDeck.Core;
using FlightStreamDeck.Logics;
using FlightStreamDeck.SimConnectFSX;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
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
            AppCenter.Start("9343b8d4-4141-40c9-9758-5c7e2fb3a1a0", typeof(Analytics), typeof(Crashes));

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

            services.AddSingleton<DeckLogic>();
            services.AddSingleton<IFlightConnector, SimConnectFlightConnector>();
            //services.AddSingleton(new UserPreferencesLoader("preferences.json"));

            services.AddTransient(typeof(MainWindow));
            services.AddSingleton<IImageLogic, ImageLogic>();
            services.AddTransient<IEvaluator, SimpleEvaluator>();
            services.AddTransient<EnumConverter>();
        }
    }
}

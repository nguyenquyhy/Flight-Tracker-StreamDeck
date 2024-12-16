using FlightStreamDeck.Logics;
using FlightStreamDeck.Logics.Actions;
using FlightStreamDeck.SimConnectFSX;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using SharpDeck.Connectivity;
using SharpDeck.Extensions.Hosting;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace FlightStreamDeck.AddOn;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public IConfigurationRoot Configuration { get; private set; } = null!;

    private MainWindow? mainWindow = null;

    protected override void OnStartup(StartupEventArgs e)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        Configuration = builder.Build();

        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);

        var host = InitializeStreamDeckHost(serviceCollection);
        if (host != null)
        {
            using var scope = host.Services.CreateScope();

            // By default SharpDeck register only those from the entry Assembly. We need to also register all actions from .Logics project.
            var registry = scope.ServiceProvider.GetRequiredService<IStreamDeckActionRegistry>();
            registry.RegisterAll(typeof(BaseAction).Assembly);

            mainWindow = scope.ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
    }

    private IHost? InitializeStreamDeckHost(ServiceCollection serviceCollection)
    {
        if (Environment.GetCommandLineArgs().Length <= 1)
        {
            MessageBox.Show($"This plugin must be launched by Stream Deck software!\nPlease install it properly by double clicking the .streamDeckPlugin file or find it in Stream Deck's More Actions.",
                "Flight Tracker", MessageBoxButton.OK, MessageBoxImage.Error);
            Current.Shutdown();
            return null;
        }

        // Create a custom factory here so we can register our own dependency in addition to those from SharpDeck
        var factory = new ServiceProviderFactory(serviceCollection);

        var host = Host.CreateDefaultBuilder()
            .UseServiceProviderFactory(factory)
            .UseStreamDeck()
            .Build();

        Task.Run(() =>
        {
            host.Start();
        });

        return host;
    }

    private void ConfigureServices(ServiceCollection services)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Debug()
            .WriteTo.Logger(config => config
                .MinimumLevel.Information()
                .WriteTo.File("logs/flightstreamdeck.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 3)
            )
            .CreateLogger();

        //services.AddOptions<AppSettings>().Bind(Configuration).ValidateDataAnnotations();

        services.AddLogging(configure =>
        {
            configure.AddSerilog();
        });

        services.AddSingleton<ThrottlingLogic>();
        services.AddSingleton<IFlightConnector, SimConnectFlightConnector>();
        //services.AddSingleton(new UserPreferencesLoader("preferences.json"));

        services.AddTransient(typeof(MainWindow));
        services.AddSingleton<IImageLogic, ImageLogic>();
        services.AddTransient<IEvaluator, ComparisonEvaluator>();

        services.AddSingleton<SimEventManager>();
        services.AddSingleton<SimVarManager>();
        services.AddTransient<IEventRegistrar>(services => services.GetRequiredService<SimEventManager>());
        services.AddTransient<IEventDispatcher>(services => services.GetRequiredService<SimEventManager>());

        services.AddPresetLogics();
    }
}

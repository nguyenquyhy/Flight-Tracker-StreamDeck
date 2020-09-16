using FlightStreamDeck.Logics;
using FlightStreamDeck.SimConnectFSX;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Fallback;
using Polly.Retry;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace FlightStreamDeck.AddOn
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DeckLogic deckLogic;
        private readonly IFlightConnector flightConnector;
        private readonly ILogger<MainWindow> logger;
        private IntPtr Handle;

        public MainWindow(DeckLogic deckLogic, IFlightConnector flightConnector, ILogger<MainWindow> logger)
        {
            InitializeComponent();
            this.deckLogic = deckLogic;
            this.flightConnector = flightConnector;
            this.logger = logger;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Hide();
            deckLogic.Initialize();

            // Initialize SimConnect
            if (flightConnector is SimConnectFlightConnector simConnect)
            {
                simConnect.Closed += SimConnect_Closed;

                // Create an event handle for the WPF window to listen for SimConnect events
                Handle = new WindowInteropHelper(sender as Window).Handle; // Get handle of main WPF Window
                var HandleSource = HwndSource.FromHwnd(Handle); // Get source of handle in order to add event handlers to it
                HandleSource.AddHook(simConnect.HandleSimConnectEvents);

                //var viewModel = ServiceProvider.GetService<MainViewModel>();

                try
                {
                    await InitializeSimConnectAsync(simConnect);
                }
                catch (BadImageFormatException ex)
                {
                    logger.LogError(ex, "Cannot find SimConnect!");

                    var result = MessageBox.Show(this, "SimConnect not found. This component is needed to connect to Flight Simulator.\n" +
                        "Please download SimConnect from\n\nhttps://events-storage.flighttracker.tech/downloads/SimConnect.zip\n\n" +
                        "follow the ReadMe.txt in the zip file and try to start again.\n\nThis program will now exit.\n\nDo you want to open the SimConnect link above?",
                        "Needed component is missing",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Error);

                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = "https://events-storage.flighttracker.tech/downloads/SimConnect.zip",
                                UseShellExecute = true
                            });
                        }
                        catch { }
                    }

                    App.Current.Shutdown(-1);
                }
            }
        }

        private async Task InitializeSimConnectAsync(SimConnectFlightConnector simConnect)
        {
            AsyncRetryPolicy _retryPolicy = Policy
                .Handle<COMException>()
                .WaitAndRetryAsync(3, retryAttempt =>
                {
                    TimeSpan timeToWait = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                    return timeToWait;
                }, (Exception ex, TimeSpan ts) => {
                    logger.LogError(ex, $"Waiting {ts.TotalSeconds} seconds before retrying...");
                }
            );

            await _retryPolicy
                .ExecuteAsync(() =>
                {
                    myNotifyIcon.Icon = new Icon("Images/button@2x.ico");
                    simConnect.Initialize(Handle);
                    myNotifyIcon.Icon = new Icon("Images/button_active@2x.ico");
                    simConnect.Send("Connected to Stream Deck plugin");
                    return Task.FromResult<object>(null);
                });
        }

        private async void SimConnect_Closed(object sender, EventArgs e)
        {
            var simConnect = sender as SimConnectFlightConnector;
            await InitializeSimConnectAsync(simConnect);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            myNotifyIcon.Dispose();
        }
    }
}

using FlightStreamDeck.Logics;
using System.Windows;

namespace FlightStreamDeck.AddOn
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DeckLogic deckLogic;

        public MainWindow(DeckLogic deckLogic)
        {
            InitializeComponent();
            this.deckLogic = deckLogic;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await deckLogic.InitializeAsync();
        }
    }
}

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

        public MainWindow()
        {
            InitializeComponent();

            deckLogic = new DeckLogic();
            deckLogic.KeyPressed += DeckLogic_KeyPressed;
        }

        private void DeckLogic_KeyPressed(object sender, System.EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show(this, $"Key pressed");
            });
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            deckLogic.Initialize();
        }
    }
}

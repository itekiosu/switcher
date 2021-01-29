
using System.Windows;
using System.Windows.Media;

namespace ItekiSwitcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ItekiSwitcherUI : Window
    {


        public ItekiSwitcherUI()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ((App)App.Current).PerformSwitcherAction();
        }

        private void languageSelectionBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ((App)App.Current).UpdateLanguageByIndex(languageSelectionBox.SelectedIndex);
        }
    }
}

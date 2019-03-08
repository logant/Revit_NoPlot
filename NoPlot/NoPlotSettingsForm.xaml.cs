using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;


namespace NoPlot
{
    /// <summary>
    /// Interaction logic for NoPlotSettings.xaml
    /// </summary>
    public partial class NoPlotSettingsForm : Window
    {
        public NoPlotSettingsForm()
        {
            InitializeComponent();

            npTextBox.Text = Properties.Settings.Default.NoPlotId;
            defaultOnCheckBox.IsChecked = Properties.Settings.Default.ServiceState;
            verifyCheckBox.IsChecked = Properties.Settings.Default.AskBefore;
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.NoPlotId = npTextBox.Text;
            Properties.Settings.Default.ServiceState = defaultOnCheckBox.IsChecked.Value;
            Properties.Settings.Default.AskBefore = verifyCheckBox.IsChecked.Value;
            Properties.Settings.Default.Save();
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

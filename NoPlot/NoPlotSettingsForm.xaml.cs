using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Autodesk.Revit.DB.Architecture;



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
            /*
            NoPlotApp.Instance.CheckSettings();

            npTextBox.Text = NoPlotApp.Instance.NpId;
            defaultOnCheckBox.IsChecked = NoPlotApp.Instance.DefaultState;
            verifyCheckBox.IsChecked = NoPlotApp.Instance.Inquire;
            */
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            /*
#if REVIT2022
            string path = RevitCommon.Interface.Windows.SettingsPath;
#else
            string path = System.IO.Path.Combine(Environment.ExpandEnvironmentVariables("%onedrive%"), "hks-revit.settings");
#endif
            string name = this.GetType().Assembly.GetName().Name;
            RevitCommon.FileUtils.SetString(path, name, "NoPlotId",
                NoPlotApp.Instance.NpId);
            RevitCommon.FileUtils.SetInt(path, name, "ServiceState", NoPlotApp.Instance.ServiceOn ? 1 : 0);
            RevitCommon.FileUtils.SetInt(path, name, "AskBefore", NoPlotApp.Instance.Inquire ? 1 : 0);
            */
            Close();
            
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

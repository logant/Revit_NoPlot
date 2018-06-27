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

            // NOTE: These settings are for use within HKS.  If being built for outside
            // use, you may leave these enabled, but be sure to disable the next block of code.
            tabLabel.Visibility = Visibility.Hidden;
            tabTextBox.IsEnabled = false;
            tabTextBox.Visibility = Visibility.Hidden;
            tabTextBox.Text = null;
            panelLabel.Visibility = Visibility.Hidden;
            panelTextBox.IsEnabled = false;
            panelTextBox.Visibility = Visibility.Hidden;
            panelTextBox.Text = null;
            Height = 200;

            // NOTE: Below is only for plugin use outside of HKS
            // If using it within HKS, Comment out everything below here to the end of the constructor
            //tabLabel.Visibility = Visibility.Visible;
            //tabTextBox.IsEnabled = true;
            //panelTextBox.IsEnabled = true;
            //tabTextBox.Visibility = Visibility.Visible;
            //panelLabel.Visibility = Visibility.Visible;
            //panelTextBox.Visibility = Visibility.Visible;
            //tabTextBox.Text = Properties.Settings.Default.TabName;
            //panelTextBox.Text = Properties.Settings.Default.PanelName;
            //Height = 225;
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.NoPlotId = npTextBox.Text;
            Properties.Settings.Default.ServiceState = defaultOnCheckBox.IsChecked.Value;
            Properties.Settings.Default.AskBefore = verifyCheckBox.IsChecked.Value;

            bool warning = false;
            if (tabTextBox.Text != null && tabTextBox.Text != string.Empty && tabTextBox.Text != Properties.Settings.Default.TabName)
            {
                Properties.Settings.Default.TabName = tabTextBox.Text;
                warning = true;
            }
            if (panelTextBox.Text != null && panelTextBox.Text != string.Empty && panelTextBox.Text != Properties.Settings.Default.PanelName)
            {
                Properties.Settings.Default.PanelName = panelTextBox.Text;
                warning = true;
            }
            if(warning)
            {
                Autodesk.Revit.UI.TaskDialog.Show("Warning", "Changes to the Tab or Panel name associated with this command will take place next time Revit is started.");
            }

            Properties.Settings.Default.Save();
            Close();
        }

       

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

      

        private void Border_KeyDown(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) // Is Ctrl key pressed
            {
                if (Keyboard.IsKeyDown(Key.U))
                {
                    // Launch the UI form
                    System.Diagnostics.Process proc = System.Diagnostics.Process.GetCurrentProcess();
                    IntPtr handle = proc.MainWindowHandle;
                    RevitCommon.UILocation uiForm = new RevitCommon.UILocation("No Plot", Properties.Settings.Default.TabName, Properties.Settings.Default.PanelName);
                    System.Windows.Interop.WindowInteropHelper wih = new System.Windows.Interop.WindowInteropHelper(uiForm) {Owner = handle};
                    uiForm.ShowDialog();

                    string tab = uiForm.Tab;
                    string panel = uiForm.Panel;

                    if (tab != Properties.Settings.Default.TabName || panel != Properties.Settings.Default.PanelName)
                    {
                        Properties.Settings.Default.TabName = tab;
                        Properties.Settings.Default.PanelName = panel;
                        Properties.Settings.Default.Save();

                        Autodesk.Revit.UI.TaskDialog.Show("Warning", "Changes to the panel or tab this tool resides on will take place when Revit restarts.");
                    }
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Autodesk.Revit.UI;



namespace NoPlot
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private static Uri localUri = new Uri("pack://application:,,,/NoPlot;component/LocalResources.xaml");

        private Settings _settings;

        public SettingsWindow()
        {
            _settings = new Settings();
            UpdateResources();
            
            InitializeComponent();

            // Associate
            switch(_settings.Theme)
            {
                case 1:
                    ThemeLightRadioButton.IsChecked = true;
                    break;
                case 2:
                    ThemeDarkRadioButton.IsChecked = true;
                    break;
                default:
                    ThemeDefaultRadioButton.IsChecked = true;
                    break;
            }
            
            DefaultStateCheckbox.IsChecked = _settings.DefaultActive;
            PromptCheckbox.IsChecked = _settings.Prompt;

            CategoryNamesCheckbox.IsChecked = _settings.SearchCatNames;
            SubcategoryNamesCheckbox.IsChecked = _settings.SearchSubcatNames;
            FamilyNamesCheckbox.IsChecked = _settings.SearchFamNames;
            TypeNamesCheckbox.IsChecked = _settings.SearchTypeNames;
            GroupNamesCheckbox.IsChecked = _settings.SearchGroupNames;

            PrintCheckbox.IsChecked = _settings.WhenPrinting;
            ExportPDFCheckbox.IsChecked = _settings.WhenExportPdf;
            ExportDWFCheckbox.IsChecked = _settings.WhenExportDwf;
            PerspectiveCheckbox.IsChecked = _settings.IncludePerspectives;

            CaseCheckbox.IsChecked = _settings.CaseSensitive;
            SearchTextBox.Text = string.Join("\n", _settings.SearchTerms) + "\n";
        }

        internal void UpdateResources()
        {
            Resources.Clear();
            try
            {
                //var resources = RevitCommon.Interface.Windows.GetThemeResources(localUri);
                var resources = Interface.GetThemeResources(localUri, _settings.Theme);
                resources.ForEach(Resources.MergedDictionaries.Add);
            }
            catch(Exception e)
            {
                TaskDialog.Show("Error", e.Message);
            }
        }

        

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Honestly should probably bind this stuff
            var themeRad = ThemePanel.Children.OfType<RadioButton>().FirstOrDefault(r => r.IsChecked.Value);
            if (themeRad != null)
            {
                switch (themeRad.Name)
                {
                    case "ThemeLightRadioButton":
                        _settings.Theme = 1;
                        break;
                    case "ThemeDarkRadioButton":
                        _settings.Theme = 2;
                        break;
                    default:
                        _settings.Theme = 0;
                        break;
                }
            }
            _settings.DefaultActive = DefaultStateCheckbox.IsChecked.Value;
            _settings.Prompt = PromptCheckbox.IsChecked.Value;

            _settings.SearchCatNames = CategoryNamesCheckbox.IsChecked.Value;
            _settings.SearchSubcatNames = SubcategoryNamesCheckbox.IsChecked.Value;
            _settings.SearchFamNames = FamilyNamesCheckbox.IsChecked.Value;
            _settings.SearchTypeNames = TypeNamesCheckbox.IsChecked.Value;
            _settings.SearchGroupNames = GroupNamesCheckbox.IsChecked.Value;

            _settings.WhenPrinting = PrintCheckbox.IsChecked.Value;
            _settings.WhenExportPdf = ExportPDFCheckbox.IsChecked.Value;
            _settings.WhenExportDwf = ExportDWFCheckbox.IsChecked.Value;
            _settings.IncludePerspectives = PerspectiveCheckbox.IsChecked.Value;

            _settings.CaseSensitive = CaseCheckbox.IsChecked.Value;
            _settings.SearchTerms = SearchTextBox.Text.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            
            // Save 
            _settings.SaveSettings();
            
            Close();
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // TODO: Validate the chosen separator is not present in this code. Probably using a weird, uncommon char.

        }
    }
}

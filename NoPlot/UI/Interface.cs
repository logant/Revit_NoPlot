using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows;
using Autodesk.Internal.Windows;

namespace NoPlot
{

    public static class Interface
    {

        public static readonly Uri StyleCommonUri = new Uri("pack://application:,,,/NoPlot;component/Styles/Styles.xaml");
        public static readonly Uri ThemeLightUri = new Uri("pack://application:,,,/NoPlot;component/Styles/ColorLight.xaml");
        public static readonly Uri ThemeDarkUri = new Uri("pack://application:,,,/NoPlot;component/Styles/ColorDark.xaml");

        public static List<ResourceDictionary> GetThemeResources(Uri localStyle, int themeIdx = -1)
        {
            List<ResourceDictionary> dicts = new List<ResourceDictionary>();


            var theme = themeIdx > 0 && themeIdx < 3 ? (Theme)themeIdx : DetectTheme();

            ResourceDictionary colorDict = new ResourceDictionary();
            colorDict.Source = theme == Theme.Dark ? ThemeDarkUri : ThemeLightUri;
            dicts.Add(colorDict);

            ResourceDictionary styleDict = new ResourceDictionary();
            styleDict.Source = StyleCommonUri;
            dicts.Add(styleDict);
            if (localStyle != null)
                dicts.Add(new ResourceDictionary() { Source = localStyle });

            return dicts;
        }

        internal static Theme DetectTheme()
        {
            string key = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            int.TryParse(Registry.GetValue(key, "AppsUseLightTheme", -1).ToString(), out int themeIdx);

            return themeIdx == 0 ? Theme.Dark : Theme.Light;
        }
    }
}

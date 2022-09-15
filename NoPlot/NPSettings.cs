using RevitCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoPlot
{
    public class Settings
    {
        // Settings...
        
        public int Theme { get; set; }

        public bool DefaultActive { get; set; }
        //public bool IsActive { get; set; }
        public bool Prompt { get; set; }

        public bool SearchCatNames { get; set; }
        public bool SearchSubcatNames { get; set; }
        public bool SearchFamNames { get; set; }
        public bool SearchTypeNames { get; set; }
        public bool SearchGroupNames { get; set; }

        public bool WhenPrinting { get; set; }
        public bool WhenExportPdf { get; set; }
        public bool WhenExportDwf { get; set; }

        public List<string> SearchTerms { get; set; }
        public bool CaseSensitive { get; set; }

        public Settings()
        {
            // Set defaults
            Theme = 0;
            DefaultActive = true;
            //IsActive = true;
            Prompt = true;
            SearchCatNames = false;
            SearchSubcatNames = true;
            SearchFamNames = true;
            SearchTypeNames = true;
            SearchGroupNames = true;
            WhenPrinting = true;
            WhenExportPdf = true;
            WhenExportDwf = true;
            SearchTerms = new List<string> { "NPLT" };
            CaseSensitive = false;

            LoadSettings();
        }

        public void LoadSettings()
        {
            // Get current settings if saved..
            string assemblyName = this.GetType().Assembly.GetName().Name;

            if (FileUtils.GetInt("Common", nameof(Theme), out int theme))
                Theme = (theme >= 0 && theme <= 2) ? theme : 0;

            if (FileUtils.GetInt(assemblyName, nameof(DefaultActive), out int defActive))
                DefaultActive = defActive == 1;
            if (FileUtils.GetInt(assemblyName, nameof(Prompt), out int prompt))
                Prompt = prompt == 1;

            if (FileUtils.GetInt(assemblyName, nameof(SearchCatNames), out int searchCat))
                SearchCatNames = searchCat == 1;
            if (FileUtils.GetInt(assemblyName, nameof(SearchSubcatNames), out int searchSubs))
                SearchSubcatNames = searchSubs == 1;
            if (FileUtils.GetInt(assemblyName, nameof(SearchFamNames), out int searchFam))
                SearchFamNames = searchFam == 1;
            if (FileUtils.GetInt(assemblyName, nameof(SearchTypeNames), out int searchTypes))
                SearchTypeNames = searchTypes == 1;
            if (FileUtils.GetInt(assemblyName, nameof(SearchGroupNames), out int searchGroups))
                SearchGroupNames = searchGroups == 1;

            if (FileUtils.GetInt(assemblyName, nameof(WhenPrinting), out int whenPrinting))
                WhenPrinting = whenPrinting == 1;
            if (FileUtils.GetInt(assemblyName, nameof(WhenExportPdf), out int whenExpPdf))
                WhenExportPdf = whenExpPdf == 1;
            if (FileUtils.GetInt(assemblyName, nameof(WhenExportDwf), out int whenExpDwf))
                WhenExportDwf = whenExpDwf == 1;
            
            if (FileUtils.GetInt(assemblyName, nameof(CaseSensitive), out int caseSensitive))
                CaseSensitive = caseSensitive == 1;
            if (FileUtils.GetString(assemblyName, nameof(SearchTerms), out string searchTerms))
                SearchTerms = searchTerms.Split(new char[] { '̸' }, StringSplitOptions.RemoveEmptyEntries).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        }

        public void SaveSettings()
        {
            // Write out the settings...
            string assemblyName = this.GetType().Assembly.GetName().Name;

            FileUtils.SetInt("Common", nameof(Theme), Theme);
            FileUtils.SetInt(assemblyName, nameof(DefaultActive), DefaultActive ? 1 : 0);
            FileUtils.SetInt(assemblyName, nameof(Prompt), Prompt ? 1 : 0);

            FileUtils.SetInt(assemblyName, nameof(SearchCatNames), SearchCatNames ? 1 : 0);
            FileUtils.SetInt(assemblyName, nameof(SearchSubcatNames), SearchSubcatNames ? 1 : 0);
            FileUtils.SetInt(assemblyName, nameof(SearchFamNames), SearchFamNames ? 1 : 0);
            FileUtils.SetInt(assemblyName, nameof(SearchTypeNames), SearchTypeNames ? 1 : 0);
            FileUtils.SetInt(assemblyName, nameof(SearchGroupNames), SearchGroupNames ? 1 : 0);

            FileUtils.SetInt(assemblyName, nameof(WhenPrinting), WhenPrinting ? 1 : 0);
            FileUtils.SetInt(assemblyName, nameof(WhenExportPdf), WhenExportPdf ? 1 : 0);
            FileUtils.SetInt(assemblyName, nameof(WhenExportDwf), WhenExportDwf ? 1 : 0);

            FileUtils.SetInt(assemblyName, nameof(CaseSensitive), CaseSensitive ? 1 : 0);
            FileUtils.SetString(assemblyName, nameof(SearchTerms), string.Join("̸", SearchTerms));
        }

    }
}

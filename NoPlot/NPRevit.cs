using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitCommon.Attributes;
using System.IO;
using System.Threading.Tasks;
using RevitCommon;

namespace NoPlot
{

    [ExtApp(Name = "NoPlot", Description = "Adds No Plot functionality to Revit",
        Guid = "79ca195f-118e-4916-9c39-9592f26add86", Vendor = "HKSL", VendorDescription = "HKS LINE, www.hksline.com",
        ForceEnabled = false, Commands = new[] { "No Plot Toggle", "No Plot Settings" })]
    public class NoPlotApp : IExternalApplication
    {
        internal static NoPlotApp npApp = null;
        Document doc;
        List<NoPlotObj> npElements;
        RibbonItem npButton;
        List<Category> npSubCats;
        private List<ElementId> _viewIds = new List<ElementId>();
        private Settings _settings;
        

        //int revitVersion = 2017;

        public static NoPlotApp Instance
        {
            get { return npApp; }
        }
        public bool IsActive { get; set; }
        List<ElementId> npElementIds;

        public Result OnShutdown(UIControlledApplication application)
        {
            // Close the event handlers
            application.ControlledApplication.DocumentPrinting -= Printing;
            application.ControlledApplication.DocumentPrinted -= Printed;

            application.ControlledApplication.FileExporting -= Exporting;
            application.ControlledApplication.FileExported -= Exported;

            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                CheckSettings();
                IsActive = _settings.DefaultActive;
                npApp = this;
               
                // Start the events
                application.ControlledApplication.DocumentPrinting += Printing;
                application.ControlledApplication.DocumentPrinted += Printed;

                application.ControlledApplication.FileExporting += Exporting;
                application.ControlledApplication.FileExported += Exported;

                BitmapSource bms;
                PushButtonData npltPBD;
                if (IsActive)
                {
                    bms = Imaging.CreateBitmapSourceFromHBitmap(Properties.Resources.NoPlotOn.GetHbitmap(), IntPtr.Zero,
                        System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

                    npltPBD = new PushButtonData("No Plot", "No Plot", typeof(NoPlotApp).Assembly.Location,
                        typeof(NoPlotToggleCmd).FullName)
                    {
                        LargeImage = bms,
                        ToolTip =
                            "No Plot functionality is currently on.  Push button to toggle the watcher off for this session."
                    };
                }
                else
                {
                    bms = Imaging.CreateBitmapSourceFromHBitmap(Properties.Resources.NoPlotOff.GetHbitmap(),
                        IntPtr.Zero, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

                    npltPBD = new PushButtonData("No Plot", "No Plot", typeof(NoPlotApp).Assembly.Location,
                        typeof(NoPlotToggleCmd).FullName)
                    {
                        LargeImage = bms,
                        ToolTip =
                            "No Plot functionality is currently off.  Push button to toggle the watcher on for this session."
                    };
                }

                PushButtonData settingsPBD = new PushButtonData("Settings", "Settings",
                    typeof(NoPlotApp).Assembly.Location, typeof(SettingsCmd).FullName)
                {
                    LargeImage = Imaging.CreateBitmapSourceFromHBitmap(Properties.Resources.NoPlotSettings.GetHbitmap(),
                        IntPtr.Zero, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions()),
                    ToolTip = "Settings for the No Plot command."
                };


                string helpPath = Path.Combine(Path.GetDirectoryName(typeof(NoPlotApp).Assembly.Location) ?? string.Empty, "help",
                    "NoPlot.pdf");
                string tabName = "Add-Ins";
                string panelName = "Tools";
                if (RevitCommon.FileUtils.GetPluginSettings(typeof(NoPlotApp).Assembly.GetName().Name,
                    out Dictionary<string, string> settings))
                {
                    // Settings retrieved, lets try to use them.
                    if (settings.ContainsKey("help-path") && !string.IsNullOrWhiteSpace(settings["help-path"]))
                    {
                        // Check to see if it's relative path
                        string hp = Path.Combine(Path.GetDirectoryName(typeof(NoPlotApp).Assembly.Location) ?? string.Empty,
                            settings["help-path"]);
                        if (File.Exists(hp))
                            helpPath = hp;
                        else
                            helpPath = settings["help-path"];
                    }

                    if (settings.ContainsKey("tab-name") && !string.IsNullOrWhiteSpace(settings["tab-name"]))
                        tabName = settings["tab-name"];
                    if (settings.ContainsKey("panel-name") && !string.IsNullOrWhiteSpace(settings["panel-name"]))
                        panelName = settings["panel-name"];
                }

                // Set the help file
                ContextualHelp help = null;
                if (File.Exists(helpPath))
                    help = new ContextualHelp(ContextualHelpType.ChmFile, helpPath);
                else if (Uri.TryCreate(helpPath, UriKind.Absolute, out Uri uriResult) &&
                         (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
                    help = new ContextualHelp(ContextualHelpType.Url, helpPath);
                if (help != null)
                {
                    npltPBD.SetContextualHelp(help);
                    settingsPBD.SetContextualHelp(help);
                }

                SplitButtonData sbd = new SplitButtonData("NoPlot", "No Plot");
                if (help != null)
                    sbd.SetContextualHelp(help);


                // Create the button

                SplitButton sb = RevitCommon.UI.AddToRibbon(application, tabName, panelName, sbd);

                if (help != null)
                    sb.SetContextualHelp(help);

                npButton = sb.AddPushButton(npltPBD);
                sb.AddPushButton(settingsPBD);
                sb.IsSynchronizedWithCurrentItem = false;

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("No Plot Error", ex.ToString());
                return Result.Failed;

            }
        }

       

        public void CheckSettings()
        {
            _settings = new Settings();
        }

        public void ToggleState()
        {
            IsActive = !IsActive;
            
            string currentState = IsActive ? "on" : "off";
            string oppositeState = IsActive ? "off" : "on";
            IntPtr bmp = IsActive ? Properties.Resources.NoPlotOn.GetHbitmap() : Properties.Resources.NoPlotOff.GetHbitmap();
            
            RibbonButton button = npButton as RibbonButton;
            button.ItemText = "No Plot";
            button.ToolTip = $"No Plot functionality is currently {currentState}. Push button to toggle the watcher {oppositeState} for this session.";
            button.LargeImage = Imaging.CreateBitmapSourceFromHBitmap(bmp, IntPtr.Zero, System.Windows.Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            npButton = button;
        }

        #region Printing and Exporting
        public void Printing(object sender, DocumentPrintingEventArgs e)
        {
            // Make sure settings are still accurate
            CheckSettings();

            if (IsActive && _settings.WhenPrinting)
            {
                // Do the no plot thing
                doc = e.Document;
                _viewIds = e.GetViewElementIds().ToList();
                npElements = NoPlotControl.HideNplt(doc, _settings, _viewIds);
            }
        }

        private void Exporting(object sender, FileExportingEventArgs e)
        {
            // Check for settings changes.
            CheckSettings();

            // Run NoPlot for exports
            if (IsActive && (_settings.WhenExportDwf && (e.Format == ImportExportFileFormat.DWF || e.Format == ImportExportFileFormat.DWFX))
#if REVIT2022 
                || (_settings.WhenExportPdf && e.Format == ImportExportFileFormat.PDF))
#else
                ) 
#endif
            {
                doc = e.Document;
                PrintManager pm = doc.PrintManager;
                if (pm.PrintRange == PrintRange.Current || pm.PrintRange == PrintRange.Visible)
                    _viewIds.Add(doc.ActiveView.Id);
                else
                {
                    ViewSet vs = pm.ViewSheetSetting.CurrentViewSheetSet.Views;
                    _viewIds = new List<ElementId>();
                    foreach (View v in vs)
                    {
                        _viewIds.Add(v.Id);
                    }
                }

                npElements = NoPlotControl.HideNplt(doc, _settings, _viewIds);
            }

        }
        #endregion

        #region Printed and Exported
        public void Printed(object sender, DocumentPrintedEventArgs e)
        {
            if (IsActive)
                ResetViews();
            
        }

        

        private void Exported(object sender, FileExportedEventArgs e)
        {
            if (IsActive && (_settings.WhenExportDwf && (e.Format == ImportExportFileFormat.DWF || e.Format == ImportExportFileFormat.DWFX))
#if REVIT2022
                || (_settings.WhenExportPdf && e.Format == ImportExportFileFormat.PDF))
#else
                ) 
#endif
                ResetViews();
            
        }
        #endregion

        public void ResetViews()
        {
            NoPlotControl.ResetViews(doc, npElements);
            _viewIds.Clear();
            FileUtils.WriteToHome(this.GetType().Assembly.GetName().Name, doc.Application.VersionNumber, doc.Application.Username);
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.IO;
using adWin = Autodesk.Windows;

namespace NoPlot
{
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
                /*
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
                */
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

                SplitButton sb = AddToRibbon(application, tabName, panelName, sbd);

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

        /// <summary>
        /// Add a SplitPushButton to Revit
        /// </summary>
        /// <param name="revApp">Revit's UIControlledApplication for adding the button</param>
        /// <param name="tabName">Name of the tab you want to add the button to.</param>
        /// <param name="panelName">Name of the panel on the tab you want to add the button</param>
        /// <param name="button">SplitButtonData object to add to the ribbon.</param>
        /// <returns>If successful, a SplitButton object is returned that can be used to add commands from its drop-down. If unsuccessful, it returns null.</returns>
        public static SplitButton AddToRibbon(UIControlledApplication revApp, string tabName, string panelName, SplitButtonData button)
        {
            RibbonPanel panel = GetRibbonPanel(revApp, tabName, panelName);

            // Add the button to the panel
            if (panel != null)
                return panel.AddItem(button) as SplitButton;
            else
            {
                TaskDialog.Show("Error", "Could not add split button to the Revit ribbon for:\n" + button.Text);
                return null;
            }
        }

        /// <summary>
        /// This is used by the other methods in this class, it's purpose is to find or create the tab and panel specified by
        /// the inputs. If the items do not exist they get created, if they do exist they're found and returned. This should only
        /// be used with a tab name that is non-default to the Revit Ribbon, excepting the Add-Ins tab which is allowed.
        /// </summary>
        /// <param name="revApp">UIControlledApplication from the IExternalApplication's OnStartUp method.</param>
        /// <param name="tabName">Name of the tab a button will be created on. Only Add-Ins is acceptable from the default Revit tabs.</param>
        /// <param name="panelName">Name of the panel the button will be created on.</param>
        /// <returns></returns>
        private static RibbonPanel GetRibbonPanel(UIControlledApplication revApp, string tabName, string panelName)
        {
            try
            {
                // Verify if the tab exists, create it if ncessary
                adWin.RibbonControl ribbon = adWin.ComponentManager.Ribbon;
                adWin.RibbonTab tab = null;
                bool defaultTab = false;

                foreach (adWin.RibbonTab t in ribbon.Tabs)
                {
                    if (t.Id == tabName)
                    {
                        if (t.Id != t.Name)
                            defaultTab = true;
                        tab = t;
                        break;
                    }
                }

                if (!defaultTab && tab == null)
                    revApp.CreateRibbonTab(tabName);
                if (defaultTab)
                    tab = null;

                // Verify if the panel exists
                List<RibbonPanel> panels;
                if (defaultTab)
                    panels = revApp.GetRibbonPanels();
                else
                    panels = revApp.GetRibbonPanels(tabName);

                RibbonPanel panel = null;
                foreach (RibbonPanel rp in panels)
                {
                    if (rp.Name == panelName)
                    {
                        panel = rp;
                        break;
                    }
                }

                if (panel == null && !defaultTab)
                    panel = revApp.CreateRibbonPanel(tabName, panelName);
                else if (panel == null && defaultTab)
                    panel = revApp.CreateRibbonPanel(panelName);

                return panel;
            }
            catch
            {
                return null;
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
                List<ElementId> viewIds = e.GetViewElementIds().ToList();
                //if (_settings.IncludePerspectives)
                    _viewIds = viewIds;
                /*
                else
                {
                    foreach (ElementId viewId in viewIds)
                    {
                        var view = doc.GetElement(viewId) as View;
                        if(view.ViewType != ViewType.ThreeD)
                            _viewIds.Add(viewId);
                        else
                        {
                            View3D view3D = view as View3D;
                            if (!view3D.IsPerspective)
                                _viewIds.Add(viewId);
                        }
                    }
                }
                */
                npElements = NoPlotControl.HideNplt(doc, _settings, _viewIds);
            }
        }

        private void Exporting(object sender, FileExportingEventArgs e)
        {
            // Check for settings changes.
            CheckSettings();

            // Run NoPlot for exports
            if (IsActive && ((_settings.WhenExportDwf && (e.Format == ImportExportFileFormat.DWF || e.Format == ImportExportFileFormat.DWFX))
                || (_settings.WhenExportPdf && e.Format == ImportExportFileFormat.PDF)))
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
                        /*
                        if(!_settings.IncludePerspectives && v.ViewType == ViewType.ThreeD)
                        {
                            if(((View3D)v).IsPerspective)
                                continue;
                            else
                                _viewIds.Add(v.Id);
                        }
                        else*/
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
                || (_settings.WhenExportPdf && e.Format == ImportExportFileFormat.PDF))
                ResetViews();
            
        }
        #endregion

        public void ResetViews()
        {
            NoPlotControl.ResetViews(doc, npElements, _settings);
            _viewIds.Clear();
        }

    }
}

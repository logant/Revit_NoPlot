using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace NoPlot
{
    [Transaction(TransactionMode.Manual)]
    public class SettingsCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                NoPlotSettingsForm form = new NoPlotSettingsForm();
                System.Diagnostics.Process proc = System.Diagnostics.Process.GetCurrentProcess();
                IntPtr handle = proc.MainWindowHandle;

                WindowInteropHelper wih = new WindowInteropHelper(form);
                wih.Owner = handle;

                form.ShowDialog();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }

    [Transaction(TransactionMode.Manual)]
    public class NoPlotToggleCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            NoPlotApp.Instance.ToggleState();
            return Result.Succeeded;
        }
    }

    public class NoPlotApp : IExternalApplication
    {
        internal static NoPlotApp npApp = null;
        bool serviceOn = false;
        Document doc;
        List<NoPlotObj> npElements;
        string npIdentifier = "NPLT";
        RibbonItem npButton;
        List<Category> npSubCats;

        public static NoPlotApp Instance
        {
            get { return npApp; }
        }

        List<ElementId> npElementIds;


        public Result OnShutdown(UIControlledApplication application)
        {
            // Close the event handlers

            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            npApp = this;
            
            // Start the events
            application.ControlledApplication.DocumentPrinting += new EventHandler<DocumentPrintingEventArgs>(Printing);
            application.ControlledApplication.DocumentPrinted += new EventHandler<DocumentPrintedEventArgs>(Printed);

            BitmapSource bms;
            PushButtonData npltPBD;
            serviceOn = Properties.Settings.Default.ServiceState;
            if (serviceOn)
            {
                bms = Imaging.CreateBitmapSourceFromHBitmap(Properties.Resources.NoPlotOn.GetHbitmap(), IntPtr.Zero, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

                npltPBD = new PushButtonData("No Plot", "No Plot", typeof(NoPlotApp).Assembly.Location, "NoPlot.NoPlotToggleCmd")
                {
                    LargeImage = bms,
                    ToolTip = "No Plot functionality is currently on.  Push button to toggle the watcher off for this session."
                };
            }
            else
            {
                bms = Imaging.CreateBitmapSourceFromHBitmap(Properties.Resources.NoPlotOff.GetHbitmap(), IntPtr.Zero, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

                npltPBD = new PushButtonData("No Plot", "No Plot", typeof(NoPlotApp).Assembly.Location, "NoPlot.NoPlotToggleCmd")
                {
                    LargeImage = bms,
                    ToolTip = "No Plot functionality is currently off.  Push button to toggle the watcher on for this session."
                };
            }

            PushButtonData settingsPBD = new PushButtonData("Settings", "Settings", typeof(NoPlotApp).Assembly.Location, "NoPlot.SettingsCmd")
            {
                LargeImage = Imaging.CreateBitmapSourceFromHBitmap(Properties.Resources.NoPlotSettings.GetHbitmap(), IntPtr.Zero, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions()),
                ToolTip = "Settings for the No Plot command."
            };

            SplitButtonData sbd = new SplitButtonData("NoPlot", "No Plot");
            SplitButton sb = RevitCommon.UI.AddToRibbon(application, Properties.Settings.Default.TabName, Properties.Settings.Default.PanelName, sbd);
            npButton = sb.AddPushButton(npltPBD) as PushButton;
            sb.AddPushButton(settingsPBD);
            sb.IsSynchronizedWithCurrentItem = false;

            return Result.Succeeded;
        }

        public void ToggleState()
        {
            if(serviceOn)
            {
                serviceOn = false;

                // Change the button
                RibbonButton button = npButton as RibbonButton;
                button.ItemText = "No Plot";
                button.ToolTip = "No Plot functionality is currently off.  Push button to toggle the watcher on for this session.";
                button.LargeImage = Imaging.CreateBitmapSourceFromHBitmap(Properties.Resources.NoPlotOff.GetHbitmap(), IntPtr.Zero, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                npButton = button;
            }
            else
            {
                serviceOn = true;

                // Change the button
                RibbonButton button = npButton as RibbonButton;
                button.ItemText = "No Plot";
                button.ToolTip = "No Plot functionality is currently on.  Push button to toggle the watcher off for this session.";
                button.LargeImage = Imaging.CreateBitmapSourceFromHBitmap(Properties.Resources.NoPlotOn.GetHbitmap(), IntPtr.Zero, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                npButton = button;
            }
        }

        public void Printing(object sender, DocumentPrintingEventArgs e)
        {
            if (serviceOn)
            {
                bool cont = true;
                if(Properties.Settings.Default.AskBefore)
                {
                    TaskDialog verifyDlg = new TaskDialog("Warning");
                    verifyDlg.TitleAutoPrefix = false;
                    verifyDlg.MainInstruction = "No Plot";
                    verifyDlg.MainContent = "Would you like to print with the No Plot functionality?";
                    verifyDlg.CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No;

                    TaskDialogResult verifyResult = verifyDlg.Show();

                    if (TaskDialogResult.No == verifyResult)
                        cont = false;
                }

                if (cont)
                {
                    // Do the no plot thing
                    doc = e.Document;
                    npIdentifier = Properties.Settings.Default.NoPlotId;
                    
                    // Get a list of elements in the project that have the npIdentifier
                    FilterableValueProvider provider = new ParameterValueProvider(new ElementId(BuiltInParameter.ALL_MODEL_TYPE_NAME));
                    FilterRule rule = new FilterStringRule(provider, new FilterStringContains(), npIdentifier, true);
                    ElementParameterFilter epf = new ElementParameterFilter(rule, false);
                    IEnumerable<ElementId> npElems = new FilteredElementCollector(doc).WherePasses(epf).ToElementIds();
                    npElementIds = npElems.ToList();

                    List<ElementId> views = new List<ElementId>();
                    views.AddRange(e.GetViewElementIds());

                    // Get a list of subcategories to turn off.
                    Categories cats = doc.Settings.Categories;
                    npSubCats = new List<Category>();
                    foreach (Category cat in cats)
                    {
                        foreach (Category sc in cat.SubCategories)
                        {
                            if (sc.Name.Contains(npIdentifier))
                                npSubCats.Add(sc);
                        }
                    }

                    npElements = new List<NoPlotObj>();
                    using (Transaction hideTrans = new Transaction(doc, "Temporary Hide for No Plot"))
                    {
                        hideTrans.Start();
                        foreach (ElementId viewId in views)
                        {
                            ViewSheet sheet = null;
                            try
                            {
                                sheet = doc.GetElement(viewId) as ViewSheet;
                            }
                            catch { }

                            if (sheet != null) // We can safely assume this is a sheet object
                            {
                                View sheetView = doc.GetElement(viewId) as View;
                                TemporaryHide(sheetView);

                                List<View> sheetViews = new List<View>();
                                foreach (ElementId vid in sheet.GetAllPlacedViews())
                                {
                                    sheetViews.Add(doc.GetElement(vid) as View);
                                }

                                foreach (View v in sheetViews) { TemporaryHide(v); }
                            }

                            else  // The view is not a sheet but another view type
                            {
                                View view = doc.GetElement(viewId) as View;
                                TemporaryHide(view);
                            }
                        }
                        hideTrans.Commit();
                    }
                }
            }
        }

        public void Printed(object sender, DocumentPrintedEventArgs e)
        {
            if (serviceOn)
            {
                ResetViews();
                
                // Write back to home about it...
                doc = e.Document;
                string userName = doc.Application.Username;
                string commandName = "No Plot";
                string appVersion = doc.Application.VersionNumber;
                RevitCommon.HKS.WriteToHome(commandName, appVersion, userName);
            }
        }

        public void TemporaryHide(View view)
        {
            // Get the view template and then turn it off so subcategories can be hidden
            ElementId viewTemplateId = view.ViewTemplateId;
            view.ViewTemplateId = new ElementId(-1);

            //Temporary hide all of the subcateogries in the subcat list
            List<Category> hiddenSubCats = new List<Category>();
            foreach (Category cat in npSubCats)
            {
                try
                {
                    if (view.GetVisibility(cat))
                    {

                        hiddenSubCats.Add(cat);
                        view.SetVisibility(cat, false);
                    }
                }
                catch { } // Subcategory does not exist, ie model category in drafting view.
            }
            
            // Hide the NP Elements
            view.HideElementsTemporary(npElementIds);

            // Store the information so that we can unhide it afterwards
            NoPlotObj np = new NoPlotObj();
            np.NPElements = npElementIds;
            np.SubCategories = hiddenSubCats;
            np.View = view.Id;
            np.ViewTemplate = viewTemplateId;
            npElements.Add(np);
        }

        public void ResetViews()
        {
            using (Transaction resetTrans = new Transaction(doc, "Reset No Plot View States"))
            {
                resetTrans.Start();
                foreach (NoPlotObj np in npElements)
                {
                    View v = doc.GetElement(np.View) as View;

                    // Reset the temporary hide
                    v.DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate);

                    // Assign the original view template
                    if (np.ViewTemplate.IntegerValue != -1)
                    {
                        v.ViewTemplateId = np.ViewTemplate;
                    }
                    // Reset each subcategory manually
                    else
                    {
                        foreach (Category c in np.SubCategories)
                        {
                            v.SetVisibility(c, true);
                        }
                    }
                }
                resetTrans.Commit();
            }
        }

    }
}

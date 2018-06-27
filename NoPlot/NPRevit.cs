using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Xml;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitCommon.Attributes;


namespace NoPlot
{
    [Transaction(TransactionMode.Manual)]
    public class SettingsCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Get the version and set the handle var.
                int.TryParse(commandData.Application.Application.VersionNumber, out int version);
                IntPtr handle = IntPtr.Zero;
                if (version < 2019)
                    handle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
                else
                    handle = commandData.Application.GetType().GetProperty("MainWindowHandle") != null
                        ? (IntPtr)commandData.Application.GetType().GetProperty("MainWindowHandle").GetValue(commandData.Application)
                        : IntPtr.Zero;

                // Set the handle to the window
                NoPlotSettingsForm form = new NoPlotSettingsForm();
                var wih = new WindowInteropHelper(form)
                {
                    Owner = handle
                };

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

    [ExtApp(Name = "NoPlot", Description = "Adds No Plot functionality to Revit",
        Guid = "79ca195f-118e-4916-9c39-9592f26add86", Vendor = "HKSL", VendorDescription = "HKS LINE, www.hksline.com",
        ForceEnabled = false, Commands = new[] { "No Plot Toggle", "No Plot Settings" })]
    public class NoPlotApp : IExternalApplication
    {
        internal static NoPlotApp npApp = null;
        bool serviceOn = false;
        Document doc;
        List<NoPlotObj> npElements;
        string npIdentifier = "NPLT";
        RibbonItem npButton;
        List<Category> npSubCats;

        int revitVersion = 2017;

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
            revitVersion = Convert.ToInt32(application.ControlledApplication.VersionNumber);
            
            // Start the events
            application.ControlledApplication.DocumentPrinting += new EventHandler<DocumentPrintingEventArgs>(Printing);
            application.ControlledApplication.DocumentPrinted += new EventHandler<DocumentPrintedEventArgs>(Printed);

            BitmapSource bms;
            PushButtonData npltPBD;
            serviceOn = Properties.Settings.Default.ServiceState;
            if (serviceOn)
            {
                bms = Imaging.CreateBitmapSourceFromHBitmap(Properties.Resources.NoPlotOn.GetHbitmap(), IntPtr.Zero, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

                npltPBD = new PushButtonData("No Plot", "No Plot", typeof(NoPlotApp).Assembly.Location, typeof(NoPlotToggleCmd).FullName)
                {
                    LargeImage = bms,
                    ToolTip = "No Plot functionality is currently on.  Push button to toggle the watcher off for this session."
                };
            }
            else
            {
                bms = Imaging.CreateBitmapSourceFromHBitmap(Properties.Resources.NoPlotOff.GetHbitmap(), IntPtr.Zero, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

                npltPBD = new PushButtonData("No Plot", "No Plot", typeof(NoPlotApp).Assembly.Location, typeof(NoPlotToggleCmd).FullName)
                {
                    LargeImage = bms,
                    ToolTip = "No Plot functionality is currently off.  Push button to toggle the watcher on for this session."
                };
            }

            PushButtonData settingsPBD = new PushButtonData("Settings", "Settings", typeof(NoPlotApp).Assembly.Location, typeof(SettingsCmd).FullName)
            {
                LargeImage = Imaging.CreateBitmapSourceFromHBitmap(Properties.Resources.NoPlotSettings.GetHbitmap(), IntPtr.Zero, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions()),
                ToolTip = "Settings for the No Plot command."
            };

            // Check for a settings file
            if (!RevitCommon.FileUtils.GetPluginSettings(typeof(NoPlotApp).Assembly.GetName().Name, out string helpPath, out string tabName, out string panelName))
            {
                // Set the help file path
                System.IO.FileInfo fi = new System.IO.FileInfo(typeof(NoPlotApp).Assembly.Location);
                System.IO.DirectoryInfo directory = fi.Directory;
                helpPath = directory.FullName + "\\help\\NoPlot.pdf";

                // Set the tab name
                tabName = Properties.Settings.Default.TabName;
                panelName = Properties.Settings.Default.PanelName;
            }
            else
            {
                // Check for nulls in the returned strings
                if (helpPath == null)
                {
                    // Set the help file path
                    System.IO.FileInfo fi = new System.IO.FileInfo(typeof(NoPlotApp).Assembly.Location);
                    System.IO.DirectoryInfo directory = fi.Directory;
                    helpPath = directory.FullName + "\\help\\NoPlot.pdf";
                }

                if (tabName == null)
                    tabName = Properties.Settings.Default.TabName;

                if (panelName == null)
                    panelName = Properties.Settings.Default.PanelName;
            }


            // Help File
            // ******************************************
            ContextualHelp help = null;
            if (System.IO.File.Exists(helpPath))
            {
                help = new ContextualHelp(ContextualHelpType.ChmFile, helpPath);
                npltPBD.SetContextualHelp(help);

                ContextualHelp settingsHelp = new ContextualHelp(ContextualHelpType.ChmFile, helpPath);
                settingsPBD.SetContextualHelp(settingsHelp);
            }
            

            // ******************************************
            // End of Help File

            SplitButtonData sbd = new SplitButtonData("NoPlot", "No Plot");
            if(help != null)
                sbd.SetContextualHelp(help);
            

            // Create the button
            SplitButton sb = RevitCommon.UI.AddToRibbon(application, tabName, panelName, sbd);
            if(help != null)
                sb.SetContextualHelp(help);

            npButton = sb.AddPushButton(npltPBD);
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
                // Do the no plot thing
                doc = e.Document;
                npIdentifier = Properties.Settings.Default.NoPlotId;
                // Check to see if there are even any NPLT elements in the project.
                bool npltFound = false;
                
                // First check subcategories
                foreach (Category cat in doc.Settings.Categories)
                {
                    foreach(Category subCat in cat.SubCategories)
                    {
                        if(subCat.Name.Contains(npIdentifier))
                        {
                            npltFound = true;
                            break;
                        }
                    }

                    if (npltFound)
                        break;
                }
                if (!npltFound)
                {
                    // Check ElementTypes
                    //Get a list of elements in the project that have the npIdentifier in the type name
                    FilterableValueProvider provider = new ParameterValueProvider(new ElementId(BuiltInParameter.ALL_MODEL_TYPE_NAME));
                    FilterRule rule = new FilterStringRule(provider, new FilterStringContains(), npIdentifier, true);
                    ElementParameterFilter epf = new ElementParameterFilter(rule, false);
                    if(new FilteredElementCollector(doc).WherePasses(epf).ToElementIds().Count > 0)
                    {
                        npltFound = true;
                    }
                }
                if (!npltFound)
                {
                    // Check ElementTypes
                    //Get a list of elements in the project that have the npIdentifier in the type name
                    FilterableValueProvider providerFam = new ParameterValueProvider(new ElementId(BuiltInParameter.ALL_MODEL_FAMILY_NAME));
                    FilterRule ruleFam = new FilterStringRule(providerFam, new FilterStringContains(), npIdentifier, true);
                    ElementParameterFilter epfFam = new ElementParameterFilter(ruleFam, false);
                    new FilteredElementCollector(doc).WherePasses(epfFam).ToElementIds();
                    
                    if (new FilteredElementCollector(doc).WherePasses(epfFam).ToElementIds().Count > 0)
                    {
                        npltFound = true;
                    }
                }
                if(!npltFound)
                {
                    IEnumerable<ElementId> npGroupElems = new FilteredElementCollector(doc).OfClass(typeof(Group)).ToElementIds();
                    foreach (ElementId eid in npGroupElems)
                    {
                        Element gElem = doc.GetElement(eid);
                        if (gElem.Name.Contains(npIdentifier))
                        {
                            npltFound = true;
                            break;
                        }
                    }
                }

                if (!npltFound)
                    return;

                bool cont = true;
                if(Properties.Settings.Default.AskBefore)
                {


                    TaskDialog verifyDlg = new TaskDialog("Warning")
                    {
                        TitleAutoPrefix = false,
                        MainInstruction = "No Plot is Active",
                        MainContent = "Continue hiding objects with '" + npIdentifier + "' in their name for this print?",
                        CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No
                    };

                    TaskDialogResult verifyResult = verifyDlg.Show();

                    if (TaskDialogResult.No == verifyResult)
                        cont = false;
                }

                if (cont)
                {
                    // Do the no plot thing
                    doc = e.Document;
                    npIdentifier = Properties.Settings.Default.NoPlotId;
                    
                    // Get a list of elements in the project that have the npIdentifier in the type name
                    FilterableValueProvider provider = new ParameterValueProvider(new ElementId(BuiltInParameter.ALL_MODEL_TYPE_NAME));
                    FilterRule rule = new FilterStringRule(provider, new FilterStringContains(), npIdentifier, true);
                    ElementParameterFilter epf = new ElementParameterFilter(rule, false);
                    IEnumerable<ElementId> npElems = new FilteredElementCollector(doc).WherePasses(epf).ToElementIds();
                    npElementIds = npElems.ToList();

                    FilterableValueProvider providerFam = new ParameterValueProvider(new ElementId(BuiltInParameter.ALL_MODEL_FAMILY_NAME));
                    FilterRule ruleFam = new FilterStringRule(providerFam, new FilterStringContains(), npIdentifier, true);
                    ElementParameterFilter epfFam = new ElementParameterFilter(ruleFam, false);
                    IEnumerable<ElementId> npElemsFam = new FilteredElementCollector(doc).WherePasses(epfFam).ToElementIds();
                    npElementIds.AddRange(npElemsFam.ToList());

                    IEnumerable<ElementId> npGroupElems = new FilteredElementCollector(doc).OfClass(typeof(Group)).ToElementIds();
                    foreach(ElementId eid in npGroupElems)
                    {
                        Element gElem = doc.GetElement(eid);
                        if(gElem.Name.Contains(npIdentifier))
                        {
                            Group g = gElem as Group;
                            npElementIds.AddRange(g.GetMemberIds());
                        }
                    }

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

                RevitCommon.FileUtils.WriteToHome(commandName, appVersion, userName);
            }
        }

        public void TemporaryHide(View view)
        {
            // Get the view template and then turn it off so subcategories can be hidden
            ElementId viewTemplateId = view.ViewTemplateId;
            view.ViewTemplateId = new ElementId(-1);

            // API for a view's category visibility changes at Revit 2018, so reflection is used to find the right method call.
            Type viewType = view.GetType();
            MethodInfo catVisMethod = null;
            if (revitVersion > 2017)
                catVisMethod = viewType.GetMethod("GetCategoryHidden");
            else
                catVisMethod = viewType.GetMethod("GetVisibility");

            MethodInfo catHideMethod = null;
            MethodInfo canHideMethod = null;
            if (revitVersion > 2017)
            {
                catHideMethod = viewType.GetMethod("SetCategoryHidden");
                canHideMethod = viewType.GetMethod("CanCategoryBeHidden");
            }
            else
                catHideMethod = viewType.GetMethod("SetVisibility");


            //Temporary hide all of the subcateogries in the subcat list
            List<Category> hiddenSubCats = new List<Category>();
            foreach (Category cat in npSubCats)
            {
                try
                {
                    //<=2017 : Check if a category is visible
                    //view.GetVisibility(cat);
                    //view.SetVisibility(cat, bool visible);

                    //2018   : Check if a category is hidden in the view.
                    //view.GetCategoryHidden(cat.Id);
                    //view.SetCategoryHidden(cat.Id, bool hidden);

                    if(revitVersion > 2017)
                    {
                        // Check to see if the category can be hidden, ie if it exists or is otherwise not locked
                        bool canHide = Convert.ToBoolean(canHideMethod.Invoke(view, new object[] { cat.Id }));
                        if (canHide)
                        {
                            object[] paramArr = new object[] {cat.Id};
                            var result = catVisMethod.Invoke(view, paramArr);
                            // A result of false means a category is not hidden, aka is visible.
                            // If that is the case, we need to hide it
                            if ((bool) result == false)
                            {
                                hiddenSubCats.Add(cat);
                                object[] setParamArr = new object[] {cat.Id, true};
                                catHideMethod.Invoke(view, setParamArr);
                            }
                        }
                    }
                    else // 2017 and earlier versions
                    {
                        object[] paramArr = new object[] { cat };
                        var result = catVisMethod.Invoke(view, paramArr);
                        // A result of true means a category is visible.
                        // If that is the case, we need to hide it
                        if ((bool)result == true)
                        {
                            hiddenSubCats.Add(cat);
                            object[] setParamArr = new object[] { cat, false };
                            catHideMethod.Invoke(view, setParamArr);
                        }
                    }
                }
                catch { } // Subcategory does not exist, ie model category in drafting view.
            }
            
            // Hide the NP Elements
            view.HideElementsTemporary(npElementIds);

            // Store the information so that we can unhide it afterwards
            NoPlotObj np = new NoPlotObj
            {
                NPElements = npElementIds,
                SubCategories = hiddenSubCats,
                View = view.Id,
                ViewTemplate = viewTemplateId
            };
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
                    Type viewType = v.GetType();
                    MethodInfo catHideMethod = null;
                    MethodInfo canHideMethod = null;
                    if (revitVersion > 2017)
                    {
                        catHideMethod = viewType.GetMethod("SetCategoryHidden");
                        canHideMethod = viewType.GetMethod("CanCategoryBeHidden");
                    }
                    else
                    {
                        catHideMethod = viewType.GetMethod("SetVisibility");
                    }

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
                            try
                            {
                                //<=2017 : Check if a category is visible
                                //view.GetVisibility(cat);
                                //view.SetVisibility(cat, bool visible);

                                //2018   : Check if a category is hidden in the view.
                                //view.GetCategoryHidden(cat.Id);
                                //view.SetCategoryHidden(cat.Id, bool hide);
                                if (revitVersion > 2017)
                                {
                                    // Check to see if the category can be hidden, ie if it exists or is otherwise not locked
                                    bool canHide = Convert.ToBoolean(canHideMethod.Invoke(v, new object[] {c.Id}));
                                    if (canHide)
                                    {
                                        object[] setParamArr = new object[] {c.Id, false};
                                        catHideMethod.Invoke(v, setParamArr);
                                    }
                                }
                                else
                                {
                                    object[] setParamArr = new object[] { c, true };
                                    catHideMethod.Invoke(v, setParamArr);
                                }
                            }
                            catch (Exception e)
                            {
                                TaskDialog.Show("Error", e.Message);
                            }
                            
                        }
                    }
                }
                resetTrans.Commit();
            }
        }

    }
}

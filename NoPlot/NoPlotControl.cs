using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;


namespace NoPlot
{
    public static class NoPlotControl
    {
        public static void ResetViews(Document doc, List<NoPlotObj> objs)
        {
            using (Transaction t = new Transaction(doc, "Reset Views - NoPlot"))
            {
                t.Start();

                foreach (var obj in objs)
                {
                    View v = doc.GetElement(obj.View) as View;
                    
                    // Reset the temporary hide
                    v.DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate);


                    // Turn categories back on
                    foreach (Category c in obj.SubCategories)
                    {
                        if(v.CanCategoryBeHidden(c.Id))
                            v.SetCategoryHidden(c.Id, false);
                    }

                    // Reset ViewProperties
                    if (!obj.TempEnabled && v.CanEnableTemporaryViewPropertiesMode())
                        v.DisableTemporaryViewMode(TemporaryViewMode.TemporaryViewProperties);

                }

                t.Commit();
            }
        }

        public static List<NoPlotObj> HideNplt(Document doc, Settings settings, List<ElementId> viewIds)
        {
            // Check if NP items are in the document
            bool npFound = ((settings.SearchCatNames || settings.SearchSubcatNames) && SearchCategories(doc.Settings.Categories, settings)) ||
                            (settings.SearchFamNames && SearchNames(doc, settings, BuiltInParameter.ELEM_FAMILY_PARAM)) ||
                            (settings.SearchTypeNames && SearchNames(doc, settings, BuiltInParameter.ELEM_TYPE_PARAM)) ||
                            (settings.SearchGroupNames && SearchGroups(doc, settings));

            if (!npFound || (settings.Prompt && !PromptUser()))
                return new List<NoPlotObj>();

            List<NoPlotObj> npObjs = new List<NoPlotObj>();
            List<ElementId> allNPElemIds = GetElements(doc, settings);
            List<Category> allCategories = GetCategories(doc.Settings.Categories, settings);
            foreach (var vid in viewIds)
            {
                View v = doc.GetElement(vid) as View;
                npObjs.Add(GetNpo(v, allCategories, allNPElemIds));
                if (v.ViewType == ViewType.DrawingSheet)
                {
                    ViewSheet vs = v as ViewSheet;
                    foreach (ElementId vpid in vs.GetAllPlacedViews())
                    {
                        View vp = doc.GetElement(vpid) as View;
                        npObjs.Add(GetNpo(vp, allCategories, allNPElemIds));
                    }
                }
            }

            ProcessNP(doc, npObjs);

            return npObjs;
        }

        private static NoPlotObj GetNpo(View v, List<Category> allCats, List<ElementId> allElemIds)
        {
            NoPlotObj npo = new NoPlotObj();
            npo.View = v.Id;
            npo.SubCategories = new List<Category>();
            //npo.SubCategories = allCats;
            npo.NPElements = allElemIds;
            npo.TempEnabled = v.IsTemporaryViewPropertiesModeEnabled();
            
            foreach (var cat in allCats)
            {
                bool canBeHidden = v.CanCategoryBeHidden(cat.Id);
                bool isHidden = v.GetCategoryHidden(cat.Id);
                //if (v.CanCategoryBeHidden(cat.Id) && !v.GetCategoryHidden(cat.Id))
                if(!isHidden)
                    npo.SubCategories.Add(cat);
            }
            
            return npo;
        }

        private static void ProcessNP(Document doc, List<NoPlotObj> objs)
        {
            using (Transaction t = new Transaction(doc, "Temporary Hide - NoPlot"))
            {
                t.Start();

                foreach (var obj in objs)
                {
                    View v = doc.GetElement(obj.View) as View;
                    try
                    {
                        
                        if (!obj.TempEnabled && v.ViewType != ViewType.DrawingSheet && v.CanEnableTemporaryViewPropertiesMode())
                            v.EnableTemporaryViewPropertiesMode(v.Id);
                        foreach (Category c in obj.SubCategories)
                        {
                            if (v.CanCategoryBeHidden(c.Id))
                                v.SetCategoryHidden(c.Id, true);
                        }
                        v.HideElementsTemporary(obj.NPElements);
                    }
                    catch (Exception e)
                    {
                        string m = e.Message;
                    }
                }

                t.Commit();
            }
        }

        private static bool PromptUser()
        {
            TaskDialog verifyDlg = new TaskDialog("Warning")
            {
                TitleAutoPrefix = false,
                MainInstruction = "No Plot is Active",
                MainContent = "Hide elements that match the No Plot settings for this print?",
                CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No
            };

            TaskDialogResult verifyResult = verifyDlg.Show();

            return TaskDialogResult.Yes == verifyResult;
        }

        private static bool SearchCategories(Categories cats, Settings settings)
        {
            List<string> search = settings.CaseSensitive ? settings.SearchTerms : settings.SearchTerms.ConvertAll(st => st.ToLower());

            foreach (Category c in cats)
            {
                if(settings.SearchCatNames && (settings.CaseSensitive ? search.Any(c.Name.Contains) : search.Any(c.Name.ToLower().Contains)))
                    return true;
                if (!settings.SearchSubcatNames)
                    continue;
                foreach (Category sc in c.SubCategories)
                {
                    if (c.Id.IntegerValue == (int)BuiltInCategory.OST_GenericModel)
                    {
                        string test = sc.Name;
                    }

                    if (settings.CaseSensitive ? search.Any(sc.Name.Contains) : search.Any(sc.Name.ToLower().Contains))
                        return true;
                }
            }

            return false;
        }

        private static bool SearchNames(Document doc, Settings settings, BuiltInParameter bip)
        {
            FilterableValueProvider provider = new ParameterValueProvider(new ElementId(bip));
            List<ElementFilter> filters = new List<ElementFilter>();
            foreach (string st in settings.SearchTerms)
            {
#if REVIT2022
                FilterRule rule = new FilterStringRule(provider, new FilterStringContains(), st);
#else
                FilterRule rule = new FilterStringRule(provider, new FilterStringContains(), st, settings.CaseSensitive);
#endif
                filters.Add(new ElementParameterFilter(rule));
            }
            LogicalOrFilter filter = new LogicalOrFilter(filters);

            return settings.CaseSensitive
                ? new FilteredElementCollector(doc).WherePasses(filter).ToElements()
                    .Any(e => settings.SearchTerms.Any(e.get_Parameter(bip).AsValueString().Contains))
                : new FilteredElementCollector(doc).WherePasses(filter).Any();
        }

        private static bool SearchGroups(Document doc, Settings settings)
        {
            List<string> search = settings.CaseSensitive ? settings.SearchTerms : settings.SearchTerms.ConvertAll(st => st.ToLower());
            return settings.CaseSensitive
                ? new FilteredElementCollector(doc).OfClass(typeof(Group)).ToElements()
                    .Any(e => search.Any(e.Name.Contains))
                : new FilteredElementCollector(doc).OfClass(typeof(Group)).ToElements()
                    .Any(e => search.Any(e.Name.ToLower().Contains));
        }

        private static List<Category> GetCategories(Categories cats, Settings settings)
        {
            List<string> search = settings.CaseSensitive ? settings.SearchTerms : settings.SearchTerms.ConvertAll(st => st.ToLower());
            List<Category> npcats = new List<Category>();
            foreach (Category c in cats)
            {
                if (settings.SearchCatNames && (settings.CaseSensitive ? search.Any(c.Name.Contains) : search.Any(c.Name.ToLower().Contains)))
                    npcats.Add(c);
                if (!settings.SearchSubcatNames)
                    continue;
                foreach (Category sc in c.SubCategories)
                {
                    if (settings.CaseSensitive ? search.Any(sc.Name.Contains) : search.Any(sc.Name.ToLower().Contains))
                        npcats.Add(sc);
                }
            }

            return npcats;
        }

        private static List<ElementId> GetElements(Document doc, Settings settings)
        {
            List<ElementId> npIds = new List<ElementId>();
            List<string> search = settings.CaseSensitive ? settings.SearchTerms : settings.SearchTerms.ConvertAll(st => st.ToLower());
            // Generate a set of filters for family or type names
            List<ElementFilter> famFilters = new List<ElementFilter>();
            List<ElementFilter> typFilters = new List<ElementFilter>();
            var fProvider = new ParameterValueProvider(new ElementId(BuiltInParameter.ELEM_FAMILY_PARAM));
            var tProvider = new ParameterValueProvider(new ElementId(BuiltInParameter.ELEM_TYPE_PARAM));
            foreach (string st in settings.SearchTerms)
            {
                if (settings.SearchFamNames)
                {
#if REVIT2022
                    var fRule = new FilterStringRule(fProvider, new FilterStringContains(), st);
#else
                    var fRule = new FilterStringRule(fProvider, new FilterStringContains(), st, settings.CaseSensitive);
#endif
                    famFilters.Add(new ElementParameterFilter(fRule));
                }

                if (settings.SearchTypeNames)
                {
#if REVIT2022
                    var tRule = new FilterStringRule(tProvider, new FilterStringContains(), st);
#else
                    var tRule = new FilterStringRule(tProvider, new FilterStringContains(), st, settings.CaseSensitive);
#endif
                    typFilters.Add(new ElementParameterFilter(tRule));
                }
            }
            // Search family names...
            if (famFilters.Count > 0)
            {
                LogicalOrFilter filter = new LogicalOrFilter(famFilters);
                var elems = settings.CaseSensitive
                    ? new FilteredElementCollector(doc).WherePasses(filter).ToElements().Where(e =>
                        search.Any(e.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM).AsValueString().Contains)).Select(e => e.Id).ToList()
                    : new FilteredElementCollector(doc).WherePasses(filter).ToElementIds().ToList();
                npIds.AddRange(elems);
            }
            // Search Type Names...
            if (typFilters.Count > 0)
            {
                LogicalOrFilter filter = new LogicalOrFilter(typFilters);
                var elems = settings.CaseSensitive
                    ? new FilteredElementCollector(doc).WherePasses(filter).ToElements().Where(e =>
                        search.Any(e.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM).AsValueString().Contains)).Select(e => e.Id).ToList()
                    : new FilteredElementCollector(doc).WherePasses(filter).ToElementIds().ToList();
                npIds.AddRange(elems);
            }

            // Search Group Names
            if (settings.SearchGroupNames)
            {
                var groups = settings.CaseSensitive
                    ? new FilteredElementCollector(doc).OfClass(typeof(Group)).ToElements()
                        .Where(e => search.Any(e.Name.Contains)).ToList()
                    : new FilteredElementCollector(doc).OfClass(typeof(Group)).ToElements()
                        .Where(e => search.Any(e.Name.ToLower().Contains)).ToList();
                foreach (var elem in groups)
                {
                    if (elem is Group)
                    {
                        Group grp = elem as Group;
                        npIds.Add(grp.Id);
                        npIds.AddRange(grp.GetMemberIds());
                    }
                }
            }

            return npIds;
        }
    }
}

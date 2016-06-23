using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace NoPlot
{
    public class NoPlotObj
    {
        public ElementId View { get; set; }
        public IList<ElementId> NPElements { get; set; }
        public List<Category> SubCategories { get; set; }
        public ElementId ViewTemplate { get; set; }
    }
}

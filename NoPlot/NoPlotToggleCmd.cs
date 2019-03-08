using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace NoPlot
{
    [Transaction(TransactionMode.Manual)]
    public class NoPlotToggleCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            NoPlotApp.Instance.ToggleState();
            return Result.Succeeded;
        }
    }
}

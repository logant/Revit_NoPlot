using System;
using System.Windows.Interop;
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
                IntPtr handle = commandData.Application.MainWindowHandle;
               
                SettingsWindow form = new SettingsWindow();
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
}

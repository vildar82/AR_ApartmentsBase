using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.Revit.DB;

namespace Revit_FlatExporter
{
    [TransactionAttribute(TransactionMode.Manual)]
    [RegenerationAttribute(RegenerationOption.Manual)]
    public class ExternalCommands : IExternalCommand
    {
      
        public Result Execute(ExternalCommandData commandData, ref string message, Autodesk.Revit.DB.ElementSet elements)
        {
            UIApplication appRevit = commandData.Application;
            Document doc = appRevit.ActiveUIDocument.Document;
            var selection = appRevit.ActiveUIDocument.Selection.GetElementIds();
            List<Element> scaningElements = new List<Element>();
            foreach (var elId1 in selection)
            {
                Element el1 = doc.GetElement(elId1);
                Group gr1 = el1 as Group;            //Группа первого уровня(содержит в себе три группы: фасад, ар, стены)
                if (gr1 == null) continue;
                foreach (var elId2 in gr1.GetMemberIds())
                {
                    Element el2 = doc.GetElement(elId2);
                    Group gr2 = el2 as Group;            //Группа второго уровня (группы фасад, стена, ар)
                    if (gr2 == null) continue;
                    foreach (var elId3 in gr2.GetMemberIds())
                    {
                        Element el3 = doc.GetElement(elId3);
                        Group gr3 = el3 as Group;            //Группа третьего уровня (в группе ар находятся группы СУ)
                        if (gr3 != null)
                        {
                            foreach (var elId4 in gr3.GetMemberIds())
                            {
                                Element el4 = doc.GetElement(elId4);
                                if ((el4.Category == null||!Helper.Categories.Any(x=>x.Equals(el4.Category.Name)))) continue;
                                if (!(el4 is Wall)) continue;
                                Wall w = el4 as Wall;
                                ElementInfo el = new ElementInfo();
                                el.CategoryName = w.Category.Name;
                                scaningElements.Add(el4);
                            }
                        }
                        else
                        {
                            if ((el3.Category == null || !Helper.Categories.Any(x => x.Equals(el3.Category.Name)))) continue;
                            if (!(el3 is Wall)) continue;
                            scaningElements.Add(el3);
                        }
                    }
                }
            }
            scaningElements = scaningElements.OrderBy(x => x.Category.Name).ToList();
            if (scaningElements.Count == 0)
            {
                
            }
            // MessageBox.Show("В разработке!");
            //ExternalApplication extApplication = new ExternalApplication();
            //extApplication.ShowMainForm(appRevit);
            return Result.Succeeded;
        }
    }
}

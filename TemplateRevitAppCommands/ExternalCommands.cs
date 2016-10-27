using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AR_ApartmentBase.Model;
using AR_ApartmentBase.Model.Elements;
using Autodesk.Revit.DB;
using Element = Autodesk.Revit.DB.Element;

namespace Revit_FlatExporter
{
    [TransactionAttribute(TransactionMode.Manual)]
    [RegenerationAttribute(RegenerationOption.Manual)]
    public class ExternalCommands : IExternalCommand
    {
        ProcessManager pm = new ProcessManager();
        //public List<Element> GetRevitElements(Group group, Document doc)
        //{
        //    List<Element> scaningElements = new List<Element>();

        //    foreach (var elId2 in group.GetMemberIds())      //Группа первого уровня(содержит в себе три группы: фасад, ар, стены)
        //    {
        //        Element el2 = doc.GetElement(elId2);
        //        Group gr2 = el2 as Group; //Группа второго уровня (группы фасад, стена, ар)
        //        if (gr2 == null) continue;
        //        foreach (var elId3 in gr2.GetMemberIds())
        //        {
        //            Element el3 = doc.GetElement(elId3);
        //            Group gr3 = el3 as Group; //Группа третьего уровня (в группе ар находятся группы СУ)
        //            if (gr3 != null)
        //            {
        //                foreach (var elId4 in gr3.GetMemberIds())
        //                {
        //                    Element el4 = doc.GetElement(elId4);
        //                    if ((el4.Category == null || !Helper.Categories.Any(x => x.Equals(el4.Category.Name))))
        //                        continue;
        //                    //if (!(el4 is Wall)) continue;
        //                    //Wall w = el4 as Wall;
        //                    //ElementInfo el = new ElementInfo();
        //                    //el.CategoryElement = w.Category.Name;
        //                    //el.Direction = Helper.ConvertXyzToString(((w.Location as LocationCurve).Curve as Line).Direction);
        //                    //apartment.Elements.Add(el);
        //                    scaningElements.Add(el4);
        //                }
        //            }
        //            else
        //            {
        //                if ((el3.Category == null || !Helper.Categories.Any(x => x.Equals(el3.Category.Name))))
        //                    continue;
        //                //  if (!(el3 is Wall)) continue;
        //                scaningElements.Add(el3);
        //            }
        //        }
        //    }
        //    return scaningElements;
        //}

        public Result Execute(ExternalCommandData commandData, ref string message, Autodesk.Revit.DB.ElementSet elements)
        {
            UIApplication appRevit = commandData.Application;
            Document doc = appRevit.ActiveUIDocument.Document;
            var selection = appRevit.ActiveUIDocument.Selection.GetElementIds();

            foreach (var elId1 in selection)
            {
                Element el1 = doc.GetElement(elId1);
                Group gr1 = el1 as Group;            //Группа первого уровня(содержит в себе три группы: фасад, ар, стены)
                List<Element> scaningElements = pm.GetRevitElements(gr1, doc);
                if (scaningElements.Count == 0) continue;
                scaningElements = scaningElements.OrderBy(x => x.Category.Name).ToList();
                Apartment apartment = new Apartment();
                apartment.Elements = new List<IElement>();
                foreach (var scanElement in scaningElements)
                {
                    if (!(scanElement is Wall)) continue;
                    Wall w = scanElement as Wall;
                    DBWall el = new DBWall();
                    el.CategoryElement = w.Category.Name;
                    Line lineWall = (w.Location as LocationCurve).Curve as Line;
                    el.Direction = Helper.ConvertXyzToString(lineWall.Direction);
                    el.FamilyName = w.WallType.FamilyName;
                    el.FamilySymbolName = w.WallType.Name;
                    el.LocationPoint = Helper.ConvertXyzToString(lineWall.Origin);
                    apartment.Elements.Add(el);
                    el.
                }
            }
            //scaningElements = scaningElements.OrderBy(x => x.Category.Name).ToList();
            //if (scaningElements.Count == 0)
            //{

            //}
            // MessageBox.Show("В разработке!");
            //ExternalApplication extApplication = new ExternalApplication();
            //extApplication.ShowMainForm(appRevit);
            return Result.Succeeded;
        }
    }
}

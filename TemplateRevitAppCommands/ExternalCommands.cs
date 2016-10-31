using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AR_ApartmentBase.Model;
using AR_ApartmentBase.Model.DB.EntityModel;
using AR_ApartmentBase.Model.Elements;
using Autodesk.Revit.DB;
using Element = Autodesk.Revit.DB.Element;
using Parameter = AR_ApartmentBase.Model.Parameter;

namespace Revit_FlatExporter
{
    [TransactionAttribute(TransactionMode.Manual)]
    [RegenerationAttribute(RegenerationOption.Manual)]
    public class ExternalCommands : IExternalCommand
    {
        ProcessManager pm = new ProcessManager();

        public Result Execute(ExternalCommandData commandData, ref string message, Autodesk.Revit.DB.ElementSet elements)
        {
            UIApplication appRevit = commandData.Application;
            Document doc = appRevit.ActiveUIDocument.Document;
            var selection = appRevit.ActiveUIDocument.Selection.GetElementIds();
            //  Assembly.LoadFrom()
            var categoryParameters = BaseApartments.GetBaseCategoryParameters();
            foreach (var elId1 in selection)
            {
                Element el1 = doc.GetElement(elId1);
                Group gr1 = el1 as Group;            //Группа первого уровня(содержит в себе три группы: фасад, ар, стены)
                List<Element> scaningElements = pm.GetRevitElements(gr1, doc);
                if (scaningElements.Count == 0) continue;
                XYZ groupLocation = (gr1.Location as LocationPoint).Point;
                scaningElements = scaningElements.OrderBy(x => x.Category.Name).ToList();
                Apartment apartment = new Apartment();
                apartment.Elements = new List<IElement>();
                foreach (var scanElement in scaningElements)
                {
                    AR_ApartmentBase.Model.Elements.Element el = null;
                    switch (scanElement.Category.Name)
                    {
                        case "Стены": el = pm.GetDbWall(scanElement as Wall, categoryParameters, doc, groupLocation);
                            break;
                        default: break;
                    }
                    if (el == null) continue;
                    apartment.Elements.Add(el);
                }

                foreach (var wall in apartment.Elements.Where(x=>x.CategoryElement.Equals("Стены")))
                {
                    var joinIdWalls = JoinGeometryUtils.GetJoinedElements(doc, doc.GetElement(new ElementId(wall.IdInRevit)));
                    List<IElement> joinWalls = new List<IElement>();
                    foreach (var idWall in joinIdWalls)
                    {
                        joinWalls.Add(apartment.Elements.First(x=>x.IdInRevit.Equals(idWall.IntegerValue)) as DBWall);
                    }
                    wall.Parameters.Add(new Parameter("MergeGeometry", joinWalls));
                }
            }
            return Result.Succeeded;
        }

      
    }
}

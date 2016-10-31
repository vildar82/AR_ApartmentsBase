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
   public class ProcessManager
    {

        public DBWall GetDbWall(Wall w, List<KeyValuePair<string, List<F_S_Parameters>>> categoryParameters, Document doc,XYZ groupLoc)
        {
            DBWall el = new DBWall { CategoryElement = w.Category.Name };
            Line lineWall = (w.Location as LocationCurve).Curve as Line;
            el.Direction = Helper.ConvertXyzToString((lineWall.Direction),true);
            el.FamilyName = w.WallType.FamilyName;
            el.FamilySymbolName = w.WallType.Name;
            el.LocationPoint = Helper.ConvertXyzToString((lineWall.Origin-groupLoc), false);
            el.Parameters = new List<Parameter>();
            el.IdInRevit = w.Id.IntegerValue;
            foreach (var vv in categoryParameters.First(x => x.Key.Equals(el.CategoryElement)).Value)
            {
                switch (vv.NAME_PARAMETER)
                {
                    case "Orientation":
                        el.Parameters.Add(new Parameter(vv.NAME_PARAMETER, Helper.ConvertXyzToString(w.Orientation, true)));
                        break;
                    case "Length":
                        el.Parameters.Add(new Parameter(vv.NAME_PARAMETER, Math.Round(lineWall.Length*304.8,1)));
                        break;
                    default:
                        {
                            Autodesk.Revit.DB.Parameter par = w.LookupParameter(vv.NAME_PARAMETER.Replace("1", " "));
                            if (par == null)
                                continue;
                            el.Parameters.Add(new Parameter(vv.NAME_PARAMETER, GetParameterValue(par, doc)));
                        }
                        break;
                }
            }
            return el;
        }
        public List<Element> GetRevitElements(Group group, Document doc)
       {
           List<Element> scaningElements = new List<Element>();

           foreach (var elId2 in group.GetMemberIds())      //Группа первого уровня(содержит в себе три группы: фасад, ар, стены)
           {
               Element el2 = doc.GetElement(elId2);
               Group gr2 = el2 as Group; //Группа второго уровня (группы фасад, стена, ар)
               if (gr2 == null) continue;
               foreach (var elId3 in gr2.GetMemberIds())
               {
                   Element el3 = doc.GetElement(elId3);
                   Group gr3 = el3 as Group; //Группа третьего уровня (в группе ар находятся группы СУ)
                   if (gr3 != null)
                   {
                       foreach (var elId4 in gr3.GetMemberIds())
                       {
                           Element el4 = doc.GetElement(elId4);
                           if ((el4.Category == null || !Helper.Categories.Any(x => x.Equals(el4.Category.Name))))
                               continue;
                           scaningElements.Add(el4);
                       }
                   }
                   else
                   {
                       if ((el3.Category == null || !Helper.Categories.Any(x => x.Equals(el3.Category.Name))))
                           continue;
                       scaningElements.Add(el3);
                   }
               }
           }
           return scaningElements;
       }

        string GetParameterValue(Autodesk.Revit.DB.Parameter para, Document document)
        {
            string defValue = string.Empty;
            switch (para.StorageType)
            {
                case StorageType.Double:
                    defValue = Math.Round(para.AsDouble()*304.8,1).ToString();
                    break;
                case StorageType.ElementId:
                    defValue = para.AsElementId().IntegerValue.ToString();
                    break;
                case StorageType.Integer:
                    defValue = para.AsInteger().ToString();
                    break;
                case StorageType.String:
                    defValue = para.AsString();
                    break;
                default:
                    defValue = "";
                    break;
            }

            return defValue;
        }
    }


}

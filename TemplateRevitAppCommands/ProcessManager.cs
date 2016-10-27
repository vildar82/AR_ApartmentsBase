using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Revit_FlatExporter
{
   public class ProcessManager
    {
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
                           //if (!(el4 is Wall)) continue;
                           //Wall w = el4 as Wall;
                           //ElementInfo el = new ElementInfo();
                           //el.CategoryElement = w.Category.Name;
                           //el.Direction = Helper.ConvertXyzToString(((w.Location as LocationCurve).Curve as Line).Direction);
                           //apartment.Elements.Add(el);
                           scaningElements.Add(el4);
                       }
                   }
                   else
                   {
                       if ((el3.Category == null || !Helper.Categories.Any(x => x.Equals(el3.Category.Name))))
                           continue;
                       //  if (!(el3 is Wall)) continue;
                       scaningElements.Add(el3);
                   }
               }
           }
           return scaningElements;
       }
    }
}

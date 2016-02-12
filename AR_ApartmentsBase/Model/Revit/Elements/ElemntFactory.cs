using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;

namespace AR_ApartmentBase.Model.Revit.Elements
{
   public static class ElementFactory
   {
      public static Element CreateElement (string typeElement, BlockReference blRefElem, Module module, string blName)
      {
         Element res = null;
         switch (typeElement)
         {
            case "Стены":
               res = new Wall(blRefElem, module, blName);
               res.TypeElement = "Стены";
               break;            
            default:
               break;
         }
         return res;         
      }
   }
}

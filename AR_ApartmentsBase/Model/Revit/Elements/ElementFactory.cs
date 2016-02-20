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
      public const string CategoryWallName = "стены";
      public const string CategoryDoorName = "двери";

      /// <summary>
      /// Создание элемента из автокадовского блока блока
      /// </summary>      
      public static Element CreateElementDWG (BlockReference blRefElem, Module module, string blName, List<Parameter> parameters, string category)
      {
         Element elem = null;

         if (category.Equals(Options.Instance.CategoryWallName, StringComparison.OrdinalIgnoreCase))
         {
            elem = new WallElement(blRefElem, module, blName, parameters, category);
         }
         else if (category.Equals(Options.Instance.CategoryDoorName, StringComparison.OrdinalIgnoreCase))
         {
            elem = new DoorElement(blRefElem, module, blName, parameters, category);
            ((DoorElement)elem).DefineOrientation(blRefElem);            
         }
         else
         {
            elem = new Element(blRefElem, module, blName, parameters, category);
         }        
          
         return elem;
      }

      /// <summary>
      /// Создание элемента из базы
      /// </summary>      
      public static Element CreateElementDB(Module module, string familyName, string fsn, List<Parameter> parameters, string category)
      {
         Element elem = null;

         if (category.Equals(Options.Instance.CategoryWallName, StringComparison.OrdinalIgnoreCase))
         {
            elem = new WallElement(module, familyName, fsn, parameters, category);
         }
         else if (category.Equals(Options.Instance.CategoryDoorName, StringComparison.OrdinalIgnoreCase))
         {
            elem = new DoorElement(module, familyName, fsn, parameters, category);            
         }
         else
         {
            elem = new Element(module, familyName, fsn, parameters, category);
         }         
         return elem;
      }
   }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AR_ApartmentBase.Model.DB.EntityModel;
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
      public static Element CreateElementDB(Module module, F_nn_Elements_Modules emEnt)
      {
         Element elem = null;
         string category = emEnt.F_S_Elements.F_S_Categories.NAME_RUS_CATEGORY;

         if (category.Equals(Options.Instance.CategoryWallName, StringComparison.OrdinalIgnoreCase))
         {
            elem = new WallElement(module, emEnt);
         }
         else if (category.Equals(Options.Instance.CategoryDoorName, StringComparison.OrdinalIgnoreCase))
         {
            elem = new DoorElement(module, emEnt);            
         }
         else
         {
            elem = new Element(module, emEnt);
         }         
         return elem;
      }
   }
}

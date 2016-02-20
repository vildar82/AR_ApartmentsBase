using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;

namespace AR_ApartmentBase.Model.Revit.Elements
{
   public class WallElement : Element
   {
      /// <summary>
      /// Чистая граница елемента, без атрибутов и т.п.
      /// </summary>
      public Extents3d ExtentsClean { get; set; }

      public WallElement(BlockReference blRefElem, Module module, string blName, List<Parameter> parameters, string category) 
            : base(blRefElem, module, blName, parameters, category)
      {
         ExtentsClean = blRefElem.GeometricExtentsСlean();
      }

      public WallElement(Module module, string familyName, string fsn, List<Parameter> parameters, string category)
         : base(module, familyName, fsn, parameters, category)
      {

      }
   }
}

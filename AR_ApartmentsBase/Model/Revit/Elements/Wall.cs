using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;

namespace AR_ApartmentBase.Model.Revit.Elements
{
   [Serializable]
   public class Wall : Element
   {
      public Wall() { }

      public Wall (BlockReference blRefElem, Module module, string blName) : base(blRefElem, module, blName)
      {

      }
   }
}

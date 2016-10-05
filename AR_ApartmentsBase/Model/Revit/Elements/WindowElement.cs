using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AR_ApartmentBase.Model.DB.EntityModel;
using Autodesk.AutoCAD.DatabaseServices;

namespace AR_ApartmentBase.Model.Revit.Elements
{
    public class WindowElement : Element
    {
        public WindowElement (BlockReference blRefElem, Module module, string blName, List<Parameter> parameters, string category)
              : base(blRefElem, module, blName, parameters, category)
        {
            DefineOrientation(blRefElem);
        }

        public WindowElement (Module module, F_nn_Elements_Modules emEnt)
           : base(module, emEnt)
        {
        }

        public override bool Equals (Element other)
        {
            WindowElement win2 = other as WindowElement;
            if (win2 == null) return false;
            if (ReferenceEquals(this, win2)) return true;

            var param1 = Parameters;
            var param2 = win2.Parameters;

            return FamilyName.Equals(win2.FamilyName, StringComparison.OrdinalIgnoreCase) &&
                FamilySymbolName.Equals(win2.FamilySymbolName, StringComparison.OrdinalIgnoreCase) &&
                Direction.Equals(win2.Direction) &&
                LocationPoint.Equals(win2.LocationPoint) &&
                Parameter.Equal(param1, param2);
        }
    }
}

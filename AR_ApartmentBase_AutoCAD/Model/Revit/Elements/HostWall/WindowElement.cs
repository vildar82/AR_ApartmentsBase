using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AR_ApartmentBase.Model;
using AR_ApartmentBase.Model.DB.EntityModel;
using Autodesk.AutoCAD.DatabaseServices;

namespace AR_ApartmentBase_AutoCAD
{
    public class WindowElement : WallHostBase
    {
        public WindowElement (BlockReference blRefElem, Module module, string blName, List<Parameter> parameters, string category)
              : base(blRefElem, module, blName, parameters, category)
        {
        }        
    }
}

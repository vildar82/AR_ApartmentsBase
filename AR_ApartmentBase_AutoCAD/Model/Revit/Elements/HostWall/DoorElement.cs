using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcadLib.Errors;
using AR_ApartmentBase.Model.DB.EntityModel;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using AcadLib.Geometry;

namespace AR_ApartmentBase.AutoCAD
{
    public class DoorElement : WallHostBase
    {        
        //public int count = -1;

        public DoorElement(BlockReference blRefElem, ModuleAC module, string blName, List<ParameterAC> parameters, string category)
              : base(blRefElem, module, blName, parameters, category)
        {
        }        
    }
}

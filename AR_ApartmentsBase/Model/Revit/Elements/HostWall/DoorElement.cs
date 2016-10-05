using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcadLib.Errors;
using AR_ApartmentBase.Model.DB.DbServices;
using AR_ApartmentBase.Model.DB.EntityModel;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using AcadLib.Geometry;

namespace AR_ApartmentBase.Model.Revit.Elements
{
    public class DoorElement : WallHostBase
    {        
        //public int count = -1;

        public DoorElement(BlockReference blRefElem, Module module, string blName, List<Parameter> parameters, string category)
              : base(blRefElem, module, blName, parameters, category)
        {
        }

        public DoorElement(Module module, F_nn_Elements_Modules emEnt)
           : base(module, emEnt)
        {
        }
    }
}

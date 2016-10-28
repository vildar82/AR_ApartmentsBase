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
using AR_ApartmentBase.Model;

namespace AR_ApartmentBase_AutoCAD
{
    public class DoorElement : WallHostBase, IElement
    {        
        //public int count = -1;

        public DoorElement(BlockReference blRefElem, ApartmentAC apart, string blName, List<Parameter> parameters, string category)
              : base(blRefElem, apart, blName, parameters, category)
        {
        }        
    }
}

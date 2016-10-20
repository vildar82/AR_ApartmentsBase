using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;

namespace AR_ApartmentBase.AutoCAD.Utils
{
    [Serializable]
    public class SaveDynProp
    {        
        public long Handle { get; set; }
        public string FloorValue { get; set; }        

        public SaveDynProp() { }

        public SaveDynProp(long h, string v)
        {
            Handle = h;
            FloorValue = v;            
        }        
    }
}

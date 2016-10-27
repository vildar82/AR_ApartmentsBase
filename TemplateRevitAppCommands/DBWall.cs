using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AR_ApartmentBase.Model.Elements;

namespace Revit_FlatExporter
{
    public class DBWall : ElementInfo,IWall
    {
        public List<AR_ApartmentBase.Model.Elements.Element> Doors { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AR_ApartmentBase.Model.Elements;
using Autodesk.Revit.DB;

namespace Revit_FlatExporter
{
  public  class WallHost:ElementInfo,IElement
    {
      public List<IElement> HostWall { get; set; }

      public int IdIElementInModule { get; set; }
      public FamilySymbol FamilyType { get; set; }

      public double Height { get; set; }
      public bool IsRight { get; set; }
        public void SearchHostWallDwg(List<IElement> elements)
        {
           
        }
        public void SearchHostWallDB()
        {
          
        }
    }
}

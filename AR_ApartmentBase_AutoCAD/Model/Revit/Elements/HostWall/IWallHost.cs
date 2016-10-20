using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AR_ApartmentBase.Model.DB.EntityModel;

namespace AR_ApartmentBase.AutoCAD
{
    /// <summary>
    /// Элемент принадлежащий стене (окно, дверь)
    /// </summary>
    public interface IWallHost : IRevitBlock
    {
        List<WallElement> HostWall { get; set; }
        void SearchHostWallDwg (List<ElementAC> elements);        
    }
}

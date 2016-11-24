using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AR_ApartmentBase_AutoCAD
{
    public interface IWallHost
    {
        void SearchHostWallDwg(List<IElement> elements);
    }
}

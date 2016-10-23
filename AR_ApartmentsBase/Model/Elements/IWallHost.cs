using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AR_ApartmentBase.Model.DB.EntityModel;

namespace AR_ApartmentBase.Model.Elements
{
    /// <summary>
    /// Элемент принадлежащий стене (окно, дверь)
    /// </summary>
    public interface IWallHost : IElement
    {
        /// <summary>
        /// Стены, которым принадлежит элемент
        /// </summary>
        List<IElement> HostWall { get; set; }
        void SearchHostWallDwg (List<IElement> elements);
        void SearchHostWallDB ();
    }
}

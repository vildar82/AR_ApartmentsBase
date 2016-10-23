using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AR_ApartmentBase.Model;
using AR_ApartmentBase.Model.DB.EntityModel;
using AR_ApartmentBase.Model.Elements;
using Autodesk.AutoCAD.DatabaseServices;

namespace AR_ApartmentBase_AutoCAD
{
    public class WallElement : ElementAC, IWall
    {
        /// <summary>
        /// Контур стены
        /// </summary>
        public Polyline Contour { get; set; }
        public Extents3d ExtentsClean { get; set; }

        public WallElement(BlockReference blRefElem, Module module, string blName, List<Parameter> parameters, string category)
              : base(blRefElem, module, blName, parameters, category)
        {
            ExtentsClean = blRefElem.GeometricExtentsСlean();
            Contour = getWallContour(blRefElem);
            if (Contour != null)
            {
                Contour = (Polyline)Contour.Clone();
                //Contour.UpgradeOpen();
                Contour.TransformBy(blRefElem.BlockTransform);
                //Contour.DowngradeOpen();
            }
        }                

        private Polyline getWallContour(BlockReference blRefElem)
        {
            var btr = blRefElem.BlockTableRecord.GetObject(OpenMode.ForRead) as BlockTableRecord;
            Polyline resVal = null;
            double maxArea = 0;
            foreach (var idEnt in btr)
            {
                var pl = idEnt.GetObject(OpenMode.ForRead, false, true) as Polyline;
                if (pl == null || !pl.Visible || pl.Area ==0) continue;
                if (pl.Area>maxArea)
                {
                    maxArea = pl.Area;
                    resVal = pl;
                }
            }
            return resVal;
        }
    }
}

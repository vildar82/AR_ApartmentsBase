using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AR_ApartmentBase.Model.Revit;
using AR_ApartmentBase.Model.Revit.Elements;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;

namespace AR_ApartmentBase.Model.AcadServices
{
    public static class ConvexHullHelper
    {
        public static List<Point2d> GetContour(List<Point2d> pts)
        {
            List<Point2d> resVal = new List<Point2d>();
            List<Coordinate> coordinates = new List<Coordinate>();
            foreach (var P in pts)
            {
                coordinates.Add(new Coordinate(P.X, P.Y));
            }
            var multiPoint = Geometry.DefaultFactory.CreateMultiPoint(coordinates.ToArray());
            multiPoint.Normalize();
            var hullGeom = multiPoint.ConvexHull();

            foreach (var c in hullGeom.Coordinates)
            {
                resVal.Add(new Point2d(c.X, c.Y));
            }
            return resVal;
        }

        public static void CreateContours(List<Apartment> apartments)
        {
            Database db = HostApplicationServices.WorkingDatabase;
            using (var t = db.TransactionManager.StartTransaction())
            {
                var layerContourInfo = new AcadLib.Layers.LayerInfo("АР_Квартиры_Штриховка");
                layerContourInfo.Color = Color.FromColor(System.Drawing.Color.Aqua);                
                var layerContourId = AcadLib.Layers.LayerExt.GetLayerOrCreateNew(layerContourInfo);

                foreach (var apart in apartments)
                {
                    List<Point2d> pts = new List<Point2d>();
                    foreach (var module in apart.Modules)
                    {
                        var blRefModule = module.IdBlRef.GetObject(OpenMode.ForRead, false, true) as BlockReference;
                        foreach (var wall in module.Elements.OfType<WallElement>())
                        {
                            var extWall = wall.ExtentsClean;
                            extWall.TransformBy(blRefModule.BlockTransform);
                            pts.Add(extWall.MinPoint.Convert2d());
                            pts.Add(extWall.MaxPoint.Convert2d());
                        }
                    }
                    var contour = GetContour(pts);

                    Polyline pl = new Polyline();
                    pl.SetDatabaseDefaults();
                    pl.LayerId = layerContourId;
                    for (int i = 0; i < contour.Count; i++)
                    {
                        pl.AddVertexAt(i, contour[i], 0, 0, 0);
                    }
                    var btrApart = apart.IdBtr.GetObject(OpenMode.ForWrite) as BlockTableRecord;
                    btrApart.AppendEntity(pl);
                    t.AddNewlyCreatedDBObject(pl, true);

                    Hatch h = new Hatch();
                    h.SetDatabaseDefaults();
                    h.LayerId = layerContourId;
                    h.SetHatchPattern(HatchPatternType.PreDefined, "Solid");

                    btrApart.AppendEntity(h);
                    t.AddNewlyCreatedDBObject(h, true);

                    h.Associative = true;
                    var idsH = new ObjectIdCollection(new[] { pl.Id });
                    h.AppendLoop(HatchLoopTypes.Default, idsH);

                    var idsBlRefApart = btrApart.GetBlockReferenceIds(true, false);
                    foreach (ObjectId idBlRefApart in idsBlRefApart)
                    {
                        var blRefApart = idBlRefApart.GetObject(OpenMode.ForWrite, false, true) as BlockReference;
                        blRefApart.RecordGraphicsModified(true);
                    }
                }
                t.Commit();
            }
        }
    }
}

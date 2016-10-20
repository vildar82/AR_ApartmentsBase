using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcadLib.Comparers;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using AcadLib;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using AcadLib.Errors;

namespace AR_ApartmentBase.AutoCAD.AcadServices
{
    public static class ContourHelper
    {
        public static void CreateContours2(List<ApartmentAC> apartments)
        {
            Database db = HostApplicationServices.WorkingDatabase;
            using (var t = db.TransactionManager.StartTransaction())
            {
                db.RegApp(AR_ApartmentBase.AutoCAD.Commands.RegAppApartBase);

                ProgressMeter progress = new ProgressMeter();
                progress.SetLimit(apartments.Count);
                progress.Start("Построение контуров квартир...");

                foreach (var apart in apartments)
                {
                    progress.MeterProgress();
                    try
                    {
                        List<Polyline> colPl = new List<Polyline>();
                        Point3dCollection pts = new Point3dCollection();
                        foreach (var module in apart.ModulesAC)
                        {
                            var blRefModule = module.IdBlRef.GetObject(OpenMode.ForRead, false, true) as BlockReference;
                            foreach (var wall in module.Elements.OfType<WallElement>())
                            {
                                var pl = wall.Contour?.Clone() as Polyline;
                                if (pl == null)
                                {
                                    pl = wall.ExtentsClean.GetPolyline();
                                }                                    
                                pl.TransformBy(blRefModule.BlockTransform);
                                colPl.Add(pl);
                            }
                        }

                        var plContour = colPl.GetExteriorContour();

                        var btrApart = apart.IdBtr.GetObject(OpenMode.ForWrite) as BlockTableRecord;
                        var blRefApart = apart.IdBlRef.GetObject(OpenMode.ForRead, false, true) as BlockReference;

                        var layerApartInfo = new AcadLib.Layers.LayerInfo(blRefApart.Layer);
                        AcadLib.Layers.LayerExt.CheckLayerState(layerApartInfo);

                        plContour.SetXData(AR_ApartmentBase.AutoCAD.Commands.RegAppApartBase, 1);
                        plContour.SetDatabaseDefaults();
                        plContour.LayerId = blRefApart.LayerId;

                        ClearOldContour(btrApart);

                        btrApart.AppendEntity(plContour);
                        t.AddNewlyCreatedDBObject(plContour, true);

                        Hatch h = new Hatch();
                        h.SetXData(AR_ApartmentBase.AutoCAD.Commands.RegAppApartBase, 1);
                        h.SetDatabaseDefaults();
                        h.LayerId = blRefApart.LayerId;
                        h.SetHatchPattern(HatchPatternType.PreDefined, "Solid");

                        btrApart.AppendEntity(h);
                        t.AddNewlyCreatedDBObject(h, true);

                        h.Associative = true;
                        var idsH = new ObjectIdCollection(new[] { plContour.Id });
                        h.AppendLoop(HatchLoopTypes.Default, idsH);
                        h.EvaluateHatch(true);

                        var btrDrawOrder = btrApart.DrawOrderTableId.GetObject(OpenMode.ForWrite) as DrawOrderTable;
                        btrDrawOrder.MoveToBottom(new ObjectIdCollection(new[] { h.Id }));

                        btrApart.SetBlRefsRecordGraphicsModified();
                    }
                    catch (System.Exception ex)
                    {
                        Inspector.AddError($"Ошибка при построении контура или штриховки в квартире {apart.Name} - {ex.Message}",
                            apart.IdBlRef, System.Drawing.SystemIcons.Error);
                    }                  
                }
                progress.Stop();
                t.Commit();
            }
        }
        
        //[Obsolete("Старый - используй CreateContours2")]
        //public static void CreateContours(List<ApartmentAC> apartments)
        //{
        //    Database db = HostApplicationServices.WorkingDatabase;
        //    using (var t = db.TransactionManager.StartTransaction())
        //    {
        //        db.RegApp(Commands.RegAppApartBase);

        //        foreach (var apart in apartments)
        //        {
        //            List<Point2d> pts = new List<Point2d>();
        //            foreach (var module in apart.Modules)
        //            {
        //                var blRefModule = module.IdBlRef.GetObject(OpenMode.ForRead, false, true) as BlockReference;
        //                foreach (var wall in module.Elements.OfType<WallElement>())
        //                {
        //                    //var extWall = wall.ExtentsClean;
        //                    //extWall.TransformBy(blRefModule.BlockTransform);
        //                    //pts.Add(extWall.MinPoint.Convert2d());
        //                    //pts.Add(new Point2d (extWall.MinPoint.X, extWall.MaxPoint.Y));
        //                    //pts.Add(extWall.MaxPoint.Convert2d());
        //                    //pts.Add(new Point2d(extWall.MaxPoint.X, extWall.MinPoint.Y));
        //                }
        //            }
        //            Point2d centroid;
        //            var contour = GetConvexHull(pts, out centroid);

        //            var blRefApart = apart.IdBlRef.GetObject(OpenMode.ForRead, false, true) as BlockReference;
        //            var layerApartInfo = new AcadLib.Layers.LayerInfo(blRefApart.Layer);
        //            AcadLib.Layers.LayerExt.CheckLayerState(layerApartInfo);

        //            Polyline pl = new Polyline();
        //            pl.SetXData(Commands.RegAppApartBase, 1);
        //            pl.SetDatabaseDefaults();
        //            pl.LayerId = blRefApart.LayerId;
        //            for (int i = 0; i < contour.Count; i++)
        //            {
        //                pl.AddVertexAt(i, contour[i], 0, 0, 0);
        //            }
        //            RectanglePolyline(pl, centroid);
        //            var btrApart = apart.IdBtr.GetObject(OpenMode.ForWrite) as BlockTableRecord;

        //            ClearOldContour(btrApart);                    

        //            btrApart.AppendEntity(pl);
        //            t.AddNewlyCreatedDBObject(pl, true);                    

        //            Hatch h = new Hatch();
        //            h.SetXData(Commands.RegAppApartBase, 1);
        //            h.SetDatabaseDefaults();
        //            h.LayerId = blRefApart.LayerId;
        //            h.SetHatchPattern(HatchPatternType.PreDefined, "Solid");                    

        //            btrApart.AppendEntity(h);
        //            t.AddNewlyCreatedDBObject(h, true);                   

        //            h.Associative = true;
        //            var idsH = new ObjectIdCollection(new[] { pl.Id });
        //            h.AppendLoop(HatchLoopTypes.Default, idsH);
        //            h.EvaluateHatch(true);

        //            var btrDrawOrder = btrApart.DrawOrderTableId.GetObject(OpenMode.ForWrite) as DrawOrderTable;
        //            btrDrawOrder.MoveToBottom(new ObjectIdCollection(new[] { h.Id }));

        //            btrApart.SetBlRefsRecordGraphicsModified();
        //        }
        //        t.Commit();
        //    }
        //}

        //public static List<Point2d> GetConvexHull(List<Point2d> pts, out Point2d centroid)
        //{
        //    List<Point2d> resVal = new List<Point2d>();
        //    List<Coordinate> coordinates = new List<Coordinate>();
        //    foreach (var P in pts)
        //    {
        //        coordinates.Add(new Coordinate(P.X, P.Y));
        //    }
        //    var multiPoint = Geometry.DefaultFactory.CreateMultiPoint(coordinates.ToArray());
        //    multiPoint.Normalize();
        //    centroid = new Point2d(multiPoint.Centroid.X, multiPoint.Centroid.Y);
        //    var hullGeom = multiPoint.ConvexHull();
        //    foreach (var c in hullGeom.Coordinates)
        //    {
        //        resVal.Add(new Point2d(c.X, c.Y));
        //    }
        //    return resVal;
        //}        

        public static void RectanglePolyline(Polyline pl, Point2d centroid)
        {            
            // Замена диагональных сегменов на прямоугольные            
            Plane plane = new Plane();
            var numVertex = pl.NumberOfVertices;
            int curIindex = 0;
            for (int i = 0; i < numVertex-1; i++)
            {
                var bulge = pl.GetBulgeAt(curIindex);
                var segment = pl.GetLineSegmentAt(curIindex);
                var angleRad = segment.Direction.AngleOnPlane(plane);
                var angleDeg = angleRad.ToDegrees();
                if (!angleDeg.IsOrthoAngle())
                {
                    // Поделить дуговой сегмент на два прямоугольных
                    var pt1 = pl.GetPoint2dAt(curIindex);
                    var pt2 = pl.GetPoint2dAt(curIindex + 1);
                    var ptC1 = new Point2d(pt1.X, pt2.Y);
                    var ptC2 = new Point2d(pt2.X, pt1.Y);
                    var dist1 = (centroid - ptC1).Length;
                    var dist2 = (centroid - ptC2).Length;
                    Point2d ptC = (dist1 <= dist2) ? ptC1 : ptC2;                    
                    pl.AddVertexAt(curIindex+1, ptC, bulge, 0, 0);
                    curIindex++;
                }
                curIindex++;
            }
        }       

        public static void ClearOldContour(BlockTableRecord btr)
        {
            foreach (var item in btr)
            {
                var ent = item.GetObject(OpenMode.ForRead, false, true) as Entity;
                var xdValue = ent.GetXData(Commands.RegAppApartBase);
                if (xdValue == 1)
                {
                    ent.UpgradeOpen();
                    ent.Erase();
                }
            }
        }

        public static void ClearOldContourAll(List<ApartmentAC> apartments)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            using (var t = db.TransactionManager.StartTransaction())
            {
                foreach (var apart in apartments)
                {
                    var btrApart = apart.IdBtr.GetObject(OpenMode.ForRead) as BlockTableRecord;
                    ClearOldContour(btrApart);
                    btrApart.SetBlRefsRecordGraphicsModified();
                }

                t.Commit();
            }
        }        
    }
}
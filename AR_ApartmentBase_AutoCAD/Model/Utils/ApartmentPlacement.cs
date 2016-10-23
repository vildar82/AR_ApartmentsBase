using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using AcadLib.Errors;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace AR_ApartmentBase_AutoCAD.Utils
{
    public static class ApartmentPlacement
    {
        static double placeWidth = 17000;
        static double placeHeight = 21000;

        public static void Placement()
        {
            Document curDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            // Выбор папки с подложками
            var folderFlats = getFolderFlats(Path.GetDirectoryName(curDoc.Name));
            if (!Directory.Exists(folderFlats))
            {
                throw new System.Exception($"Выбранной папки не существует - {folderFlats}.");
            }
            // Список файлов dwg квартир
            var fileFlats = Directory.GetFiles(folderFlats, "*.dwg", SearchOption.TopDirectoryOnly);
            if (fileFlats.Length == 0)
            {
                throw new System.Exception($"Не найдены файлы dwg в папке {folderFlats}.");
            }

            // Создание нового файла
            var docFlats = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.Add(null);
            Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument = docFlats;
            using (docFlats.LockDocument())
            {
                // Копирование квартир из файлов подложек
                copyFlats(fileFlats, docFlats);
                // Расстановка блоков
                placementAparts(docFlats.Database);

                docFlats.Editor.ZoomExtents();
            }
        }       

        private static void copyFlats(string[] fileFlats, Document docFlats)
        {
            using (var progress = new ProgressMeter())
            {
                progress.SetLimit(fileFlats.Length);
                progress.Start("Копирование квартир из файлов подложек...");

                foreach (var fileFlat in fileFlats)
                {
                    progress.MeterProgress();
                    // Копирование блока квартиры из файла подложки
                    try
                    {
                        copyFlat(fileFlat, docFlats.Database.BlockTableId);
                    }
                    catch (System.Exception ex)
                    {
                        Inspector.AddError($"Ошибка копирования квартиры из файла {fileFlat} - {ex.ToString()}.",
                            System.Drawing.SystemIcons.Error);
                    }
                }
                progress.Stop();
            }
        }

        private static void copyFlat(string fileFlat, ObjectId idBt)
        {
            using (var dbFlat = new Database(false, true))
            {
                dbFlat.ReadDwgFile(fileFlat, FileOpenMode.OpenForReadAndAllShare, false, "");
                dbFlat.CloseInput(true);
                using (var bt = dbFlat.BlockTableId.Open( OpenMode.ForRead) as BlockTable)
                {
                    foreach (var idBtr in bt)
                    {
                        using (var btr = idBtr.Open( OpenMode.ForRead) as BlockTableRecord)
                        {
                            if (ApartmentAC.IsBlockNameApartment(btr.Name))
                            {
                                using (var map = new IdMapping())
                                {
                                    ObjectIdCollection ids = new ObjectIdCollection(new[] { idBtr });
                                    dbFlat.WblockCloneObjects(ids, idBt, map, DuplicateRecordCloning.Ignore, false);
                                }
                                return;
                            }
                        }
                    }
                }
            }
        }

        private static string getFolderFlats(string startFolder)
        {
            FolderBrowserDialog folderDlg = new FolderBrowserDialog();
            folderDlg.Description = "Выбор папки с подложками квартир для сбора в один файл всех блоков из них.";
            folderDlg.SelectedPath = startFolder;
            if (folderDlg.ShowDialog() != DialogResult.OK)
            {
                throw new System.Exception(AcadLib.General.CanceledByUser);
            }
            return folderDlg.SelectedPath;
        }

        private static void placementAparts(Database db)
        {
            using (var t = db.TransactionManager.StartTransaction())
            {
                ObjectId idTextStylePik = db.GetTextStylePIK();

                var bt = db.BlockTableId.GetObject(OpenMode.ForRead) as BlockTable;
                var ms = bt[BlockTableRecord.ModelSpace].GetObject(OpenMode.ForWrite) as BlockTableRecord;
                int countAparts;
                var btrApartGroups = getGroupedAparts(bt, out countAparts);
                Point3d pt = Point3d.Origin;
                Point3d ptCenterPlace;                

                using (var progress = new ProgressMeter())
                {
                    progress.SetLimit(countAparts);
                    progress.Start("Расстановка квартир...");

                    foreach (var btrApartGroup in btrApartGroups)
                    {
                        progress.MeterProgress();                     

                        foreach (var idBtrApart in btrApartGroup)
                        {
                            var curPlaceWidth = placeWidth;                            

                            var blRefApart = new BlockReference(pt, idBtrApart);
                            blRefApart.SetDatabaseDefaults(db);
                            var extApart = blRefApart.GeometricExtents;
                            var lenApart = extApart.MaxPoint.X - extApart.MinPoint.X;
                            if (lenApart > placeWidth)
                            {
                                curPlaceWidth = lenApart + 1000;
                            }

                            ptCenterPlace = new Point3d(pt.X + curPlaceWidth*0.5, pt.Y - placeHeight*0.5, 0);

                            
                            var ptBlCenter = extApart.Center();
                            // Перемещение блока в центр прямоугольной области
                            Matrix3d displace = Matrix3d.Displacement(ptCenterPlace - ptBlCenter);
                            blRefApart.TransformBy(displace);
                            ms.AppendEntity(blRefApart);
                            t.AddNewlyCreatedDBObject(blRefApart, true);

                            // Подпись квартиры
                            DBText text = new DBText();                            
                            text.SetDatabaseDefaults();
                            text.TextStyleId = idTextStylePik;
                            text.Height = 900;
                            text.TextString = getApartName(blRefApart.Name);
                            text.Position = new Point3d (pt.X+300, pt.Y+300,0);
                            ms.AppendEntity(text);
                            t.AddNewlyCreatedDBObject(text, true);

                            // Прямоугольник расположения квартиры
                            Polyline pl = new Polyline(4);
                            pl.AddVertexAt(0, pt.Convert2d(), 0, 0, 0);
                            pl.AddVertexAt(1, new Point2d (pt.X+ curPlaceWidth, pt.Y), 0, 0, 0);
                            pl.AddVertexAt(2, new Point2d(pt.X + curPlaceWidth, pt.Y-placeHeight), 0, 0, 0);
                            pl.AddVertexAt(3, new Point2d(pt.X, pt.Y - placeHeight), 0, 0, 0);
                            pl.Closed = true;
                            pl.SetDatabaseDefaults();
                            ms.AppendEntity(pl);
                            t.AddNewlyCreatedDBObject(pl, true);                                

                            pt = new Point3d(pt.X + curPlaceWidth, pt.Y, 0);
                        }
                        pt = new Point3d(0, pt.Y - placeHeight - 8000, 0);
                    }
                    progress.Stop();
                }
                t.Commit();
            }
        }

        private static string getApartName(string name)
        {
            var splits = name.Split(new[] { '_' },4);
            if (splits.Length>3)
            {
                return splits[3];
            }
            return name;
        }

        private static IEnumerable<IGrouping<int, ObjectId>> getGroupedAparts(BlockTable bt, out int count)
        {
            count = 0;
            List<Tuple<int, ObjectId>> btrAparts = new List<Tuple<int, ObjectId>>();
            foreach (var idBtr in bt)
            {
                var btr = idBtr.GetObject(OpenMode.ForRead) as BlockTableRecord;
                if (ApartmentAC.IsBlockNameApartment(btr.Name))
                {
                    int group = getGroup(btr.Name);
                    btrAparts.Add(new Tuple<int, ObjectId>(group, btr.Id));
                    count++;
                }
            }
            // группировка 
            var groups = btrAparts.GroupBy(g => g.Item1, s=>s.Item2);
            return groups;
        }

        private static int getGroup(string name)
        {
            string apartType = name.Split('_').Reverse().Skip(1).FirstOrDefault();
            if (string.IsNullOrEmpty(apartType)) return 0;
            string groupNum = Regex.Match(apartType, @"^\d+").Value;
            int res;
            int.TryParse(groupNum, out res);
            return res;
        }
    }
}

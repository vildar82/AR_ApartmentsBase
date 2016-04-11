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
using Autodesk.AutoCAD.Geometry;

namespace AR_ApartmentBase.Model.Utils
{
    public static class ApartmentPlacement
    {
        public static void Placement()
        {
            Document curDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            // Выбор папки с подложками
            var folderFlats = getFolderFlats(Path.GetDirectoryName(curDoc.Name));
            if (!Directory.Exists(folderFlats))
            {
                throw new Exception($"Выбранной папки не существует - {folderFlats}.");
            }
            // Список файлов dwg квартир
            var fileFlats = Directory.GetFiles(folderFlats, "*.dwg", SearchOption.TopDirectoryOnly);
            if (fileFlats.Length == 0)
            {
                throw new Exception($"Не найдены файлы dwg в папке {folderFlats}.");
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
            }
        }       

        private static void copyFlats(string[] fileFlats, Document docFlats)
        {
            foreach (var fileFlat in fileFlats)
            {
                // Копирование блока квартиры из файла подложки
                try
                {
                    copyFlat(fileFlat, docFlats.Database.BlockTableId);
                }
                catch (Exception ex)
                {
                    Inspector.AddError($"Ошибка копирования квартиры из файла {fileFlat} - {ex.ToString()}.",
                        System.Drawing.SystemIcons.Error);
                }
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
                            if (Revit.Apartment.IsBlockNameApartment(btr.Name))
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
                throw new Exception(AcadLib.General.CanceledByUser);
            }
            return folderDlg.SelectedPath;
        }

        private static void placementAparts(Database db)
        {
            using (var t = db.TransactionManager.StartTransaction())
            {
                var bt = db.BlockTableId.GetObject(OpenMode.ForRead) as BlockTable;
                var ms = bt[BlockTableRecord.ModelSpace].GetObject(OpenMode.ForWrite) as BlockTableRecord;
                var btrApartGroups = getGroupedAparts(bt);
                Point3d pt = Point3d.Origin;
                foreach (var btrApartGroup in btrApartGroups)
                {
                    foreach (var idBtrApart in btrApartGroup)
                    {
                        var blRefApart = new BlockReference(pt, idBtrApart);
                        blRefApart.SetDatabaseDefaults(db);

                        ms.AppendEntity(blRefApart);
                        t.AddNewlyCreatedDBObject(blRefApart, true);

                        pt = new Point3d(pt.X+17000, pt.Y, 0);
                    }
                    pt = new Point3d(0, pt.Y - 21000, 0);
                }
                t.Commit();
            }
        }

        private static IEnumerable<IGrouping<int, ObjectId>> getGroupedAparts(BlockTable bt)
        {
            List<Tuple<int, ObjectId>> btrAparts = new List<Tuple<int, ObjectId>>();
            foreach (var idBtr in bt)
            {
                var btr = idBtr.GetObject(OpenMode.ForRead) as BlockTableRecord;
                if (Revit.Apartment.IsBlockNameApartment(btr.Name))
                {
                    int group = getGroup(btr.Name);
                    btrAparts.Add(new Tuple<int, ObjectId>(group, btr.Id));
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

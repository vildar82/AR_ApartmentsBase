﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using AcadLib.Errors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AR_ApartmentBase.Model.AcadServices;
using System.Xml.Serialization;
using System.Drawing;
using AR_ApartmentBase.Model.Revit.Elements;
using Autodesk.AutoCAD.ApplicationServices;
using AR_ApartmentBase.Model.DB.DbServices;
using AR_ApartmentBase.Model.DB.EntityModel;
using System.Data.Entity;
using AcadLib.Blocks;

namespace AR_ApartmentBase.Model.Revit
{
    /// <summary>
    /// Квартира или МОП - блок в автокаде
    /// </summary>
    public class Apartment : IRevitBlock, IEquatable<Apartment>
    {        
        private static List<ObjectId> layersOff;
        public static List<KeyValuePair<string, List<F_S_Parameters>>> BaseCategoryParameters;

        /// <summary>
        /// Имя блока
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Параметр типа квартиры - Студия, 1комн, и т.д.
        /// </summary>
        public string TypeFlat { get; set; }
        public AttributeInfo TypeFlatAttr { get; set; }

        public ObjectId IdBlRef { get; set; }
        public string Layer { get; set; }

        public ObjectId IdBtr { get; set; }

        /// <summary>
        /// Модули в квартире.
        /// </summary>
        public List<Module> Modules { get; set; }

        /// <summary>
        /// Дата экспорта
        /// </summary>      
        public DateTime ExportDate { get; set; }

        /// <summary>
        /// Полный путь к файлу экспортированного блока
        /// </summary>      
        public string File { get; set; }

        /// <summary>
        /// Точка вставки бллока квартиры в Модели.
        /// </summary>      
        public Point3d Position { get; set; }

        /// <summary>
        /// Угол поворота блока квартиры.
        /// </summary>
        public double Rotation { get; set; }

        public List<Parameter> Parameters { get; set; }

        private bool _extentsAreDefined;
        private bool _extentsIsNull;
        private Extents3d _extentsInModel;
        public Extents3d ExtentsInModel
        {
            get
            {
                if (!_extentsAreDefined)
                {
                    _extentsAreDefined = true;
                    using (var blRef = IdBlRef.Open(OpenMode.ForRead, false, true) as BlockReference)
                    {
                        try
                        {
                            _extentsInModel = blRef.GeometricExtents;

                        }
                        catch
                        {
                            _extentsIsNull = true;
                        }
                    }
                }
                //if (_extentsIsNull)
                //{
                //    if (Error == null)
                //    {
                //        Error = new Error("Границы блока не определены. ");
                //    }
                //    else
                //    {
                //        if (!Error.Message.Contains("Границы блока не определены."))
                //        {
                //            Error.AdditionToMessage("Границы блока не определены. ");
                //        }
                //    }
                //}
                return _extentsInModel;
            }
        }

        public Matrix3d BlockTransform { get; set; }
        public Error Error { get; set; }

        public string Direction { get; set; }
        public string LocationPoint { get; set; }

        public EnumBaseStatus BaseStatus { get; set; }
        public int Revision { get; set; }

        /// <summary>
        /// F_R_Flats
        /// </summary>
        public object DBObject { get; set; }

        public string NodeName
        {
            get
            {
                return "Квартира " + Name + " " + ((Revision>0) ? "Ревизия-" + Revision.ToString(): "");
            }
        }

        public string Info
        {
            get
            {
                return "Инфо:\r\n" +
                    NodeName + "\r\n" +
                    "Точка вставки \t" + LocationPoint + "\r\n" +
                    "Поворот \t" + Rotation;                    
            }
        }

        /// <summary>
        /// Создание блока для экспорта из id
        /// Если id не блока, то Exception
        /// </summary>      
        public Apartment(BlockReference blRef, string blName)
        {
            Name = blName;
            IdBlRef = blRef.Id;
            Layer = blRef.Layer;
            IdBtr = blRef.BlockTableRecord;
            BlockTransform = blRef.BlockTransform;
            Position = blRef.Position;
            Rotation = blRef.Rotation;
            Direction = Element.GetDirection(Rotation);
            LocationPoint = TypeConverter.Point(Position);
            File = Path.Combine(Commands.DirExportApartments, Name + ".dwg");

            defineAttrs(blRef);

            // Определение модулуй в квартире
            Modules = Module.GetModules(this);
        }

        private void defineAttrs(BlockReference blRef)
        {
            var attrs = AttributeInfo.GetAttrRefs(blRef);
            // Поиск атрибута типа квартиры
            TypeFlatAttr = attrs.Find(a => a.Tag.Equals(Options.Instance.ApartmentTypeFlatParameter, StringComparison.OrdinalIgnoreCase));
            if (TypeFlatAttr != null)
            {
                TypeFlat = TypeFlatAttr.Text.Trim();
            }
        }

        /// <summary>
        /// Конструктор для создания квартиры из базы
        /// </summary>
        public Apartment(F_R_Flats flatEnt)
        {
            Name = flatEnt.WORKNAME;
            _extentsIsNull = true;
            _extentsAreDefined = true;
            Modules = new List<Module>();
            DBObject = flatEnt;
            Revision = flatEnt.REVISION;
            TypeFlat = flatEnt.TYPE_FLAT;
        }

        /// <summary>
        /// Экспорт квартир в XML
        /// </summary>
        public static void ExportToXML(string fileXml, List<Apartment> apartments)
        {
            try
            {
                //AparmentCollection apartCol = new AparmentCollection();
                //apartCol.Apartments = apartments;
                AcadLib.Files.SerializerXml ser = new AcadLib.Files.SerializerXml(fileXml);
                ser.SerializeList(apartments);
            }
            catch (System.Exception ex)
            {
                Inspector.AddError($"Ошибка при экспорте квартир в XML - {ex.Message}.", icon: SystemIcons.Error);
            }
        }

        /// <summary>
        /// Экспорт блоков квартир в отдельные файлы dwg квартир.
        /// </summary>      
        /// <returns>Количество экспортированных квартир.</returns>
        public static int ExportToFiles(List<Apartment> apartments)
        {
            int count = 0;
            DateTime now = DateTime.Now;

            // Бекап старых подложек            
            BackupOldApartmentsFile();            

            // Выключение слоев штриховки
            layersOff = LayerService.LayersOff(Options.Instance.LayersOffMatch);

            var apartsToFile = apartments.Where(a => !a.BaseStatus.HasFlag(EnumBaseStatus.NotInDwg));

            using (var progress = new ProgressMeter())
            {
                progress.SetLimit(apartsToFile.Count());
                progress.Start("Экспорт квартир в файлы...");

                foreach (var apart in apartsToFile)
                {
                    progress.MeterProgress();
                    try
                    {
                        apart.ExportToFile();
                        apart.ExportDate = now;
                        count++;
                    }
                    catch (System.Exception ex)
                    {
                        Inspector.AddError($"Ошибка при экспорте блока '{apart.Name}' - {ex.Message}", icon: System.Drawing.SystemIcons.Error);
                    }
                }
                progress.Stop();
            }

            // Восстановление слоев
            LayerService.LayersOn(layersOff);

            return count;
        }

        private static void BackupOldApartmentsFile()
        {
            try
            {
                var files = Directory.GetFiles(Commands.DirExportApartments, "*.dwg");
                if (files.Length > 0)
                {
                    // Куда архивировать
                    var dirBak = Path.Combine(Commands.DirExportApartments, @"Архив\Квартиры_" + DateTime.Now.ToString("dd-MM-yyyy"));
                    if (!Directory.Exists(dirBak))
                    {
                        Directory.CreateDirectory(dirBak);
                    }
                    foreach (var fileOldApart in files)
                    {
                        var dest = Path.Combine(dirBak, Path.GetFileName(fileOldApart));
                        System.IO.File.Move(fileOldApart, dest);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Logger.Log.Error(ex, $"BackupOldApartmentsFile - {Commands.DirExportApartments}");
            }
        }

        /// <summary>
        /// Экспорт блока в файл - файл в корне текущего чертежа с именем блока.
        /// Точка вставки блока - 0,0
        /// </summary>      
        public void ExportToFile()
        {
            using (var db = new Autodesk.AutoCAD.DatabaseServices.Database(true, true))
            {
                db.CloseInput(true);

                var ids = new ObjectIdCollection(new[] { IdBlRef });
                var idMS = SymbolUtilityServices.GetBlockModelSpaceId(db);

                using (IdMapping map = new IdMapping())
                {
                    db.WblockCloneObjects(ids, idMS, map, DuplicateRecordCloning.Replace, false);
                    // перенос блока в ноль            
                    var idBlRefMap = map[IdBlRef].Value;
                    if (!idBlRefMap.IsNull)
                    {
                        using (var t = db.TransactionManager.StartTransaction())
                        {
                            var blRef = idBlRefMap.GetObject(OpenMode.ForWrite, false, true) as BlockReference;
                            blRef.Position = Point3d.Origin;

                            // Изменение вида
                            if (blRef.Bounds.HasValue)
                            {
                                try
                                {
                                    zoomDb(db, blRef.Bounds.Value);
                                    // Перенос штриховки на задний план
                                    var btrApart = blRef.BlockTableRecord.GetObject(OpenMode.ForRead) as BlockTableRecord;
                                    var orders = btrApart.DrawOrderTableId.GetObject(OpenMode.ForWrite) as DrawOrderTable;
                                    var idsHatch = new ObjectIdCollection();
                                    foreach (var idEnt in btrApart)
                                        if (idEnt.ObjectClass == RXClass.GetClass(typeof(Hatch)))
                                            idsHatch.Add(idEnt);
                                    if (idsHatch.Count > 0)
                                        orders.MoveToBottom(idsHatch);                                    
                                    // Превью чертежа из блока квартиры
                                    db.ThumbnailBitmap = new Bitmap(btrApart.PreviewIcon, new Size(320, 270));                                    
                                }
                                catch { }
                            }
                            t.Commit();
                        }
                        db.SaveAs(File, DwgVersion.Current);
                    }
                }
                //Inspector.AddError($"Экспортирован блок {Name} в файл {File}", IdBlRef, icon: System.Drawing.SystemIcons.Information);
            }
        }

        private void zoomDb(Autodesk.AutoCAD.DatabaseServices.Database db, Extents3d ext)
        {
            using (var switcher = new AcadLib.WorkingDatabaseSwitcher(db))
            {
                //db.UpdateExt(true);
                using (ViewportTable vTab = db.ViewportTableId.GetObject(OpenMode.ForRead) as ViewportTable)
                {
                    ObjectId acVptId = vTab["*Active"];
                    using (ViewportTableRecord vpTabRec = acVptId.GetObject(OpenMode.ForWrite) as ViewportTableRecord)
                    {
                        double scrRatio = (vpTabRec.Width / vpTabRec.Height);
                        //Matrix3d matWCS2DCS = Matrix3d.PlaneToWorld(vpTabRec.ViewDirection);
                        //matWCS2DCS = Matrix3d.Displacement(vpTabRec.Target - Point3d.Origin) * matWCS2DCS;
                        //matWCS2DCS = Matrix3d.Rotation(-vpTabRec.ViewTwist,
                        //                                vpTabRec.ViewDirection,
                        //                                vpTabRec.Target)
                        //                                * matWCS2DCS;
                        //matWCS2DCS = matWCS2DCS.Inverse();
                        //Extents3d extents = new Extents3d(db.Extmin, db.Extmax);
                        //extents.TransformBy(matWCS2DCS);
                        //double width = (extents.MaxPoint.X - extents.MinPoint.X);
                        //double height = (extents.MaxPoint.Y - extents.MinPoint.Y);
                        //Point2d center = new Point2d((extents.MaxPoint.X + extents.MinPoint.X) * 0.5,
                        //                             (extents.MaxPoint.Y + extents.MinPoint.Y) * 0.5);

                        double width = (ext.MaxPoint.X - ext.MinPoint.X);
                        double height = (ext.MaxPoint.Y - ext.MinPoint.Y);
                        if (width > (height * scrRatio))
                            height = width / scrRatio;
                        vpTabRec.Height = ext.MaxPoint.Y-ext.MinPoint.Y;
                        vpTabRec.Width = height * scrRatio;
                        vpTabRec.CenterPoint = ext.Center().Convert2d();
                    }
                }
            }
        }

        /// <summary>
        /// Поиск квартир в чертеже.
        /// </summary>      
        public static List<Apartment> GetApartments(Autodesk.AutoCAD.DatabaseServices.Database db)
        {
            List<Apartment> apartments = new List<Apartment>();

            // Импользование базы для проверки категории элементов и их параметров
            using (var entities = BaseApartments.ConnectEntities())
            {
                entities.F_nn_Category_Parameters.Load();
                BaseCategoryParameters = entities.F_nn_Category_Parameters.Local.GroupBy(cp => cp.F_S_Categories).Select(p =>
                              new KeyValuePair<string, List<F_S_Parameters>>(p.Key.NAME_RUS_CATEGORY, p.Select(i => i.F_S_Parameters).ToList())).ToList();
            }            

            using (var t = db.TransactionManager.StartTransaction())
            {
                ProgressMeter progress = new ProgressMeter();                
                progress.Start("Считывание квартир с чертежа...");

                var ms = SymbolUtilityServices.GetBlockModelSpaceId(db).GetObject(OpenMode.ForRead) as BlockTableRecord;
                foreach (ObjectId idEnt in ms)
                {
                    progress.MeterProgress();                    

                    var blRefApart = idEnt.GetObject(OpenMode.ForRead, false, true) as BlockReference;
                    if (blRefApart != null)
                    {
                        string blName = blRefApart.GetEffectiveName();
                        if (IsBlockNameApartment(blName))
                        {
                            // Не добавлять одну и ту же квартиру в список
                            if (!apartments.Exists(a => a.Name.Equals(blName, StringComparison.OrdinalIgnoreCase)))
                            {
                                // Проверка масштабирования блока
                                if (!blRefApart.CheckNaturalBlockTransform())
                                {
                                    Inspector.AddError($"Блок квартиры масштабирован '{blName}' - {blRefApart.ScaleFactors.ToString()}.",
                                       blRefApart, icon: SystemIcons.Error);
                                }

                                try
                                {
                                    var apartment = new Apartment(blRefApart, blName);
                                    apartments.Add(apartment);
                                }
                                catch (System.Exception ex)
                                {
                                    Inspector.AddError($"Ошибка считывания блока квартиры '{blName}' - {ex.Message}.",
                                       blRefApart, icon: SystemIcons.Error);
                                }
                            }
                        }
                        else
                        {
                            Inspector.AddError($"Отфильтрован блок квартиры '{blName}', имя не соответствует " +
                               $"'{Options.Instance.BlockApartmentNameMatch}",
                               blRefApart, icon: System.Drawing.SystemIcons.Information);
                        }
                    }
                }
                progress.Stop();
                t.Commit();
            }
            apartments.Sort((a1, a2) => a1.Name.CompareTo(a2.Name));
            return apartments;
        }

        /// <summary>
        /// Поиск квартир в вфбранных блоках
        /// </summary>      
        public static List<Apartment> GetApartments(IEnumerable<ObjectId> idsBlRef)
        {            
            List<Apartment> apartments = new List<Apartment>();

            // Импользование базы для проверки категории элементов и их параметров
            using (var entities = BaseApartments.ConnectEntities())
            {
                entities.F_nn_Category_Parameters.Load();
                BaseCategoryParameters = entities.F_nn_Category_Parameters.Local.GroupBy(cp => cp.F_S_Categories).Select(p =>
                              new KeyValuePair<string, List<F_S_Parameters>>(p.Key.NAME_RUS_CATEGORY, p.Select(i => i.F_S_Parameters).ToList())).ToList();
            }

            Autodesk.AutoCAD.DatabaseServices.Database db = HostApplicationServices.WorkingDatabase;
            using (var t = db.TransactionManager.StartTransaction())
            {
                ProgressMeter progress = new ProgressMeter();                
                progress.Start("Считывание квартир с чертежа...");

                foreach (ObjectId idEnt in idsBlRef)
                {
                    progress.MeterProgress();

                    var blRefApart = idEnt.GetObject(OpenMode.ForRead, false, true) as BlockReference;
                    if (blRefApart != null)
                    {
                        string blName = blRefApart.GetEffectiveName();
                        if (IsBlockNameApartment(blName))
                        {
                            // Не добавлять одну и ту же квартиру в список
                            if (!apartments.Exists(a => a.Name.Equals(blName, StringComparison.OrdinalIgnoreCase)))
                            {
                                try
                                {
                                    var apartment = new Apartment(blRefApart, blName);
                                    apartments.Add(apartment);
                                }
                                catch (System.Exception ex)
                                {
                                    Inspector.AddError($"Ошибка считывания блока квартиры '{blName}' - {ex.Message}.",
                                       blRefApart, icon: SystemIcons.Error);
                                }
                            }
                        }
                        else
                        {
                            Inspector.AddError($"Отфильтрован блок квартиры '{blName}', имя не соответствует " +
                               $"'{Options.Instance.BlockApartmentNameMatch}",
                               blRefApart, icon: System.Drawing.SystemIcons.Information);
                        }
                    }
                }
                progress.Stop();
                t.Commit();
            }
            apartments.Sort((a1, a2) => a1.Name.CompareTo(a2.Name));
            return apartments;
        }

        /// <summary>
        /// Проверка имени блока квартиры
        /// </summary>      
        public static bool IsBlockNameApartment(string blName)
        {
            return Regex.IsMatch(blName, Options.Instance.BlockApartmentNameMatch, RegexOptions.IgnoreCase);
        }

        public bool Equals(Apartment other)
        {
            return this.Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase);
        }

        public ObjectId[] GetSubentPath()
        {
            return new[] { IdBlRef };
        }
    }
}
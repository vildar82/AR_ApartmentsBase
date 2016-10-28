using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using AcadLib.Errors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System.Xml.Serialization;
using System.Drawing;
using Autodesk.AutoCAD.ApplicationServices;
using AR_ApartmentBase.Model.DB.EntityModel;
using AcadLib.Blocks;
using AR_ApartmentBase.Model;
using AR_ApartmentBase_AutoCAD.AcadServices;
using AcadLib;

namespace AR_ApartmentBase_AutoCAD
{
    /// <summary>
    /// Квартира или МОП - блок в автокаде
    /// </summary>
    public class ApartmentAC : Apartment, IRevitBlock
    {        
        private static List<ObjectId> layersOff;        
       
        public AttributeInfo TypeFlatAttr { get; set; }
        public ObjectId IdBlRef { get; set; }
        public string Layer { get; set; }
        public ObjectId IdBtr { get; set; }        
        public List<Parameter> Parameters { get; set; }
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
        public Matrix3d BlockTransform { get; set; }        
        public string Direction { get; set; }
        public string LocationPoint { get; set; }
        public Error Error { get; set; }

        public EnumBaseStatus BaseStatus { get; set; }

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
                return _extentsInModel;
            }
        }

        public string NodeName
        {
            get
            {
                return "Квартира " + Name;
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
        public ApartmentAC(BlockReference blRef, string blName)
        {
            Name = blName;
            IdBlRef = blRef.Id;
            Layer = blRef.Layer;
            IdBtr = blRef.BlockTableRecord;
            BlockTransform = blRef.BlockTransform;
            Position = blRef.Position;
            Rotation = blRef.Rotation;
            Direction = ElementAC.GetDirection(Rotation);
            LocationPoint = TypeConverter.Point(Position);
            File = Path.Combine(Commands.DirExportApartments, Name + ".dwg");

            defineAttrs(blRef);

            // Определение модулуй в квартире
            Elements = ElementAC.GetElements(this);
        }

        private void defineAttrs(BlockReference blRef)
        {
            var attrs = AttributeInfo.GetAttrRefs(blRef);
            // Поиск атрибута типа квартиры
            TypeFlatAttr = attrs.Find(a => a.Tag.Equals(OptionsAC.Instance.ApartmentTypeFlatParameter, StringComparison.OrdinalIgnoreCase));
            if (TypeFlatAttr != null)
            {
                TypeFlat = TypeFlatAttr.Text.Trim();
            }
        }

        /// <summary>
        /// Конструктор для создания квартиры из базы
        /// </summary>
        public ApartmentAC (F_R_Flats flatEnt)
        {
            Name = flatEnt.WORKNAME;
            _extentsIsNull = true;
            _extentsAreDefined = true;
            Elements = new List<IElement>();
            DBObject = flatEnt;            
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
            layersOff = LayerService.LayersOff(OptionsAC.Instance.LayersOffMatch);

            //var apartsToFile = apartments.Where(a => !a.BaseStatus.HasFlag(EnumBaseStatus.NotInDwg));

            using (var progress = new ProgressMeter())
            {
                progress.SetLimit(apartments.Count());
                progress.Start("Экспорт квартир в файлы...");

                foreach (var apart in apartments)
                {
                    progress.MeterProgress();
                    try
                    {
                        var apartAC = (ApartmentAC)apart;
                        apartAC.ExportToFile();
                        apartAC.ExportDate = now;
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
                                    var apartment = new ApartmentAC(blRefApart, blName);
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
                               $"'{OptionsAC.Instance.BlockApartmentNameMatch}",
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
        public static List<ApartmentAC> GetApartments(IEnumerable<ObjectId> idsBlRef)
        {            
            List<ApartmentAC> apartments = new List<ApartmentAC>();            

            Database db = HostApplicationServices.WorkingDatabase;
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
                                    var apartment = new ApartmentAC(blRefApart, blName);
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
                               $"'{OptionsAC.Instance.BlockApartmentNameMatch}",
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
            return Regex.IsMatch(blName, OptionsAC.Instance.BlockApartmentNameMatch, RegexOptions.IgnoreCase);
        }        

        public ObjectId[] GetSubentPath()
        {
            return new[] { IdBlRef };
        }
    }
}
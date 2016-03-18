using System;
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

        public ObjectId IdBlRef { get; set; }

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
                if (_extentsIsNull)
                {
                    if (Error == null)
                    {
                        Error = new Error("Границы блока не определены. ");
                    }
                    else
                    {
                        if (!Error.Message.Contains("Границы блока не определены."))
                        {
                            Error.AdditionToMessage("Границы блока не определены. ");
                        }
                    }
                }
                return _extentsInModel;
            }
        }

        public Matrix3d BlockTransform { get; set; }
        public Error Error { get; set; }

        public string Direction { get; set; }
        public string LocationPoint { get; set; }

        public EnumBaseStatus BaseStatus { get; set; }

        /// <summary>
        /// F_R_Flats
        /// </summary>
        public object DBObject { get; set; }

        /// <summary>
        /// Создание блока для экспорта из id
        /// Если id не блока, то Exception
        /// </summary>      
        public Apartment(BlockReference blRef, string blName)
        {
            Name = blName;
            IdBlRef = blRef.Id;
            IdBtr = blRef.BlockTableRecord;
            BlockTransform = blRef.BlockTransform;
            Position = blRef.Position;
            Rotation = blRef.Rotation;
            Direction = Element.GetDirection(Rotation);
            LocationPoint = TypeConverter.Point(Position);
            File = Path.Combine(Path.GetDirectoryName(IdBlRef.Database.Filename), Name + ".dwg");

            // Определение модулуй в квартире
            Modules = Module.GetModules(this);
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

            // Выключение слоев штриховки
            layersOff = LayerService.LayersOff(Options.Instance.LayersOffMatch);

            var apartsToFile = apartments.Where(a => !a.BaseStatus.HasFlag(EnumBaseStatus.NotInDwg));

            foreach (var apart in apartsToFile)
         {
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

            // Восстановление слоев
            LayerService.LayersOn(layersOff);

            return count;
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
                        using (var blRef = idBlRefMap.Open(OpenMode.ForWrite, false, true) as BlockReference)
                        {
                            blRef.Position = Point3d.Origin;
                        }
                        db.SaveAs(File, DwgVersion.Current);
                    }
                }
                //Inspector.AddError($"Экспортирован блок {Name} в файл {File}", IdBlRef, icon: System.Drawing.SystemIcons.Information);
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

            using (var ms = SymbolUtilityServices.GetBlockModelSpaceId(db).Open(OpenMode.ForRead) as BlockTableRecord)
            {
                foreach (ObjectId idEnt in ms)
                {
                    using (var blRefApart = idEnt.Open(OpenMode.ForRead, false, true) as BlockReference)
                    {
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
                }
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
    }
}
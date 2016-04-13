using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using AcadLib.Blocks;
using AcadLib.Errors;
using AR_ApartmentBase.Model.DB.DbServices;
using AR_ApartmentBase.Model.DB.EntityModel;
using AR_ApartmentBase.Model.Revit.Elements;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace AR_ApartmentBase.Model.Revit
{
    /// <summary>
    /// Модуль - блок помещения в автокаде
    /// </summary>
    public class Module : IRevitBlock, IEquatable<Module>
    {
        public Apartment Apartment { get; set; }

        public string Name { get; set; }

        /// <summary>
        /// Точка вставки модуля
        /// </summary>      
        public Point3d Position { get; set; }

        public double Rotation { get; set; }

        public List<Element> Elements { get; set; }

        public ObjectId IdBlRef { get; set; }

        public ObjectId IdBtr { get; set; }

        public Matrix3d BlockTransform { get; set; }

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
                            _extentsInModel.TransformBy(Apartment.BlockTransform);
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

        public Error Error { get; set; }

        public string Direction { get; set; }
        public string LocationPoint { get; set; }

        public EnumBaseStatus BaseStatus { get; set; }
        public int Revision { get; set; }

        /// <summary>
        /// F_nn_FlatModules
        /// </summary>
        public object DBObject { get; set; }

        public string NodeName
        {
            get
            {
                return "Модуль " + Name + " " + ((Revision > 0) ? "Ревизия-" + Revision.ToString() : ""); ;
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

        public Module(BlockReference blRefModule, Apartment apartment, string blName)
        {
            Apartment = apartment;
            BlockTransform = blRefModule.BlockTransform;
            IdBlRef = blRefModule.Id;
            IdBtr = blRefModule.BlockTableRecord;
            Position = blRefModule.Position;
            Rotation = blRefModule.Rotation;
            Direction = Element.GetDirection(Rotation);
            LocationPoint = TypeConverter.Point(Position);

            Parameters = Parameter.GetParameters(blRefModule, blName);

            Name = getModuleName(blRefModule, blName);

            Elements = Element.GetElements(this);
        }

        private string getModuleName(BlockReference blRefModule, string blName)
        {
            string name = string.Empty;
            if (blRefModule.IsDynamicBlock)
            {
                name = Parameters.SingleOrDefault(p =>
                            p.Name.Equals(Options.Instance.ParameterModuleName, StringComparison.OrdinalIgnoreCase))?.Value;
                if (string.IsNullOrEmpty(name))
                {
                    Inspector.AddError($"Для дин.блока модуля '{blName}' не определен параметр имени модуля '{Options.Instance.ParameterModuleName}'.",
                       blRefModule, Apartment.BlockTransform, System.Drawing.SystemIcons.Error);
                }
            }
            else
            {
                name = blName;
            }
            return name;
        }

        /// <summary>
        /// Конструктор для создания модуля из Базы
        /// </summary>
        public Module(F_nn_FlatModules fmEnt, Apartment apart)
        {
            Name = fmEnt.F_R_Modules.NAME_MODULE;
            Apartment = apart;
            _extentsAreDefined = true;
            _extentsIsNull = true;
            Elements = new List<Element>();
            apart.Modules.Add(this);
            Direction = fmEnt.DIRECTION;
            LocationPoint = fmEnt.LOCATION;
            DBObject = fmEnt;
            Revision = fmEnt.F_R_Modules.REVISION;
        }

        /// <summary>
        /// Поиск модулей в квартире
        /// </summary>      
        public static List<Module> GetModules(Apartment apartment)
        {
            List<Module> modules = new List<Module>();
            var btrApartment = apartment.IdBtr.GetObject(OpenMode.ForRead) as BlockTableRecord;

            foreach (var idEnt in btrApartment)
            {
                var blRefModule = idEnt.GetObject(OpenMode.ForRead, false, true) as BlockReference;

                if (blRefModule == null || !blRefModule.Visible) continue;
                string blName = blRefModule.GetEffectiveName();
                if (IsBlockNameModule(blName))
                {
                    // Проверка масштабирования блока
                    if (!blRefModule.CheckNaturalBlockTransform())
                    {
                        Inspector.AddError($"Блок модуля масштабирован '{blName}' - {blRefModule.ScaleFactors.ToString()}.",
                           blRefModule, apartment.BlockTransform, icon: System.Drawing.SystemIcons.Error);
                    }

                    Module module = new Module(blRefModule, apartment, blName);
                    modules.Add(module);
                }
                else
                {
                    Inspector.AddError($"Отфильтрован блок модуля '{blName}' в блоке квартиры {apartment.Name}, имя не соответствует " +
                       $"'{Options.Instance.BlockModuleNameMatch}",
                       blRefModule, apartment.BlockTransform,
                       icon: System.Drawing.SystemIcons.Information);
                }
            }
            modules.Sort((m1, m2) => m1.Name.CompareTo(m2.Name));
            return modules;
        }

        public static bool IsBlockNameModule(string blName)
        {
            return Regex.IsMatch(blName, Options.Instance.BlockModuleNameMatch, RegexOptions.IgnoreCase);
        }

        public bool Equals(Module other)
        {
            return this.Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase) &&
                   this.Direction.Equals(other.Direction) &&
                   this.LocationPoint.Equals(other.LocationPoint) &&
                   this.Elements.SequenceEqual(other.Elements);
        }

        public ObjectId[] GetSubentPath()
        {
            return new[] { Apartment.IdBlRef, IdBlRef };
        }
    }
}
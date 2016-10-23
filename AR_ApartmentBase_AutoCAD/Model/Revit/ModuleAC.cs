using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using AcadLib.Blocks;
using AcadLib.Errors;
using AR_ApartmentBase.Model;
using AR_ApartmentBase.Model.DB.EntityModel;
using AR_ApartmentBase.Model.Elements;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace AR_ApartmentBase_AutoCAD
{
    /// <summary>
    /// Модуль - блок помещения в автокаде
    /// </summary>
    public class ModuleAC : Module, IRevitBlock
    {
        public ApartmentAC ApartmentAC { get; set; }
        /// <summary>
        /// Точка вставки модуля
        /// </summary>      
        public Point3d Position { get; set; }        
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
                            _extentsInModel.TransformBy(ApartmentAC.BlockTransform);
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

        public Error Error { get; set; }

        public EnumBaseStatus BaseStatus { get; set; }        

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

        public ModuleAC(BlockReference blRefModule, ApartmentAC apartment, string blName)
        {
            Apartment = apartment;
            BlockTransform = blRefModule.BlockTransform;
            IdBlRef = blRefModule.Id;
            IdBtr = blRefModule.BlockTableRecord;
            Position = blRefModule.Position;
            Rotation = blRefModule.Rotation;
            Direction = ElementAC.GetDirection(Rotation);
            LocationPoint = TypeConverter.Point(Position);

            Parameters = ParameterAC.GetParameters(blRefModule, blName, apartment.BlockTransform);

            Name = getModuleName(blRefModule, blName);

            Elements = ElementAC.GetElements(this);
        }

        private string getModuleName(BlockReference blRefModule, string blName)
        {
            string name = string.Empty;
            if (blRefModule.IsDynamicBlock)
            {
                name = Parameters.SingleOrDefault(p =>
                            p.Name.Equals(OptionsAC.Instance.ParameterModuleName, StringComparison.OrdinalIgnoreCase))?.Value;
                if (string.IsNullOrEmpty(name))
                {
                    Inspector.AddError($"Для дин.блока модуля '{blName}' не определен параметр имени модуля '{OptionsAC.Instance.ParameterModuleName}'.",
                       blRefModule, ApartmentAC.BlockTransform, System.Drawing.SystemIcons.Error);
                }
            }
            else
            {
                name = blName;
            }
            return name;
        }        

        /// <summary>
        /// Поиск модулей в квартире
        /// </summary>      
        public static List<Module> GetModules(ApartmentAC apartment)
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

                    Module module = new ModuleAC(blRefModule, apartment, blName);
                    modules.Add(module);
                }
                else
                {
                    Inspector.AddError($"Отфильтрован блок модуля '{blName}' в блоке квартиры {apartment.Name}, имя не соответствует " +
                       $"'{OptionsAC.Instance.BlockModuleNameMatch}",
                       blRefModule, apartment.BlockTransform,
                       icon: System.Drawing.SystemIcons.Information);
                }
            }
            modules.Sort((m1, m2) => m1.Name.CompareTo(m2.Name));
            return modules;
        }

        public static bool IsBlockNameModule(string blName)
        {
            return Regex.IsMatch(blName, OptionsAC.Instance.BlockModuleNameMatch, RegexOptions.IgnoreCase);
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
            return new[] { ApartmentAC.IdBlRef, IdBlRef };
        }
    }
}
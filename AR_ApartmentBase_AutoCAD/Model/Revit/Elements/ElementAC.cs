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
    /// Элемент - блок в автокаде из которых состоит модуль - стены, окна, двери, мебель и т.п.
    /// </summary>      
    public class ElementAC : Element, IRevitBlock, IEquatable<ElementAC>
    {
        public ElementAC () { }

        public Module Module { get; set; }
        /// <summary>
        /// Точка вставки относительно базовой точки квартиры
        /// </summary>      
        public Point3d Position { get; set; }
        /// <summary>
        /// Поворот относительно 0 в блоке квартиры
        /// </summary>
        public double Rotation { get; set; }        
        public ObjectId IdBlRef { get; set; }
        public ObjectId IdBtr { get; set; }        
        public Matrix3d BlockTransform { get; set; }
        public Error Error { get; set; }        

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
                    using (var t = IdBlRef.Database.TransactionManager.StartTransaction())
                    {
                        var blRef = IdBlRef.GetObject(OpenMode.ForRead, false, true) as BlockReference;                    
                        try
                        {
                            _extentsInModel = blRef.GeometricExtents;
                            var moduleAC = (ModuleAC)Module;
                            _extentsInModel.TransformBy(moduleAC.BlockTransform* ((ApartmentAC)moduleAC.Apartment).BlockTransform);
                        }
                        catch
                        {
                            _extentsIsNull = true;
                        }
                        t.Commit();
                    }
                }                
                return _extentsInModel;
            }
        }

        public EnumBaseStatus BaseStatus { get; set; }

        public string NodeName
        {
            get
            {
                return CategoryElement + " " + FamilySymbolName;
            }
        }

        public string Info
        {
            get
            {
                return "Инфо:\r\n" +
                    NodeName + "\r\n" +
                    "Категория \t" + CategoryElement + "\r\n" +
                    "Семейство \t" + FamilyName + "\r\n" +
                    "Типоразмер \t" + FamilySymbolName + "\r\n" +
                    "Точка вставки \t" + LocationPoint + "\r\n" +
                    "Направление \t" + Direction + "\r\n" +                    
                    "Параметры:\r\n" +
                    string.Join ("\r\n", Parameters.Select(p=> p.Name + " = " + p.Value));                    
            }
        }

        public ElementAC(BlockReference blRefElem, Module module, string blName, List<Parameter> parameters, string category)
        {
            Name = blName;
            Module = module;
            IdBlRef = blRefElem.Id;
            IdBtr = blRefElem.BlockTableRecord;
            BlockTransform = blRefElem.BlockTransform;
            Position = blRefElem.Position;
            Rotation = blRefElem.Rotation;
            Direction = ElementAC.GetDirection(Rotation);
            LocationPoint = TypeConverter.Point(Position);

            CategoryElement = category;

            FamilyName = parameters.SingleOrDefault(p => p.Name.Equals(OptionsAC.Instance.ParameterFamilyName))?.Value ?? "";
            FamilySymbolName = parameters.SingleOrDefault(p => p.Name.Equals(OptionsAC.Instance.ParameterFamilySymbolName))?.Value ?? "";

            Parameters = ParameterAC.ExceptOnlyRequiredParameters(parameters, category);
        }

        /// <summary>
        /// Поиск элементов в блоке модуля
        /// </summary>      
        public static List<IElement> GetElements(ModuleAC module)
        {
            List<IElement> elements = new List<IElement>();

            var btrModule = module.IdBtr.GetObject(OpenMode.ForRead, false, true) as BlockTableRecord;
            foreach (var idEnt in btrModule)
            {
                using (var blRefElem = idEnt.GetObject(OpenMode.ForRead, false, true) as BlockReference)
                {
                    if (blRefElem == null || !blRefElem.Visible) continue;

                    string blName = blRefElem.GetEffectiveName();

                    var apartAC = (ApartmentAC)module.Apartment;

                    if (IsBlockElement(blName))
                    {                        
                        // Проверка масштабирования блока
                        if (!blRefElem.CheckNaturalBlockTransform())
                        {
                            Inspector.AddError($"Блок элемента масштабирован '{blName}' - {blRefElem.ScaleFactors.ToString()}.",
                               blRefElem, module.BlockTransform * apartAC.BlockTransform, icon: System.Drawing.SystemIcons.Error);
                        }
                        
                        var parameters = ParameterAC.GetParameters(blRefElem, blName, module.BlockTransform* apartAC.BlockTransform);
                        var categoryElement = parameters.SingleOrDefault(p => p.Name.Equals(OptionsAC.Instance.ParameterCategoryName, StringComparison.OrdinalIgnoreCase));

                        if (categoryElement == null || string.IsNullOrEmpty(categoryElement.Value))
                        {
                            Inspector.AddError($"Не определена категория элемента у блока {blName}",
                               blRefElem, module.BlockTransform * apartAC.BlockTransform, icon: System.Drawing.SystemIcons.Error);
                        }
                        else
                        {
                            try
                            {
                                // Попытка создать элемент. Если такой категории нет в базе, то будет ошибка
                                ElementAC elem = ElementFactory.CreateElementDWG(blRefElem, module, blName, parameters, categoryElement.Value);
                                if (elem == null)
                                {
                                    Inspector.AddError($"Не удалось создать элемент из блока '{blName}', категории '{categoryElement.Value}'.",
                                       blRefElem, module.BlockTransform * apartAC.BlockTransform, icon: System.Drawing.SystemIcons.Error);
                                    continue;
                                }
                                // проверка элемента
                                elem.checkElement();
                                if (!elem.BaseStatus.HasFlag(EnumBaseStatus.Error))
                                {
                                    elements.Add(elem);
                                }
                            }
                            catch (Exception ex)
                            {
                                Inspector.AddError($"Ошибка при создании элемента из блока '{blName}' категории '{categoryElement.Value}'. Возможно такой категории нет в базе. - {ex.ToString()}.",
                                      blRefElem, module.BlockTransform * apartAC.BlockTransform, icon: System.Drawing.SystemIcons.Error);
                            }
                        }
                    }
                    else
                    {
                        var extInModel = blRefElem.GeometricExtents;
                        extInModel.TransformBy(module.BlockTransform * apartAC.BlockTransform);

                        Inspector.AddError($"Отфильтрован блок элемента '{blName}' имя не соответствует блоку элемента - {OptionsAC.Instance.BlockElementNameMatch}.",
                           extInModel, idEnt, icon: System.Drawing.SystemIcons.Information);
                    }
                }
            }

            // Для элементов стены (которые должны принадлежать стене)
            var hostsWallElems = elements.OfType<IWallHost>().ToList();
            foreach (var hostWall in hostsWallElems)
            {
                hostWall.SearchHostWallDwg(elements);
            }
            elements.Sort((e1, e2) => e1.Name.CompareTo(e2.Name));
            return elements;
        }

        /// <summary>
        /// Проверка элемента - есть ли все необходимые параметры
        /// </summary>
        private void checkElement()
        {
            // категорию не нужно проверять, без категории элемент не был бы создан.
            // проверка наличия всех параметров
            string errElem = string.Empty;
            var paramsForCategory = BaseApartments.GetBaseCategoryParameters().Find(c => c.Key.Equals(CategoryElement, StringComparison.OrdinalIgnoreCase)).Value;
            if (paramsForCategory != null)
            {
                foreach (var paramEnt in paramsForCategory)
                {
                    Parameter paramElem = null;
                    try
                    {
                        paramElem = Parameters.SingleOrDefault(p => p.Name.Equals(paramEnt.NAME_PARAMETER, StringComparison.OrdinalIgnoreCase));
                    }
                    catch
                    {
                        // Дублирование параметров
                        errElem += $"Дублирование параметра '{paramEnt.NAME_PARAMETER}'. ";
                    }
                    if (paramElem == null)
                    {
                        // Нет такого параметра
                        errElem += $"Нет параметра '{paramEnt.NAME_PARAMETER}'. ";
                    }
                }
            }
            else
            {
                // Неизвестная категория элемента
                errElem += $"Неизвестная категория '{CategoryElement}'. ";                
            }

            if (!string.IsNullOrEmpty(errElem))
            {
                BaseStatus = EnumBaseStatus.Error;
                Inspector.AddError($"Пропущен блок элемента '{Name}', ошибка - {errElem}", ExtentsInModel, IdBlRef, System.Drawing.SystemIcons.Error);
            }
        }

        public static bool IsBlockElement(string blName)
        {
            return Regex.IsMatch(blName, OptionsAC.Instance.BlockElementNameMatch, RegexOptions.IgnoreCase);
        }

        public static string GetDirection(double rotation)
        {
            Vector3d direction = new Vector3d(1, 0, 0);
            direction = direction.RotateBy(rotation, Vector3d.ZAxis);
            return TypeConverter.Point(direction);
        }
        

        public ObjectId[] GetSubentPath()
        {            
            return new[] { ((ApartmentAC)Module.Apartment).IdBlRef, ((ModuleAC)Module).IdBlRef, IdBlRef };            
        }

        public void AddErrMsg(string errElem)
        {
            if (Error == null)
            {
                Error = new Error(errElem);
            }
            else
            {
                Error.AdditionToMessage(errElem);
            }
        }

        public void DefineOrientation (BlockReference blRefElem)
        {
            // Определение направления
            var btr = blRefElem.BlockTableRecord.GetObject(OpenMode.ForRead) as BlockTableRecord;
            bool isFinded = false;
            foreach (var idEnt in btr)
            {
                var lineOrient = idEnt.GetObject(OpenMode.ForRead, false, true) as Line;
                if (lineOrient == null || lineOrient.ColorIndex != OptionsAC.Instance.DirectionLineColorIndex || !lineOrient.Visible) continue;
                var lineTemp = (Line)lineOrient.Clone();
                lineTemp.TransformBy(blRefElem.BlockTransform);
                Direction = ElementAC.GetDirection(lineTemp.Angle);
                isFinded = true;
                break;
            }
            if (!isFinded)
            {
                Inspector.AddError($"Не определено направление открывания в блоке - {Name}. " +
                   $"Направление открывания определяется отрезком с цветом {OptionsAC.Instance.DirectionLineColorIndex} - вектор от стартовой точки отрезка.",
                   this.ExtentsInModel, this.IdBlRef, System.Drawing.SystemIcons.Error);
            }
        }

        public virtual bool Equals (ElementAC other)
        {
            if (other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;
            var res = this.CategoryElement.Equals(other.CategoryElement) &&
               this.Direction.Equals(other.Direction) &&
               this.LocationPoint.Equals(other.LocationPoint) &&
               this.FamilyName.Equals(other.FamilyName) &&
               this.FamilySymbolName.Equals(other.FamilySymbolName) &&
               Parameter.Equal(this.Parameters, other.Parameters);
            return res;
        }        
    }
}

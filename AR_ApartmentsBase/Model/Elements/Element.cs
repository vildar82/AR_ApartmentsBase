using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using AR_ApartmentBase.Model.DB.EntityModel;

namespace AR_ApartmentBase.Model.Elements
{
    /// <summary>
    /// Элемент - блок в автокаде из которых состоит модуль - стены, окна, двери, мебель и т.п.
    /// </summary>      
    public class Element : IElement, IEquatable<IElement>
    {
        public string FamilyName { get; set; }
        public string FamilySymbolName { get; set; }
        public string CategoryElement { get; set; }
        public F_S_Elements DBElement { get; set; }
        public F_nn_Elements_Modules DBElementInApart { get; set; }
        public string Name { get; set; }
        public List<Parameter> Parameters { get; set; }
        public string Direction { get; set; }
        public string LocationPoint { get; set; }

        public Element() { }

        /// <summary>
        /// Конструктор создания элемента из базы
        /// </summary>
        public Element(F_S_Elements emEnt)
        {
            CategoryElement = emEnt.F_S_Categories.NAME_RUS_CATEGORY;
            Direction = "";
            LocationPoint = "";
            FamilyName = emEnt.F_S_FamilyInfos.FAMILY_NAME;
            FamilySymbolName = emEnt.F_S_FamilyInfos.FAMILY_SYMBOL;
            DBElement = emEnt;

            // Параметры элемента в базе
            List<Parameter> parameters = new List<Parameter>();
            foreach (var item in emEnt.F_nn_ElementParam_Value)
            {
                var parameter = new Parameter(item.F_nn_Category_Parameters.F_S_Parameters.NAME_PARAMETER,
                    item.PARAMETER_VALUE);
                parameters.Add(parameter);
            }
            Parameters = Parameter.Sort(parameters);
        }

        public Element(F_nn_Elements_Modules emEnt)
        {
            CategoryElement = emEnt.F_S_Elements.F_S_Categories.NAME_RUS_CATEGORY;
            Direction = emEnt.DIRECTION;
            LocationPoint = emEnt.LOCATION;
            FamilyName = emEnt.F_S_Elements.F_S_FamilyInfos.FAMILY_NAME;
            FamilySymbolName = emEnt.F_S_Elements.F_S_FamilyInfos.FAMILY_SYMBOL;

            DBElementInApart = emEnt;

            // Параметры элемента в базе
            List<Parameter> parameters = new List<Parameter>();
            foreach (var item in emEnt.F_S_Elements.F_nn_ElementParam_Value)
            {
                var parameter = new Parameter(item.F_nn_Category_Parameters.F_S_Parameters.NAME_PARAMETER,
                    item.PARAMETER_VALUE);
                parameters.Add(parameter);
            }
            Parameters = Parameter.Sort(parameters);

        }

        /// <summary>
        /// Проверка элемента - есть ли все необходимые параметры.
        /// </summary>
        //private string checkElement()
        //{
        //    // категорию не нужно проверять, без категории элемент не был бы создан.
        //    // проверка наличия всех параметров
        //    string errElem = string.Empty;
        //    var paramsForCategory = BaseApartments.GetBaseCategoryParameters().Find(c => c.Key.Equals(CategoryElement, StringComparison.OrdinalIgnoreCase)).Value;
        //    if (paramsForCategory != null)
        //    {
        //        foreach (var paramEnt in paramsForCategory)
        //        {
        //            Parameter paramElem = null;
        //            try
        //            {
        //                paramElem = Parameters.SingleOrDefault(p => p.Name.Equals(paramEnt.NAME_PARAMETER, StringComparison.OrdinalIgnoreCase));
        //            }
        //            catch
        //            {
        //                // Дублирование параметров
        //                errElem += "Дублирование параметра "+paramEnt.NAME_PARAMETER+". ";
        //            }
        //            if (paramElem == null)
        //            {
        //                // Нет такого параметра
        //                errElem += "Нет параметра "+paramEnt.NAME_PARAMETER+". ";
        //            }
        //        }
        //    }
        //    else
        //    {
        //        // Неизвестная категория элемента
        //        errElem += "Неизвестная категория "+CategoryElement+". ";                
        //    }
        //    return errElem;
        //}

        /// <summary>
        /// Сравниваются элементы - без привязки к модулю (т.е. без Location и Direction)!!!
        /// </summary>        
        public virtual bool Equals(IElement other)
        {
            if (other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;
            var res = this.CategoryElement.Equals(other.CategoryElement) &&
                //this.Direction.Equals(other.Direction) &&
                //this.LocationPoint.Equals(other.LocationPoint) &&
               this.FamilyName.Equals(other.FamilyName) &&
               this.FamilySymbolName.Equals(other.FamilySymbolName) &&
               Parameter.Equal(this.Parameters, other.Parameters);
            return res;
        }

        public override int GetHashCode()
        {
            return CategoryElement.GetHashCode() ^ FamilyName.GetHashCode() ^ FamilySymbolName.GetHashCode();
        }        
    }
}

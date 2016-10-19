using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;

namespace AR_ApartmentBase.Model
{
    public class Parameter : IEquatable<Parameter>
    {
        public string Name { get; set; }
        public string Value { get; set; }
        private object objectValue;  

        public Parameter(string name, object value)
        {
            Name = name;
            objectValue = value;
            Value = objectValue.ToString();
        }

        public static List<Parameter> Sort(List<Parameter> parameters)
        {
            return parameters.OrderBy(p => p.Name).ToList();
        }

        private static void addParam (List<Parameter> parameters, string name, object value)
        {
            if (hasParamName(parameters, name))
            {

            }
            else
            {
                Parameter param = new Parameter(name, value);
                parameters.Add(param);
            }
        }

        /// <summary>
        /// Оставить только нужные для базы параметры
        /// </summary>      
        public static List<Parameter> ExceptOnlyRequiredParameters(List<Parameter> parameters, string category)
        {
            var paramsCategory = Apartment.BaseCategoryParameters.SingleOrDefault(c => c.Key.Equals(category)).Value;
            List<Parameter> resVal = new List<Parameter>();

            if (paramsCategory != null)
            {
                foreach (var param in parameters)
                {
                    var paramDb = paramsCategory.FirstOrDefault(p => p.NAME_PARAMETER.Equals(param.Name, StringComparison.OrdinalIgnoreCase));
                    if (paramDb != null)
                    {                        
                        resVal.Add(param);
                    }
                }
                if (paramsCategory.Exists(p => p.NAME_PARAMETER.Equals("FuckUp", StringComparison.OrdinalIgnoreCase)))
                {
                    if (!resVal.Exists(p => p.Name.Equals("FuckUp", StringComparison.OrdinalIgnoreCase)))
                    {
                        resVal.Add(new Parameter("FuckUp", ""));
                    }
                }
            }
            return resVal;
        }

        private static bool hasParamName(List<Parameter> parameters, string name)
        {
            return parameters.Exists(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public bool Equals(Parameter other)
        {
            return this.Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase) &&
               this.Value.Equals(other.Value, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode ()
        {
            return Name.GetHashCode();
        }

        /// <summary>
        /// проверка списков параметров.
        /// Все элементы из второго списка обязательно должны соответствовать первому списку, 
        /// второй список может содержать лишние параметры
        /// </summary>      
        public static bool Equal(List<Parameter> params1, List<Parameter> params2)
        {
            foreach (var p1 in params1)
            {
                if (!params2.Contains(p1))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
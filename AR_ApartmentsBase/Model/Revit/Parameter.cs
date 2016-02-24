using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using AcadLib.Errors;
using System.Drawing;
using AR_ApartmentBase.Model.DB.DbServices;

namespace AR_ApartmentBase.Model.Revit
{
   public class Parameter : IEquatable<Parameter>
   {
      public string Name { get;  set; }      
      public string Value { get;  set; }
      private object objectValue;      

      // Константные атрибуты в блоках
      public static Dictionary<ObjectId, List<Parameter>> BlocksConstantAtrs = new Dictionary<ObjectId, List<Parameter>>();

      public Parameter (string name, object value)
      {
         Name = name;
         objectValue = value;
         Value = objectValue.ToString();     
      }      

      public static List<Parameter> GetParameters(BlockReference blRef)
      {         
         List<Parameter> parameters = new List<Parameter>();         

         // считывание дин параметров
         defineDynParams(blRef, parameters);

         // считывание атрибутов
         defineAttributesParam(blRef, parameters);

         // Сортировка параметров по имени
         parameters= Sort(parameters);

         return parameters;
      }

      public static List<Parameter> Sort(List<Parameter> parameters)
      {
         return parameters.OrderBy(p => p.Name).ToList();         
      }

      private static void defineDynParams(BlockReference blRef, List<Parameter> parameters)
      {
         if (blRef.IsDynamicBlock)
         {
            foreach (DynamicBlockReferenceProperty prop in blRef.DynamicBlockReferencePropertyCollection)
            {
               Error errHasParam = new Error($"Дублирование параметра {prop.PropertyName} в блоке {blRef.Name}.", icon: SystemIcons.Error);
               addParam(parameters, prop.PropertyName, prop.Value, errHasParam);
            }
         }         
      }      

      private static void defineAttributesParam(BlockReference blRef, List<Parameter> parameters)
      {
         if (blRef.AttributeCollection != null)
         {            
            foreach (ObjectId idAtrRef in blRef.AttributeCollection)
            {
               using (var atrRef = idAtrRef.Open(OpenMode.ForRead, false, true) as AttributeReference)
               {
                  if (atrRef != null)
                  {
                     Error errHasParam = new Error($"Дублирование параметра {atrRef.Tag} в блоке {blRef.Name}.", icon: SystemIcons.Error);
                     addParam(parameters, atrRef.Tag, atrRef.TextString, errHasParam);                     
                  }
               }
            }
         }
         // Добавка константных атрибутов
         parameters.AddRange(getConstAtrParameters(blRef));
      }

      private static List<Parameter> getConstAtrParameters(BlockReference blRef)
      {
         List<Parameter> constAtrParameters;
         ObjectId idBtr = blRef.DynamicBlockTableRecord;
         if (!BlocksConstantAtrs.TryGetValue(idBtr, out constAtrParameters))
         {
            constAtrParameters = new List<Parameter>();
            using (var btr = idBtr.Open( OpenMode.ForRead) as BlockTableRecord)
            {
               foreach (var idEnt in btr)
               {
                  using (var atr = idEnt.Open( OpenMode.ForRead, false, true)as AttributeDefinition)
                  {
                     if (atr == null || !atr.Constant) continue;
                     Parameter constAtrParam = new Parameter(atr.Tag.Trim(), atr.TextString.Trim());
                     constAtrParameters.Add(constAtrParam);
                  }
               }
               BlocksConstantAtrs.Add(idBtr, constAtrParameters);
            }
         }
         return constAtrParameters;
      }

      private static void addParam (List<Parameter> parameters, string name, object value, Error errorHasParam)
      {
         if (hasParamName(parameters, name))
         {
            Inspector.Errors.Add(errorHasParam);
         }
         else
         {
            if (!Options.Instance.IgnoreParamNames.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
               Parameter param = new Parameter(name, value);               
               parameters.Add(param);
            }
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
                  param.ConvertValueToDbType(paramDb.TYPE_PARAMETER);
                  resVal.Add(param);
               }
            }
            if (paramsCategory.Exists(p=>p.NAME_PARAMETER.Equals ("FuckUp", StringComparison.OrdinalIgnoreCase)))
            {
               if (!resVal.Exists(p=>p.Name.Equals("FuckUp", StringComparison.OrdinalIgnoreCase)))
               {
                  resVal.Add(new Parameter("FuckUp", ""));
               }
            }
         }
         return resVal;
      }

      /// <summary>
      /// Приведение значения параметра в соответствие с типом значения нужным для базы
      /// </summary>      
      public void ConvertValueToDbType(string tYPE_PARAMETER)
      {
         switch (tYPE_PARAMETER)
         {
            case "Double":
               Value = Convert.ToDouble(objectValue).ToString("F4");
               break;
            case "Int":
               Value = Convert.ToInt32(objectValue).ToString();
               break;
            case "Point":
               Value = TypeConverter.Point(objectValue);
               break;
            default:
               Value = objectValue.ToString();
               break;
         }
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
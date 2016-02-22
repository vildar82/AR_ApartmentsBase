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

      // Константные атрибуты в блоках
      public static Dictionary<ObjectId, List<Parameter>> BlocksConstantAtrs = new Dictionary<ObjectId, List<Parameter>>();

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
               addParam(parameters, prop.PropertyName, TypeConverter.Object(prop.Value), errHasParam);
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
                     Parameter constAtrParam = new Parameter() { Name = atr.Tag.Trim(), Value = atr.TextString.Trim() };
                     constAtrParameters.Add(constAtrParam);
                  }
               }
               BlocksConstantAtrs.Add(idBtr, constAtrParameters);
            }
         }
         return constAtrParameters;
      }

      private static void addParam (List<Parameter> parameters, string name, string value, Error errorHasParam)
      {
         if (hasParamName(parameters, name))
         {
            Inspector.Errors.Add(errorHasParam);
         }
         else
         {
            if (!Options.Instance.IgnoreParamNames.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
               Parameter param = new Parameter();
               param.Name = name;
               param.Value = value;
               parameters.Add(param);
            }
         }
      }

      /// <summary>
      /// Оставить только нужные для базы параметры
      /// </summary>      
      public static List<Parameter> ExceptOnlyRequiredParameters(List<Parameter> parameters, string category)
      {         
         var paramsCategory = Apartment.BaseCategoryParameters.Single(c => c.Key.Equals(category)).Value;
         List<Parameter> resVal = new List<Parameter>();

         foreach (var param in parameters)
         {
            if (paramsCategory.Any(p=>p.NAME_PARAMETER.Equals(param.Name, StringComparison.OrdinalIgnoreCase)))
            {
               resVal.Add(param);
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

      /// <summary>
      /// проверка списков параметров.
      /// Все элементы из второго списка обязательно должны соответствовать первому списку, 
      /// второй список может содержать лишние параметры
      /// </summary>      
      public static bool Equal(List<Parameter> params1, List<Parameter> params2)
      {         
         foreach (var p1 in params1)
         {
            if (!params2.Exists(p => p.Name.Equals(p1.Name, StringComparison.OrdinalIgnoreCase) &&
                                       p.Value.Equals(p1.Value, StringComparison.OrdinalIgnoreCase)))
            {
               return false;
            }
         }
         return true;
      }
   }
}
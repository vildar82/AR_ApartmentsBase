using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using AcadLib.Errors;
using System.Drawing;

namespace AR_ApartmentBase.Model.Revit
{
   public class Parameter : IEquatable<Parameter>
   {
      public string Name { get;  set; }      
      public string Value { get;  set; }

      public static List<Parameter> GetParameters(BlockReference blRef, IRevitBlock rBlock)
      {
         List<Parameter> parameters = new List<Parameter>();

         // Добавление параметров LocationPoint и Direction
         parameters.Add(new Parameter() { Name = nameof(IRevitBlock.LocationPoint), Value = rBlock.LocationPoint });
         parameters.Add(new Parameter() { Name = nameof(IRevitBlock.Direction), Value = rBlock.Direction });

         // считывание дин параметров
         defineDynParams(blRef, parameters);

         // считывание атрибутов
         defineAttributesParam(blRef, parameters);

         //// Сортировка параметров по имени
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
               addParam(parameters, prop.PropertyName, prop.Value.ToString(), errHasParam);               
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
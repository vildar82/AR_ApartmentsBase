using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using AcadLib.Errors;
using System.Drawing;

namespace AR_ApartmentBase.Model.Revit
{
   public class Parameter
   {
      public string Name { get;  set; }      
      public string Value { get;  set; }

      public static List<Parameter> GetParameters(BlockReference blRef, IRevitBlock rBlock)
      {
         List<Parameter> parameters = new List<Parameter>();

         // Добавление параметров LocationPoint и Direction
         parameters.Add(new Parameter() { Name = "LocationPoint", Value = rBlock.LocationPoint });
         parameters.Add(new Parameter() { Name = "Direction", Value = rBlock.Direction });

         // считывание дин параметров
         defineDynParams(blRef, parameters);

         // считывание атрибутов
         defineAttributesParam(blRef, parameters);        

         return parameters;
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
   }
}
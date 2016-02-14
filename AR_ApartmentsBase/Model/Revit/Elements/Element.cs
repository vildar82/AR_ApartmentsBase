using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using AcadLib.Errors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace AR_ApartmentBase.Model.Revit.Elements
{
   /// <summary>
   /// Элемент - блок в автокаде из которых состоит модуль - стены, окна, двери, мебель и т.п.
   /// </summary>      
   public class Element : IRevitBlock
   {
      public Element() { }

      public Parameter FamilyName { get; set; }
      public Parameter FamilySymbolName { get; set; }

      public string TypeElement { get; set; }

      public string BlockName { get; set; }    

      /// <summary>
      /// Точка вставки относительно базовой точки квартиры
      /// </summary>      
      public Point3d Position { get; set; }
      public Vector3d Direction { get; set; }

      /// <summary>
      /// Поворот относительно 0 в блоке квартиры
      /// </summary>
      public double Rotation { get; set; }      

      /// <summary>
      /// Параметры элемента
      /// </summary>
      public List<Parameter> Parameters { get; set; }
      
      public ObjectId IdBlRefElement { get; set; }
      
      public ObjectId IdBtrElement { get; set; }
      
      [XmlIgnore]
      public Module Module { get;  set; }      

      public Element(BlockReference blRefElem, Module module, string blName)
      {
         BlockName = blName;
         Module = module;
         IdBlRefElement = blRefElem.Id;
         IdBtrElement = blRefElem.BlockTableRecord;
         Position = blRefElem.Position;
         Rotation = blRefElem.Rotation;
         Direction = GetDirection(blRefElem);

         Parameters = Parameter.GetParameters(blRefElem);

         FamilyName = Parameters.SingleOrDefault(p => p.Name.Equals(Options.Instance.ParameterFamilyName));
         FamilySymbolName = Parameters.SingleOrDefault(p => p.Name.Equals(Options.Instance.ParameterFamilySymbolName));
      }

      /// <summary>
      /// Поиск элементов в блоке модуля
      /// </summary>      
      public static List<Element> GetElements(Module module)
      {
         List<Element> elements = new List<Element>();

         using ( var btrModule = module.IdBtrModule.Open( OpenMode.ForRead, false, true) as BlockTableRecord)
         {
            foreach (var idEnt in btrModule)
            {
               using (var blRefElem = idEnt.Open( OpenMode.ForRead, false, true)as BlockReference )
               {
                  if (blRefElem == null) continue;

                  string blName = blRefElem.GetEffectiveName();
                  string typeElement;
                  if (IsBlockElement(blName, out typeElement))
                  {
                     Element element = new Element(blRefElem, module, blName);
                     element.TypeElement = typeElement;
                     if (element != null)
                     {
                        //Element element = new Element(blRefElem, module, blName);
                        elements.Add(element);
                     }
                     else
                     {
                        Inspector.AddError($"Не определен тип элемента по блоку {blName}");
                     }
                  }
                  else
                  {
                     Inspector.AddError($"Отфильтрован блок элемента '{blName}' в блоке модуля {module.BlockName} " + 
                        $"в квартире {module.Apartment.BlockName}, имя не соответствует блоку элемента.",
                        icon: System.Drawing.SystemIcons.Information);
                  }
               }
            }
         }
         return elements;
      }

      public static bool IsBlockElement(string blName, out string typeElement)
      {
         var tokens = blName.Split('_');
         if (tokens.Count()>=4 &&
            tokens[0].Equals("RV") &&
            tokens[1].Equals("EL") &&
            tokens[2].Equals("BS")
            )
         {
            typeElement = tokens[3];
            return true;
         }
         typeElement = string.Empty;
         return false;
      }      

      public static Vector3d GetDirection (BlockReference blRef)
      {
         Vector3d direction = new Vector3d(1, 0, 0);
         return direction.RotateBy(blRef.Rotation, Vector3d.ZAxis);
      }
   }
}

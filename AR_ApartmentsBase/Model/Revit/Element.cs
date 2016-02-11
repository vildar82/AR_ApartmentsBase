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

namespace AR_ApartmentBase.Model.Revit
{
   /// <summary>
   /// Элемент ревита - семейство
   /// </summary>
   [Serializable]
   public class Element : IRevitBlock
   {
      private Element() { }

      public string BlockName { get; set; }    

      /// <summary>
      /// Точка вставки относительно базовой точки квартиры
      /// </summary>
      [XmlIgnore]
      public Point3d Position { get; set; }

      public string LocationPoint { get { return Position.ToString(); } set { } }

      /// <summary>
      /// Поворот относительно 0 в блоке квартиры
      /// </summary>
      public double Rotation { get; set; }      

      public List<Parameter> Parameters { get; set; }

      [XmlIgnore]
      public ObjectId IdBlRefElement { get; set; }
      [XmlIgnore]
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

         Parameters = Parameter.GetParameters(blRefElem);
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
                  if (IsBlockElement (blName))
                  {
                     Element element = new Element(blRefElem, module, blName);
                     elements.Add(element);
                  }
                  else
                  {
                     Inspector.AddError($"Отфильтрован блок элемента '{blName}' в блоке модуля {module.BlockName} " + 
                        $"в квартире {module.Apartment.BlockName}, имя не соответствует " +
                        $"'{Options.Instance.BlockElementNameMatch}",
                        icon: System.Drawing.SystemIcons.Information);
                  }
               }
            }
         }

         return elements;
      }

      public static bool IsBlockElement(string blName)
      {
         return Regex.IsMatch(blName, Options.Instance.BlockElementNameMatch, RegexOptions.IgnoreCase);
      }      
   }
}

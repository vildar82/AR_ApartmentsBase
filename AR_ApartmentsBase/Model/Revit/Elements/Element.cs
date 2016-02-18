using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using AcadLib.Errors;
using AR_ApartmentBase.Model.DB.DbServices;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace AR_ApartmentBase.Model.Revit.Elements
{
   /// <summary>
   /// Элемент - блок в автокаде из которых состоит модуль - стены, окна, двери, мебель и т.п.
   /// </summary>      
   public class Element : IRevitBlock, IEquatable<Element>
   {
      private Element() { }

      public Parameter FamilyName { get; set; }
      public Parameter FamilySymbolName { get; set; }

      public string CategoryElement { get; set; }

      public string BlockName { get; set; }    

      /// <summary>
      /// Точка вставки относительно базовой точки квартиры
      /// </summary>      
      public Point3d Position { get; set; }      

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
      
      public Module Module { get;  set; }

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
               using (var blRef = IdBlRefElement.Open(OpenMode.ForRead, false, true) as BlockReference)
               {
                  try
                  {
                     _extentsInModel = blRef.GeometricExtents;
                     _extentsInModel.TransformBy(Module.BlockTransform*Module.Apartment.BlockTransform);

                  }
                  catch
                  {
                     _extentsIsNull = true;
                  }
               }
            }
            if (_extentsIsNull)
            {
               Application.ShowAlertDialog("Границы блока не определены");
            }
            return _extentsInModel;
         }
      }

      public string Direction { get; set; }
      public string LocationPoint { get; set; }

      public EnumBaseStatus BaseStatus { get; set; }

      public Element(BlockReference blRefElem, Module module, string blName)
      {
         BlockName = blName;
         Module = module;
         IdBlRefElement = blRefElem.Id;
         IdBtrElement = blRefElem.BlockTableRecord;
         BlockTransform = blRefElem.BlockTransform;
         Position = blRefElem.Position;
         Rotation = blRefElem.Rotation;
         Direction = Element.GetDirection(Rotation);
         LocationPoint = TypeConverter.Point(Position);

         Parameters = Parameter.GetParameters(blRefElem, this);

         FamilyName = Parameters.SingleOrDefault(p => p.Name.Equals(Options.Instance.ParameterFamilyName)) 
                        ?? new Parameter() { Name = Options.Instance.ParameterFamilyName, Value = "" };
         FamilySymbolName = Parameters.SingleOrDefault(p => p.Name.Equals(Options.Instance.ParameterFamilySymbolName)) 
                        ?? new Parameter() { Name = Options.Instance.ParameterFamilySymbolName, Value = "" };
      }

      /// <summary>
      /// Конструктор создания элемента из базы
      /// </summary>
      public Element (Module module, string familyName, string fsn, List<Parameter> parameters)
      {
         Direction = parameters.FirstOrDefault(p => p.Name.Equals(nameof(IRevitBlock.Direction)))?.Value;
         LocationPoint = parameters.FirstOrDefault(p => p.Name.Equals(nameof(IRevitBlock.LocationPoint)))?.Value;
         FamilyName = new Parameter() { Name = Options.Instance.ParameterFamilyName, Value = familyName };
         FamilySymbolName = new Parameter() { Name = Options.Instance.ParameterFamilySymbolName, Value = fsn }; 
         Module = module;
         module.Elements.Add(this);
         Parameters = parameters;
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
                  if (blRefElem == null || !blRefElem.Visible) continue;

                  string blName = blRefElem.GetEffectiveName();
                  string typeElement;
                  if (IsBlockElement(blName, out typeElement))
                  {
                     Element element = new Element(blRefElem, module, blName);
                     element.CategoryElement = typeElement;
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
         elements.Sort((e1, e2) => e1.BlockName.CompareTo(e2.BlockName));
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

      public static string GetDirection (double rotation)
      {
         Vector3d direction = new Vector3d(1, 0, 0);
         direction = direction.RotateBy(rotation, Vector3d.ZAxis);
         return TypeConverter.Point(direction);
      }

      public bool Equals(Element other)
      {
         return this.Direction.Equals(other.Direction) &&
            this.LocationPoint.Equals(other.LocationPoint) &&
            this.FamilyName.Equals(other.FamilyName) &&
            this.FamilySymbolName.Equals(other.FamilySymbolName);            
      }
   }
}

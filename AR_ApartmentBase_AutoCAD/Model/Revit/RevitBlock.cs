using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using AcadLib.Errors;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace AR_ApartmentBase.Model.Revit
{
   /// <summary>
   /// Блок ревитовского элемента
   /// </summary>
   [Serializable]
   public abstract class RevitBlock : IModevView
   {
      /// <summary>
      /// Имя блока
      /// </summary>
      public string BlockName { get; set; }

      /// <summary>
      /// Точка вставки блока.
      /// </summary>
      [XmlIgnore]
      public Point3d Position { get; set; }      

      public string LocationPoint { get { return Position.ToString(); } set { } }

      /// <summary>
      /// Поворот блока
      /// </summary>      
      public double Rotation { get; set; }

      /// <summary>
      /// Параметры элемента
      /// </summary>
      public List<Parameter> Parameters { get; set; }

      /// <summary>
      /// Родительский блок (Квартира -> Модуль -> Элемент)
      /// </summary>
      public RevitBlock Parent { get; set;}

      public List<RevitBlock> ChildElements { get; set; }

      [XmlIgnore]
      public ObjectId IdBlRef { get; set; }
      [XmlIgnore]
      public ObjectId IdBtr { get; set; }
      [XmlIgnore]
      public Matrix3d BlockTransform { get; set; }

      private Extents3d _extentsView;
      private bool _extentsIsNull;

      public Extents3d ExtentsView
      {
         get
         {
            if (_extentsView.Diagonal() == 0 && !_extentsIsNull)
            {
               using (var blRef = IdBlRef.Open(OpenMode.ForRead, false, true) as BlockReference)
               {
                  try
                  {
                     _extentsView = blRef.GeometricExtents;
                     // трансформация границы в координаты Модели
                     _extentsView.TransformBy(Parent.BlockTransform);
                  }
                  catch
                  {
                     _extentsIsNull = true;
                  }
               }
            }
            if (_extentsIsNull)
            {
               Application.ShowAlertDialog("Границы элемента не определены.");
            }
            return _extentsView;
         }
      }

      /// <summary>
      /// Поиск квартир в чертеже.
      /// </summary>      
      public static List<RevitBlock> GetChilds(ObjectId idBtr, string patternBlockNameMatch)
      {
         List<RevitBlock> places = new List<RevitBlock>();
         using (var btrParent = idBtr.Open(OpenMode.ForRead) as BlockTableRecord)
         {
            foreach (ObjectId idEnt in btrParent)
            {
               using (var blRefChild = idEnt.Open(OpenMode.ForRead, false, true) as BlockReference)
               {
                  if (blRefChild != null)
                  {
                     string blName = blRefChild.GetEffectiveName();
                     if (IsRevitBlockName(blName, patternBlockNameMatch))
                     {
                        try
                        {
                           var place = new RevitBlock(blRefChild, blName);
                           places.Add(place);
                        }
                        catch (System.Exception ex)
                        {
                           Inspector.AddError($"Ошибка считывания блока квартиры {blName} - {ex.Message}.",
                              blRefChild, icon: SystemIcons.Error);
                        }
                     }
                     else
                     {
                        Inspector.AddError($"Отфильтрован блок квартиры '{blName}', имя не соответствует " +
                           $"'{Options.Instance.BlockApartmentNameMatch}",
                           blRefChild, icon: SystemIcons.Information);
                     }
                  }
               }
            }
         }
         return places;
      }

      /// <summary>
      /// Проверка имени блока
      /// </summary>      
      public static bool IsRevitBlockName(string blName, string pattern)
      {
         return Regex.IsMatch(blName, pattern, RegexOptions.IgnoreCase);
      }
   }
}

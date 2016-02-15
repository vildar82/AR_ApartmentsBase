using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace AR_ApartmentBase.Model.Revit
{
   /// <summary>
   /// Блок ревитовского элемента
   /// </summary>
   public interface IRevitBlock
   {
      /// <summary>
      /// Имя блока
      /// </summary>
      string BlockName { get; }

      /// <summary>
      /// Точка вставки блока.
      /// </summary>
      Point3d Position { get; }          

      /// <summary>
      /// Поворот блока
      /// </summary>      
      double Rotation { get; }

      /// <summary>
      /// Направление - единичный вектор
      /// </summary>
      string Direction { get; }
      string LocationPoint { get; }

      /// <summary>
      /// Параметры элемента
      /// </summary>
      List<Parameter> Parameters { get; }      

      /// <summary>
      /// Границы блока в Модели
      /// </summary>
      Extents3d ExtentsInModel { get; }

      /// <summary>
      /// Трансформация блока.
      /// </summary>
      Matrix3d BlockTransform { get; }

      /// <summary>
      /// Описание ошибки элемента если есть.
      /// </summary>
      AcadLib.Errors.Error Error { get; }

      /// <summary>
      /// Есть ли ошибки
      /// </summary>      
      bool HasError();
   }
}

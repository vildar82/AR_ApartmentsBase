using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
      /// Параметры элемента
      /// </summary>
      List<Parameter> Parameters { get; }
   }
}

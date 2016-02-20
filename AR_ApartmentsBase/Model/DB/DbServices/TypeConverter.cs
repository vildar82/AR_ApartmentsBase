using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;

namespace AR_ApartmentBase.Model.DB.DbServices
{
   public static class TypeConverter
   {
      public static string Point(Point3d pt)
      {
         return $"{pt.X.ToString("F4")};{pt.Y.ToString("F4")};{pt.Z.ToString("F4")}";
      }

      public static string Point(Vector3d vec)
      {
         return $"{vec.X.ToString("F4")};{vec.Y.ToString("F4")};{vec.Z.ToString("F4")}";
      }

      /// <summary>
      /// Конвертация типа object в строку
      /// При этом double числа округляются.
      /// </summary>      
      public static string Object(object value)
      {
         if (value is double)
         {
            return ((double)value).ToString("F4");
         }
         return value.ToString();
      }
   }
}

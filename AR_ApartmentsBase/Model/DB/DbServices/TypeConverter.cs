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
         return $"{pt.X.ToString("0.0000")};{pt.Y.ToString("0.0000")};{pt.Z.ToString("0.0000")}";
      }

      public static string Point(Vector3d vec)
      {
         return $"{vec.X.ToString("0.0000")};{vec.Y.ToString("0.0000")};{vec.Z.ToString("0.0000")}";
      }      
   }
}

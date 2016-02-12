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
         return $"{pt.X};{pt.Y};{pt.Z}";
      }
   }
}

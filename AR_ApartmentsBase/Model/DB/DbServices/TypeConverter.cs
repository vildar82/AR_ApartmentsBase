﻿using System;
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

      public static string Point(object objectValue)
      {
         if (objectValue is Point3d)
         {
            return Point((Point3d)objectValue);
         }
         else if (objectValue is Vector3d)
         {
            return Point((Vector3d)objectValue);
         }
         else
         {
            return objectValue.ToString();
         }         
      }
   }
}

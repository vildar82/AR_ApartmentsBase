using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using AcadLib.Errors;
using AR_ApartmentBase.Model.DB.DbServices;
using AR_ApartmentBase.Model.Revit.Elements;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace AR_ApartmentBase.Model.Revit
{   
   /// <summary>
   /// Модуль - блок помещения в автокаде
   /// </summary>
   public class Module : IRevitBlock
   {
      private Module() { }

      [XmlIgnore]
      public Apartment Apartment { get;  set; }
            
      public string BlockName { get;  set; }     

      /// <summary>
      /// Точка вставки модуля
      /// </summary>      
      public Point3d Position { get;  set; }      

      public double Rotation { get;  set; }      

      public List<Element> Elements { get;  set; }
      
      public ObjectId IdBlRefModule { get;  set; }
            
      public ObjectId IdBtrModule { get;  set; }
      
      public Matrix3d BlockTransform { get;  set; }

      public List<Parameter> Parameters { get;  set; }

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
               using (var blRef = IdBlRefModule.Open(OpenMode.ForRead, false, true) as BlockReference)
               {
                  try
                  {
                     _extentsInModel = blRef.GeometricExtents;
                     _extentsInModel.TransformBy(Apartment.BlockTransform);

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

      public Error Error { get; set; }

      public string Direction { get; set; }
      public string LocationPoint { get; set; }

      public Module(BlockReference blRefModule, Apartment apartment, string blName)
      {
         BlockName = blName;
         Apartment = apartment;
         BlockTransform = blRefModule.BlockTransform;
         IdBlRefModule = blRefModule.Id;
         IdBtrModule = blRefModule.BlockTableRecord;
         Position = blRefModule.Position;
         Rotation = blRefModule.Rotation;
         Direction = Element.GetDirection(Rotation);
         LocationPoint = TypeConverter.Point(Position);

         Parameters = Parameter.GetParameters(blRefModule, this);

         Elements = Element.GetElements(this);
      }

      /// <summary>
      /// Поиск модулей в квартире
      /// </summary>      
      public static List<Module> GetModules(Apartment apartment)
      {
         List<Module> modules = new List<Module>();
         using (var btrApartment = apartment.IdBtr.Open( OpenMode.ForRead) as BlockTableRecord)
         {
            foreach (var idEnt in btrApartment)
            {
               using (var blRefModule = idEnt.Open( OpenMode.ForRead, false, true)as BlockReference)
               {
                  if (blRefModule == null) continue;
                  string blName = blRefModule.GetEffectiveName();
                  if (IsBlockNameModule(blName))
                  {
                     Module module = new Module(blRefModule, apartment, blName);
                     modules.Add(module);
                  }
                  else
                  {
                     Inspector.AddError($"Отфильтрован блок модуля '{blName}' в блоке квартиры {apartment.BlockName}, имя не соответствует " +
                        $"'{Options.Instance.BlockModuleNameMatch}",
                        icon: System.Drawing.SystemIcons.Information);
                  }
               }
            }
         }
         modules.Sort((m1, m2) => m1.BlockName.CompareTo(m2.BlockName));
         return modules;
      }

      public static bool IsBlockNameModule(string blName)
      {
         return Regex.IsMatch(blName, Options.Instance.BlockModuleNameMatch, RegexOptions.IgnoreCase);
      }

      public bool HasError()
      {
         if (Error != null)
         {
            return true;
         }
         return Elements.Any(e => e.HasError());
      }
   }
}

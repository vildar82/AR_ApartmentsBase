using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using AcadLib.Errors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AR_ApartmentBase.Model.AcadServices;
using System.Xml.Serialization;
using System.Drawing;

namespace AR_ApartmentBase.Model.Revit
{   
   public class Apartment :IRevitBlock
   {
      private static List<ObjectId> _layersOff;

      private Apartment() { }

      /// <summary>
      /// Имя блока
      /// </summary>
      public string BlockName { get; set; }
      
      public ObjectId IdBlRef { get; set; }
      
      public ObjectId IdBtr { get;  set; }
      
      public Extents3d Extents { get;  set; }  

      /// <summary>
      /// Модули в квартире.
      /// </summary>
      public List<Module> Modules { get;  set; }

      /// <summary>
      /// Дата экспорта
      /// </summary>      
      public DateTime ExportDate { get; set; }

      /// <summary>
      /// Полный путь к файлу экспортированного блока
      /// </summary>      
      public string File { get;  set; }            
            
      /// <summary>
      /// Точка вставки бллока квартиры в Модели.
      /// </summary>      
      public Point3d Position { get;  set; }      

      /// <summary>
      /// Угол поворота блока квартиры.
      /// </summary>
      public double Rotation { get;  set; }

      public List<Parameter> Parameters { get;  set; }

      /// <summary>
      /// Создание блока для экспорта из id
      /// Если id не блока, то Exception
      /// </summary>      
      public Apartment(BlockReference blRef, string blName)
      {
         BlockName = blName;
         IdBlRef = blRef.Id;
         IdBtr = blRef.BlockTableRecord;
         Position = blRef.Position;
         Rotation = blRef.Rotation;
         File = Path.Combine(Path.GetDirectoryName(IdBlRef.Database.Filename), BlockName + ".dwg");
         try
         {
            Extents = blRef.GeometricExtents;
         }
         catch (System.Exception ex)
         {
            Logger.Log.Error(ex, "BlockToExport - blRef.GeometricExtents");
            Inspector.AddError($"Не определены границы блока '{BlockName}' с точкой вставки {Position}",                
               icon: System.Drawing.SystemIcons.Error);
         }

         // Определение модулуй в квартире
         Modules = Module.GetModules(this);
      }

      /// <summary>
      /// Экспорт квартир в XML
      /// </summary>
      public static void ExportToXML(string fileXml, List<Apartment> apartments)
      {
         try
         {
            //AparmentCollection apartCol = new AparmentCollection();
            //apartCol.Apartments = apartments;
            AcadLib.Files.SerializerXml ser = new AcadLib.Files.SerializerXml(fileXml);
            ser.SerializeList(apartments);
         }
         catch (System.Exception ex)
         {
            Inspector.AddError($"Ошибка при экспорте квартир в XML - {ex.Message}");
         }         
      }

      /// <summary>
      /// Экспорт блоков квартир в отдельные файлы dwg квартир.
      /// </summary>      
      /// <returns>Количество экспортированных квартир.</returns>
      public static int ExportToFiles(List<Apartment> apartments)
      {
         int count = 0;
         DateTime now = DateTime.Now;

         // Выключение слоев штриховки
         _layersOff = LayerService.LayersOff(Options.Instance.LayersOffMatch);

         foreach (var apart in apartments)
         {
            try
            {
               apart.ExportToFile();
               apart.ExportDate = now;
               count++;
            }
            catch (System.Exception ex)
            {
               Inspector.AddError($"Ошибка при экспорте блока '{apart.BlockName}' - {ex.Message}", icon: System.Drawing.SystemIcons.Error);
            }
         }

         // Восстановление слоев
         LayerService.LayersOn(_layersOff);

         return count;
      }      

      /// <summary>
      /// Экспорт блока в файл - файл в корне текущего чертежа с именем блока.
      /// Точка вставки блока - 0,0
      /// </summary>      
      public void ExportToFile()
      {
         using (var db = new Database(true, true))
         {
            db.CloseInput(true);

            var ids = new ObjectIdCollection(new[] { IdBlRef });
            var idMS = SymbolUtilityServices.GetBlockModelSpaceId(db);

            using (IdMapping map = new IdMapping())
            {
               db.WblockCloneObjects(ids, idMS, map, DuplicateRecordCloning.Replace, false);
               // перенос блока в ноль            
               var idBlRefMap = map[IdBlRef].Value;
               if (!idBlRefMap.IsNull)
               {
                  using (var blRef = idBlRefMap.Open(OpenMode.ForWrite, false, true) as BlockReference)
                  {
                     blRef.Position = Point3d.Origin;
                  }
                  db.SaveAs(File, DwgVersion.Current);
               }
            }
            //Inspector.AddError($"Экспортирован блок {Name} в файл {File}", IdBlRef, icon: System.Drawing.SystemIcons.Information);
         }
      }

      /// <summary>
      /// Поиск квартир в чертеже.
      /// </summary>      
      public static List<Apartment> GetApartments(Database db)
      {
         List<Apartment> apartments = new List<Apartment>();
         using (var ms = SymbolUtilityServices.GetBlockModelSpaceId(db).Open(OpenMode.ForRead) as BlockTableRecord)
         {
            foreach (ObjectId idEnt in ms)
            {
               using (var blRefApart = idEnt.Open(OpenMode.ForRead, false, true) as BlockReference)
               {
                  if (blRefApart != null)
                  {
                     string blName = blRefApart.GetEffectiveName();
                     if (IsBlockNameApartment(blName))
                     {
                        try
                        {
                           var apartment = new Apartment(blRefApart, blName);
                           apartments.Add(apartment);
                        }
                        catch (System.Exception ex)
                        {
                           Inspector.AddError($"Ошибка считывания блока квартиры {blName} - {ex.Message}.",
                              blRefApart,  icon: SystemIcons.Error);
                        }
                     }
                     else
                     {
                        Inspector.AddError($"Отфильтрован блок квартиры '{blName}', имя не соответствует " +
                           $"'{Options.Instance.BlockApartmentNameMatch}",
                           blRefApart, icon: System.Drawing.SystemIcons.Information);
                     }
                  }
               }
            }
         }
         return apartments;
      }

      /// <summary>
      /// Проверка имени блока квартиры
      /// </summary>      
      public static bool IsBlockNameApartment(string blName)
      {
         return Regex.IsMatch(blName, Options.Instance.BlockApartmentNameMatch, RegexOptions.IgnoreCase);
      }
   }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using AcadLib.Errors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace AR_ExportApartments.Model.ExportApartment
{
   public class BlockApartment
   {
      /// <summary>
      /// Имя бллка
      /// </summary>
      public string Name { get; private set; }

      public ObjectId IdBlRef { get; private set; }      
      public Extents3d Extents { get; set; }

      /// <summary>
      /// Дата экспорта
      /// </summary>
      public DateTime ExportDate { get; set; }

      /// <summary>
      /// Полный путь к файлу экспортированного блока
      /// </summary>
      public string File { get; private set; }

      /// <summary>
      /// Создание блока для экспорта из id
      /// Если id не блока, то Exception
      /// </summary>
      /// <param name="idBlRef"></param>
      public BlockApartment(BlockReference blRef, string blName)
      {         
         Name = blName;
         IdBlRef = blRef.Id;
         File = Path.Combine(Path.GetDirectoryName(IdBlRef.Database.Filename), Name + ".dwg");
         try
         {
            Extents = blRef.GeometricExtents;
         }
         catch (System.Exception ex)
         {
            Logger.Log.Error(ex, "BlockToExport - blRef.GeometricExtents");
            Inspector.AddError($"Не определены границы блока '{Name}' с точкой вставки {blRef.Position}");
         }
      }

      public static int ExportToFiles(List<BlockApartment> blocksToExport)
      {
         int count = 0;
         DateTime now = DateTime.Now;
         foreach (var blToExport in blocksToExport)
         {
            try
            {
               blToExport.Export();
               blToExport.ExportDate = now;
               count++;
            }
            catch (System.Exception ex)
            {
               Inspector.AddError($"Ошибка при экспорте блока '{blToExport.Name}' - {ex.Message}", icon: System.Drawing.SystemIcons.Error);
            }            
         }
         return count;
      }

      /// <summary>
      /// Экспорт блока в файл - файл в корне текущего чертежа с именем блока.
      /// Точка вставки блока - 0,0
      /// </summary>      
      public void Export()
      {
         using (var db = new Database(true, true))
         {
            db.CloseInput(true);

            var ids = new ObjectIdCollection(new[] { IdBlRef });
            var idMS = SymbolUtilityServices.GetBlockModelSpaceId(db);

            IdMapping map = new IdMapping();
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
            //Inspector.AddError($"Экспортирован блок {Name} в файл {File}", IdBlRef, icon: System.Drawing.SystemIcons.Information);
         }
      }

      public static List<BlockApartment> GetBlockApartments(Database db)
      {
         List<BlockApartment> blocksToExport = new List<BlockApartment>();
         using (var t = db.TransactionManager.StartOpenCloseTransaction())
         {
            var ms = t.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db),OpenMode.ForRead) as BlockTableRecord;
            foreach (ObjectId idEnt in ms)
            {
               var blRef = t.GetObject(idEnt,OpenMode.ForRead, false, true) as BlockReference;

               if (blRef != null)
               {
                  string blName = blRef.GetEffectiveName();
                  if (IsNameBlockApartment(blName))
                  {
                     var blExport = new BlockApartment(blRef, blName);
                     blocksToExport.Add(blExport);
                  }
                  else
                  {
                     Inspector.AddError($"Отфильтрован блок '{blName}'", blRef, icon: System.Drawing.SystemIcons.Information);
                  }
               }
            }
            t.Commit();
         }
         return blocksToExport;
      }

      public static bool IsNameBlockApartment(string blName)
      {
         return Regex.IsMatch(blName, Options.Instance.BlockApartmentNameMatch, RegexOptions.IgnoreCase);
      }
   }
}
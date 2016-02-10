using System;
using System.Collections.Generic;
using System.IO;
using AcadLib.Errors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace AR_ExportApartments.Model.ExportBlocks
{
   public class BlockToExport
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
      public BlockToExport(ObjectId idBlRef)
      {
         using (var blRef = idBlRef.Open( OpenMode.ForRead, false, true) as BlockReference)
         {
            if (blRef == null)
            {
               throw new Autodesk.AutoCAD.Runtime.Exception(Autodesk.AutoCAD.Runtime.ErrorStatus.InvalidObjectId, "Это не блок.");
            }       
            Name = blRef.GetEffectiveName();
            IdBlRef = blRef.Id;
            File = Path.Combine(Path.GetDirectoryName(IdBlRef.Database.Filename), Name + ".dwg");
            try
            {
               Extents = blRef.GeometricExtents;
            }
            catch (Exception ex)
            {
               Logger.Log.Error(ex, "BlockToExport - blRef.GeometricExtents");
               Inspector.AddError($"Не определены границы блока '{Name}' с точкой вставки {blRef.Position}");
            }
         }
      }

      public static int ExportToFiles(List<BlockToExport> blocksToExport)
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
            catch (Exception ex)
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

      public static List<BlockToExport> GetBlocksToExport(List<ObjectId> idsBlRef)
      {
         List<BlockToExport> blocksToExport = new List<BlockToExport>();
         foreach (var idBlRef in idsBlRef)
         {
            try
            {
               var blExport = new BlockToExport(idBlRef);
               blocksToExport.Add(blExport);
            }
            catch (System.Exception ex)
            {
               Inspector.AddError($"{ex.Message}");
            }
         }
         return blocksToExport;
      }
   }
}
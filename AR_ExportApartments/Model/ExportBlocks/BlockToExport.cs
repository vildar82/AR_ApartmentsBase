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
         }
      }

      /// <summary>
      /// Экспорт блока в файл - файл в корне текущего чертежа с именем блока.
      /// Точка вставки блока - 0,0
      /// </summary>      
      public void Export()
      {
         string file = Path.Combine(Path.GetDirectoryName(IdBlRef.Database.Filename), Name + ".dwg");

         using (var db = new Database(true, true))
         {
            db.CloseInput(true);

            var ids = new ObjectIdCollection(new[] { IdBlRef });
            var idMS = SymbolUtilityServices.GetBlockModelSpaceId(db);

            IdMapping map = new IdMapping();
            db.WblockCloneObjects(ids, idMS, map, DuplicateRecordCloning.Replace, false);

            // перенос блока в ноль
            using (var blRef = map[IdBlRef].Value.Open(OpenMode.ForWrite, false, true) as BlockReference)
            {
               blRef.Position = Point3d.Origin;
            }
            db.SaveAs(file, DwgVersion.Current);

            Inspector.AddError($"Экспортирован блок {Name} в файл {file}", IdBlRef, icon: System.Drawing.SystemIcons.Information);
         }
      }
   }
}
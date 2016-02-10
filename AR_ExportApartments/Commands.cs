using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcadLib.Errors;
using AR_ApartmentExport.Model.ExportBlocks;
using AR_ExportApartments.Model.ExportApartment;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

[assembly: CommandClass(typeof(AR_ExportApartments.Commands))]

namespace AR_ExportApartments
{
   public class Commands
   {
      [CommandMethod("PIK", "AR-ExportApartments", CommandFlags.Modal | CommandFlags.NoPaperSpace | CommandFlags.NoBlockEditor)]
      public void ExportApartments()
      {
         Logger.Log.Info("Start command AR-ExportApartments");
         Document doc = Application.DocumentManager.MdiActiveDocument;
         if (doc == null) return;

         try
         {
            Inspector.Clear();

            Database db = doc.Database;
            Editor ed = doc.Editor;

            // Считывание выбранных блоков
            var blocksApartment = BlockApartment.GetBlockApartments(db);
            if (blocksApartment.Count == 0)
            {
               throw new System.Exception($"Блоки квартир не найдены. Имя блока квартиры должно соответствовать условию Match = '{Options.Instance.BlockApartmentNameMatch}'");
            }

            ed.WriteMessage($"\nВ Модели найдено {blocksApartment.Count} блоков квартир. ");

            // Форма предпросмотра экспорта блоков
            FormBlocksExport formExport = new FormBlocksExport(blocksApartment);
            if (Application.ShowModalDialog(formExport) == System.Windows.Forms.DialogResult.OK)
            {
               // Экспорт блоков в файлы
               var count = BlockApartment.ExportToFiles(blocksApartment);

               ed.WriteMessage($"\nЭкспортированно {count} блоков.");

               // Запись лога экспортированных блоков      
               string logFile = Path.Combine(Path.GetDirectoryName(doc.Name), Options.Instance.LogFileName);
               ExcelLog excelLog = new ExcelLog(logFile);
               try
               {
                  excelLog.AddtoLog(blocksApartment);
               }
               catch (System.Exception ex)
               {
                  Inspector.AddError($"Ошибка записи экспорта в лог файл {logFile} - {ex.Message}");
               }

               if (Inspector.HasErrors)
               {
                  Inspector.Show();
               }
            }
         }
         catch (System.Exception ex)
         {
            doc.Editor.WriteMessage($"\nОшибка экспорта блоков: {ex.Message}");
            if (!ex.Message.Contains("Отменено пользователем"))
            {
               Logger.Log.Error(ex, $"Command: AR-ExportApartments. {doc.Name}");
            }
         }         
      }
   }
}

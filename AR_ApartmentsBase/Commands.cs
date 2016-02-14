using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AcadLib.Errors;
using AR_ApartmentBase.Model;
using AR_ApartmentBase.Model.DB;
using AR_ApartmentBase.Model.Export;
using AR_ApartmentBase.Model.Revit;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

[assembly: CommandClass(typeof(AR_ApartmentBase.Commands))]

namespace AR_ApartmentBase
{
   public class Commands
   {
      [CommandMethod("PIK", "AR-ExportApartmentsAbout", CommandFlags.Modal)]
      public void ExportApartmentsAbout()
      {
         Logger.Log.Info("Start command AR-ExportApartmentsAbout");
         Document doc = Application.DocumentManager.MdiActiveDocument;         
         if (doc == null) return;

         Editor ed = doc.Editor;

         ed.WriteMessage($"\nПрограмма экспорта блоков квартир. Версия {Assembly.GetExecutingAssembly().GetName().Version}");
         ed.WriteMessage("\nКоманды:");
         ed.WriteMessage("\nAR-ExportApartments - экспорт блоков квартир найденных в Модели в отдельные файлы и в базу.\n" +
                          $"Имя блока квартиры должно соответствовать {Options.Instance.BlockApartmentNameMatch}");
         ed.WriteMessage("\nAR-ExportApartmentsOptions - настройки программы.");
         ed.WriteMessage("\nAR-ExportApartmentsAbout - описание программы.");
      }

      [CommandMethod("PIK", "AR-ExportApartmentsOptions", CommandFlags.Modal)]
      public void ExportApartmentsOptions()
      {
         Logger.Log.Info("Start command AR-ExportApartmentsOptions");
         Document doc = Application.DocumentManager.MdiActiveDocument;
         if (doc == null) return;

         Options.Show();
      }

      /// <summary>
      /// Поиск квартир в Модели и экспорт в отдельные файлы каждой квартиры, и експорт в базу.
      /// </summary>
      [CommandMethod("PIK", "AR-ExportApartments", CommandFlags.Modal | CommandFlags.NoPaperSpace | CommandFlags.NoBlockEditor)]
      public void ExportApartments()
      {
         Logger.Log.Info("Start command AR-ExportApartments");
         Document doc = Application.DocumentManager.MdiActiveDocument;
         if (doc == null) return;

         if (!File.Exists(doc.Name))
         {
            doc.Editor.WriteMessage("\nНужно сохранить текущий чертеж.");
            return;
         }

         try
         {
            Inspector.Clear();

            Database db = doc.Database;
            Editor ed = doc.Editor;

            ExportApartmentsAbout();

            // Считывание блоков квартир.
            var apartments = Apartment.GetApartments(db);
            if (apartments.Count == 0)
            {
               throw new System.Exception($"Блоки квартир не найдены. Имя блока квартиры должно соответствовать условию Match = '{Options.Instance.BlockApartmentNameMatch}'");
            }
            ed.WriteMessage($"\nВ Модели найдено {apartments.Count} блоков квартир.");

            // Форма предпросмотра экспорта блоков
            FormBlocksExport formExport = new FormBlocksExport(apartments);
            if (Application.ShowModalDialog(formExport) == System.Windows.Forms.DialogResult.OK)
            {
               // Экспорт блоков в файлы
               var count = Apartment.ExportToFiles(apartments);
               ed.WriteMessage($"\nЭкспортированно {count} квартиры.");

               // Запись в DB
               ExportDB exportDb = new ExportDB();
               exportDb.Export(apartments);

               // Запись квартир в xml
               string fileXml = Path.Combine(Path.GetDirectoryName(doc.Name), Path.GetFileNameWithoutExtension(doc.Name) + ".xml");
               Apartment.ExportToXML(fileXml, apartments);               

               // Запись лога экспортированных блоков      
               string logFile = Path.Combine(Path.GetDirectoryName(doc.Name), Options.Instance.LogFileName);
               ExcelLog excelLog = new ExcelLog(logFile);
               excelLog.AddtoLog(apartments);

               // Показ ошибок
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

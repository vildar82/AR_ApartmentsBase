using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AcadLib.Errors;
using AR_ApartmentBase.Model;
using AR_ApartmentBase.Model.DB.EntityModel;
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
      [CommandMethod("PIK", "AR-BaseApartmentsAbout", CommandFlags.Modal)]
      public void ExportApartmentsAbout()
      {
         Logger.Log.Info("Start command AR-BaseApartmentsAbout");
         Document doc = Application.DocumentManager.MdiActiveDocument;
         if (doc == null) return;

         Editor ed = doc.Editor;

         ed.WriteMessage($"\nПрограмма экспорта блоков квартир. Версия {Assembly.GetExecutingAssembly().GetName().Version}");
         ed.WriteMessage("\nКоманды:");
         ed.WriteMessage("\nAR-BaseApartmentsExport - экспорт блоков квартир найденных в Модели в отдельные файлы и в базу.\n" +
                          $"Имя блока квартиры должно соответствовать {Options.Instance.BlockApartmentNameMatch}");
         ed.WriteMessage("\nAR-BaseApartmentsOptions - настройки программы.");
         ed.WriteMessage("\nAR-BaseApartmentsAbout - описание программы.");
      }

      [CommandMethod("PIK", "AR-BaseApartmentsOptions", CommandFlags.Modal)]
      public void ExportApartmentsOptions()
      {
         Logger.Log.Info("Start command AR-BaseApartmentsOptions");
         Document doc = Application.DocumentManager.MdiActiveDocument;
         if (doc == null) return;

         Options.Show();
      }

      /// <summary>
      /// Поиск квартир в Модели и экспорт в отдельные файлы каждой квартиры, и експорт в базу.
      /// </summary>
      [CommandMethod("PIK", "AR-BaseApartmentsExport", CommandFlags.Modal | CommandFlags.NoPaperSpace | CommandFlags.NoBlockEditor)]
      public void BaseApartmentsExport()
      {
         Logger.Log.Info("Start command AR-BaseApartmentsExport");
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

            // Квартиры в базе
            var apartmentsInBase = GetBaseApartments.GetAll();            

            //Проверка всех элементов квартир в базе - категории, параметры.
            CheckApartments.Check(apartments, apartmentsInBase);

            // Форма предпросмотра экспорта блоков
            FormBlocksExport formExport = new FormBlocksExport(apartments);
            if (Application.ShowModalDialog(formExport) == System.Windows.Forms.DialogResult.OK)
            {
               // Экспорт блоков в файлы
               var count = Apartment.ExportToFiles(apartments);
               ed.WriteMessage($"\nЭкспортированно {count} квартир в отдельные файлы.");

               // Экспорт только блоков квартир без ошибок, которых нет в базе или они изменились
               var apartmentsToExport = apartments.Where(a => 
                                 !a.BaseStatus.HasFlag(EnumBaseStatus.Error) &&
                                 !a.BaseStatus.HasFlag(EnumBaseStatus.NotInDwg)).ToList();


               //// Запись квартир в xml
               //string fileXml = Path.Combine(Path.GetDirectoryName(doc.Name), Path.GetFileNameWithoutExtension(doc.Name) + ".xml");               
               //Apartment.ExportToXML(fileXml, apartmentsToExport);               

               // Запись в DB    
               // Для димана пока без записи в БД           
               try
               {
                  BaseApartments.Export(apartmentsToExport);
               }
               catch (System.Exception ex)
               {
                  Inspector.AddError($"Ошибка экспорта в БД - {ex.Message}", icon: System.Drawing.SystemIcons.Error);
               }

               // Запись лога экспортированных блоков      
               string logFile = Path.Combine(Path.GetDirectoryName(doc.Name), Options.Instance.LogFileName);
               ExcelLog excelLog = new ExcelLog(logFile);
               excelLog.AddtoLog(apartmentsToExport);

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
               Logger.Log.Error(ex, $"Command: AR-BaseApartmentsExport. {doc.Name}");
            }
         }
      }

      /// <summary>
      /// Очистка базы квартир.
      /// </summary>
      [CommandMethod("PIK", "AR-BaseApartmentsClear", CommandFlags.Modal | CommandFlags.NoPaperSpace | CommandFlags.NoBlockEditor)]
      public void BaseApartmentsClear()
      {
         Logger.Log.Info("Start command AR-BaseApartmentsClear");
         Document doc = Application.DocumentManager.MdiActiveDocument;
         if (doc == null) return;

         try
         {            
            BaseApartments.Clear();
         }
         catch (System.Exception ex)
         {
            doc.Editor.WriteMessage($"\nОшибка очистки базы: {ex.Message}");
            if (!ex.Message.Contains("Отменено пользователем"))
            {
               Logger.Log.Error(ex, $"Command: AR-BaseApartmentsClear. {doc.Name}");
            }
         }
      }
   }
}        
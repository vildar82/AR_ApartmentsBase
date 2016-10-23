using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcadLib.Errors;
using AR_ApartmentBase.Model;
using OfficeOpenXml;

namespace AR_ApartmentBase_AutoCAD.Export
{
   public class ExcelLog
   {
      private string logFile;

      public ExcelLog(string fileLog)
      {
         this.logFile = fileLog;
      }

      public void AddtoLog(List<Apartment> apartments)
      {
         try
         {
            if (!File.Exists(logFile))
            {
               createExcel();
            }

            using (var xlPackage = new ExcelPackage(new FileInfo(logFile)))
            {
               var worksheet = xlPackage.Workbook.Worksheets["ЭкспортБлоковКвартир"];
               int row = 2;

               while (worksheet.Cells[row, 1].Text != "")
                  row++;

               foreach (var apart in apartments)
               {
                  var apartAC = (ApartmentAC)apart;
                  if (apartAC.ExportDate > DateTime.MinValue)
                  {
                     worksheet.Cells[row, 1].Value = apartAC.ExportDate;//"Дата"
                     worksheet.Cells[row, 2].Value = apartAC.Name;//"Блок"
                     worksheet.Cells[row, 3].Value = apartAC.File;//"файл"
                     worksheet.Cells[row, 4].Value = Environment.UserName;//"Пользователь"               
                     row++;
                  }
               }
               xlPackage.Save();
            }
         }
         catch (Exception ex)
         {
            Inspector.AddError($"Ошибка записи экспорта квартир в лог файл {logFile} - {ex.Message}");
         }
      }      

      private void createExcel()
      {
         using (var xlPackage = new ExcelPackage(new FileInfo(logFile)))
         {
            var worksheet = xlPackage.Workbook.Worksheets.Add("ЭкспортБлоковКвартир");            
            worksheet.Cells[1, 1].Value = "Дата";
            worksheet.Cells[1, 2].Value = "Блок";
            worksheet.Cells[1, 3].Value = "Файл";
            worksheet.Cells[1, 4].Value = "Пользователь";
            worksheet.Column(1).Style.Numberformat.Format = "yyyy-mm-dd h:mm";            
            xlPackage.Save();
         }
      }
   }
}

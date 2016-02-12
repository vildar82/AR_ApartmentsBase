using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;
using AcadLib.Files;
using Autodesk.AutoCAD.ApplicationServices;

namespace AR_ApartmentBase
{
   [Serializable]
   public class Options
   {
      private static readonly string fileOptions = Path.Combine(
                     AutoCAD_PIK_Manager.Settings.PikSettings.ServerShareSettingsFolder,
                     "АР\\ApartmentBase\\AR_ExportApartment_Options.xml");
      private static Options _instance;
      public static Options Instance
      {
         get
         {
            if (_instance == null)
            {
               _instance = Load();
            }
            return _instance;
         }
      }

      private Options() {}

      /// <summary>
      /// Имя файла лога в Excel
      /// </summary>
      public string LogFileName { get; set; }= "AR_ExportApartment_Log.xlsx";

      /// <summary>
      /// Фильтр для блоков квартир
      /// Имя блока начинается с RV_FL или RV_MD
      /// </summary>
      public string BlockApartmentNameMatch { get; set; } = "^(RV_FL|RV_MD)";

      /// <summary>
      /// Фильтр для блоков модулей
      /// Имя блока начинается с "RV_EL" и в имени блока есть слово "модуль".
      /// </summary>
      public string BlockModuleNameMatch { get; set; } = "^(RV_EL).*модуль";

      /// <summary>
      /// Отключяаемые слои в файлах эксопрта квартир
      /// </summary>
      public string LayersOffMatch { get; set; } = "штриховк";

      /// <summary>
      /// Пропускаемые параметры в блоках (динамических свойств или атрибутов)
      /// </summary>
      public List<string> IgnoreParamNames { get; set; } = new List<string> { "origin" };


      public static Options Load()
      {
         Options options = null;
         // загрузка из файла настроек
         if (File.Exists(fileOptions))
         {
            SerializerXml xmlSer = new SerializerXml(fileOptions);
            try
            {
               options = xmlSer.DeserializeXmlFile<Options>();
               if (options != null)
               {  
                  return options;
               }
            }
            catch (Exception ex)
            {
               Logger.Log.Error(ex, $"Не удалось десериализовать настройки из файла {fileOptions}");
            }
         }
         options = new Options();
         options.Save();
         return options;
      }     

      public void Save()
      {
         try
         {
            if (!File.Exists(fileOptions))
            {
               Directory.CreateDirectory(Path.GetDirectoryName(fileOptions));
            }
            SerializerXml xmlSer = new SerializerXml(fileOptions);
            xmlSer.SerializeList(this);            
         }
         catch (Exception ex)
         {
            Logger.Log.Error(ex, $"Не удалось сериализовать настройки в {fileOptions}");
         }
      }      

      //private static Options DefaultOptions()
      //{
      //   Options options = new Options();

      //   options.LogFileName = "AR_ExportApartment_Log.xlsx";
      //   options.BlockApartmentNameMatch = "квартира";

      //   return options;
      //}      
   }
}
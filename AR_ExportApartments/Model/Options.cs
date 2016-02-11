using System;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;
using AcadLib.Files;
using Autodesk.AutoCAD.ApplicationServices;

namespace AR_ExportApartments
{
   [Serializable]
   public class Options
   {
      private static readonly string fileOptions = Path.Combine(
                     AutoCAD_PIK_Manager.Settings.PikSettings.ServerShareSettingsFolder,
                     "АР\\ExportApartment\\AR_ExportApartment_Options.xml");
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

      public string LogFileName { get; set; }= "AR_ExportApartment_Log.xlsx";
      public string BlockApartmentNameMatch { get; set; } = "^(RV_FL|RV_MD)";
      public string LayersOffMatch { get; set; } = "штриховк";

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
               Logger.Log.Error(ex, "Не удалось десериализовать настройки из файла {0}", fileOptions);
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
            Logger.Log.Error(ex, "Не удалось сериализовать настройки в {0}", fileOptions);
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
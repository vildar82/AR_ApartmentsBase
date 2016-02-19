using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;
using AcadLib.Files;
using Autodesk.AutoCAD.ApplicationServices;

namespace AR_ApartmentBase.Model
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

      private Options() { }

      /// <summary>
      /// Имя файла лога в Excel
      /// </summary>
      [Description("Имя Excel-файла лога экспорта квартир - файл располагается в корневой папке файла из которого выполняется экспорт квартир.")]      
      [DefaultValue("AR_ExportApartment_Log.xlsx")]
      public string LogFileName { get; set; } = "AR_ExportApartment_Log.xlsx";

      /// <summary>
      /// Фильтр для блоков квартир
      /// Имя блока начинается с RV_FL или RV_MD
      /// </summary>
      [Description("Паттерн для фильтра блоков квартир. ^(RV_FL|RV_MD) - означает, что имя блока начинающееся на RV_FL или RV_MD - это блок квартиры")]
      [DefaultValue("^(RV_FL|RV_MD)")]
      public string BlockApartmentNameMatch { get; set; } = "^(RV_FL|RV_MD)";

      /// <summary>
      /// Фильтр для блоков модулей
      /// Имя блока начинается с "RV_EL" и в имени блока есть слово "модуль".
      /// </summary>
      [Description("Паттерн для блоков модулей. ^(RV_EL).*модуль - имя блока начинается на RV_EL и в имени блока есть слово модуль.")]
      [DefaultValue("^(RV_EL).*модуль")]
      public string BlockModuleNameMatch { get; set; } = "^(RV_EL).*модуль";

      /// <summary>
      /// Фильтр для блоков элемнтов
      /// Имя блока начинается с "RV_EL_BS".
      /// </summary>
      [Description("Паттерн для блоков элементов. ^RV_EL_BS - имя блока начинается на RV_EL_BS.")]
      [DefaultValue("^RV_EL_BS")]
      public string BlockElementNameMatch { get; set; } = "^RV_EL_BS";

      /// <summary>
      /// Отключяаемые слои в файлах эксопрта квартир
      /// </summary>
      [Description("Отключаемые слои при экспорте квартир. Паттерн соответствия имени слоя. штриховк - если в имени слоя есть слово штриховк, то этот слой будет отключен в файле экспортированной квартиры.")]
      [DefaultValue("штриховк")]
      public string LayersOffMatch { get; set; } = "штриховк";

      /// <summary>
      /// Пропускаемые параметры в блоках (динамических свойств или атрибутов)
      /// </summary>
      [Description("Игнорируемые имена свойств блоков элементов. origin - служебное свойство.")]
      [DefaultValue(new string[] { "origin" })]
      public string[] IgnoreParamNames { get; set; } = new string[] { "origin" };

      /// <summary>
      /// Имя параметра для имени семейства в блоках элементов в автокаде
      /// </summary>
      [DefaultValue("FamilyName")]
      [Description("Имя параметра для имени семейства в блоках элементов в автокаде")]
      public string ParameterFamilyName { get; set; } = "FamilyName";

      /// <summary>
      /// Имя параметра для типоразмера семейства в блоках элементов в автокаде
      /// </summary>
      [DefaultValue("FamilySymbolName")]
      [Description("Имя параметра для типоразмера семейства в блоках элементов в автокаде")]
      public string ParameterFamilySymbolName { get; set; } = "FamilySymbolName";

      /// <summary>
      /// Имя параметра для категории элемента в блоке автокада.
      /// </summary>
      [DefaultValue("Категория")]
      [Description("Имя параметра для категории элемента в блоке автокада.")]
      public string ParameterCategoryName { get; set; } = "Категория";

      /// <summary>
      /// Имя параметра для имени модуля
      /// </summary>
      [DefaultValue("Тип")]
      [Description("Имя параметра для имени модуля.")]
      public string ParameterModuleName { get; set; } = "Тип";

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

      public static void Show()
      {
         FormOptions formOpt = new FormOptions((Options)Instance.MemberwiseClone());
         if (Application.ShowModalDialog(formOpt) == System.Windows.Forms.DialogResult.OK)
         {
            _instance = formOpt.Options;
            _instance.Save();
         }
      }
   }
}
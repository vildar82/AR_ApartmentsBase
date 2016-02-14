using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AR_ApartmentBase.Model.Revit;
using AR_ApartmentBase.Model.Revit.Elements;

namespace AR_ApartmentBase.Model.DB
{
   /// <summary>
   /// Экспорт квартир в базу.
   /// </summary>
   public class ExportDB
   {
      public ExportDB()
      {         
      }

      public void Export(List<Apartment> apartments)
      {
         DataSet ds = new DataSet();

         // заполнение справочников 
         fillDataset(ds);

         foreach (Apartment apart in apartments)
         {
            // Квартира
            var flatRow = ds.F_R_Flats.NewF_R_FlatsRow();
            flatRow.WORKNAME = apart.BlockName;
            flatRow.REVISION = 0;
            flatRow.COMMERCIAL_NAME = "";
            ds.F_R_Flats.AddF_R_FlatsRow(flatRow);

            // Модули
            foreach (var module in apart.Modules)
            {
               var moduleRow = getModuleRow(module, ds);
               var flatModuleRow = ds.F_nn_FlatModules.NewF_nn_FlatModulesRow();
               flatModuleRow.ID_FLAT = flatRow.ID_FLAT;
               flatModuleRow.ID_MODULE = moduleRow.ID_MODULE;

               // Элементы
               foreach (var elem in module.Elements)
               {
                  // Определение элемента в базе
                  var dbElem = getElement(elem, ds);                  
                  // Привязка элемента к модулю                  
               }               
            }            
         }
      }

      private void fillDataset(DataSet ds)
      {
         // Добавление категории - Стены
         var catWall = ds.F_S_Categories.AddF_S_CategoriesRow("Стены", "Wall");
         // Добавление параметров
         var parLP = ds.F_S_Parameters.AddF_S_ParametersRow("LocationPoint", "Point");
         var parDir = ds.F_S_Parameters.AddF_S_ParametersRow("Direction", "Point");
         var parLevel= ds.F_S_Parameters.AddF_S_ParametersRow("Level", "String");
         var parLen = ds.F_S_Parameters.AddF_S_ParametersRow("Length", "Int");
         var parH= ds.F_S_Parameters.AddF_S_ParametersRow("Height", "Int");
         var parFN = ds.F_S_Parameters.AddF_S_ParametersRow("FamilyName", "String");
         var parFSN = ds.F_S_Parameters.AddF_S_ParametersRow("FamilySymbolName", "String");

         // Параметры стены
         foreach (var paramRow in ds.F_S_Parameters)
         {
            ds.F_nn_Category_Parameters.AddF_nn_Category_ParametersRow(catWall.ID_CATEGORY, parLP);
            ds.F_nn_Category_Parameters.AddF_nn_Category_ParametersRow(catWall.ID_CATEGORY, parDir);
            ds.F_nn_Category_Parameters.AddF_nn_Category_ParametersRow(catWall.ID_CATEGORY, parLevel);
            ds.F_nn_Category_Parameters.AddF_nn_Category_ParametersRow(catWall.ID_CATEGORY, parLen);
            ds.F_nn_Category_Parameters.AddF_nn_Category_ParametersRow(catWall.ID_CATEGORY, parH);
            ds.F_nn_Category_Parameters.AddF_nn_Category_ParametersRow(catWall.ID_CATEGORY, parFN);
            ds.F_nn_Category_Parameters.AddF_nn_Category_ParametersRow(catWall.ID_CATEGORY, parFSN);
         }
      }

      private DataSet.F_R_ModulesRow getModuleRow(Module module, DataSet ds)
      {
         var moduleRow = ds.F_R_Modules.FirstOrDefault(m => m.NAME_MODULE.Equals(module.BlockName));
         if (moduleRow == null)
         {
            moduleRow = ds.F_R_Modules.NewF_R_ModulesRow();
            moduleRow.LOCATION_POINT = DbServices.TypeConverter.Point(module.Position);
            moduleRow.NAME_MODULE = module.BlockName;
            ds.F_R_Modules.AddF_R_ModulesRow(moduleRow);
         }
         return moduleRow;
      }

      /// <summary>
      /// Получение или создание строки таблицы Элементов по блоку Елемента
      /// </summary>      
      private DataSet.F_S_ElementsRow getElement(Element elem, DataSet ds)
      {
         var category = getCategory(elem, ds);
         var famInfo = getFamilyInfo(elem, ds);
         var elemDb = ds.F_S_Elements.SingleOrDefault(e => e.F_S_CategoriesRow == category &&
                                     e.F_S_FamilyInfosRow == famInfo);
         if (elemDb == null)
         {
            // Создание новой записи элемента
            elemDb = ds.F_S_Elements.AddF_S_ElementsRow(famInfo, category);
         }
         return elemDb;
      }      

      /// <summary>
      /// Определение категории блока элемента
      /// </summary>      
      private DataSet.F_S_CategoriesRow getCategory(Element elem, DataSet ds)
      {
         return ds.F_S_Categories.Single(c => c.NAME_RUS_CATEGORY.Equals(elem.TypeElement, StringComparison.OrdinalIgnoreCase));         
      }

      /// <summary>
      /// Получениа или создания семейства
      /// </summary>      
      private DataSet.F_S_FamilyInfosRow getFamilyInfo(Element elem, DataSet ds)
      {
         var famInfo = ds.F_S_FamilyInfos.SingleOrDefault(f =>
                                    f.FAMILY_NAME.Equals(elem.FamilyName) &&
                                    f.FAMILY_SYMBOL.Equals(elem.FamilySymbolName)
                                 );
         if (famInfo == null)
         {
            // Добавление записи семейства
            famInfo = ds.F_S_FamilyInfos.AddF_S_FamilyInfosRow(elem.FamilyName.Value, elem.FamilySymbolName.Value);
         }
         return famInfo;
      }
   }
}

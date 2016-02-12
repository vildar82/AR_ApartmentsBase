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
                  var dbElem = getElement(elem, ds);                  
               }               
            }            
         }
      }

      private void fillDataset(DataSet ds)
      {
         // Добавление категории - Стены
         var catWall = ds.F_S_Categories.AddF_S_CategoriesRow("Стены", "Wall");
         // Добавление параметров
         ds.F_S_Parameters.AddF_S_ParametersRow("LocationPoint", "Point");
         ds.F_S_Parameters.AddF_S_ParametersRow("Direction", "Point");
         ds.F_S_Parameters.AddF_S_ParametersRow("Level", "String");
         ds.F_S_Parameters.AddF_S_ParametersRow("Length", "Int");
         ds.F_S_Parameters.AddF_S_ParametersRow("Height", "Int");
         ds.F_S_Parameters.AddF_S_ParametersRow("FamilyName", "String");
         ds.F_S_Parameters.AddF_S_ParametersRow("FamilySymbolName", "String");

         // Параметры стены
         foreach (var paramRow in ds.F_S_Parameters)
         {
            ds.F_nn_Category_Parameters.AddF_nn_Category_ParametersRow();
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

      private DataSet.F_S_ElementsRow getElement(Element elem, DataSet ds)
      {
         var dbCategory = getCategory(elem, ds);
      }

      private object getCategory(Element elem, DataSet ds)
      {
         ds.F_S_Categories
      }
   }
}

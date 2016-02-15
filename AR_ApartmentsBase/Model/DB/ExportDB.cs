using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcadLib.Errors;
using AR_ApartmentBase.Model.DB.DataSetTableAdapters;
using AR_ApartmentBase.Model.DB.DbServices;
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
         TableAdapterManager manager = new TableAdapterManager();
         manager.UpdateOrder = TableAdapterManager.UpdateOrderOption.InsertUpdateDelete;

         fillDataset(ds, manager);         

         bool hasError = false;
         foreach (Apartment apart in apartments)
         {            
            try
            {
               // Квартира
               DataSet.F_R_FlatsRow flatRow = getFlat(ds, apart);

               // Модули
               foreach (var module in apart.Modules)
               {
                  var moduleRow = getModuleRow(module, ds);
                  var flatModuleRow = getFlatModuleRow(ds, flatRow, moduleRow, module);

                  // Элементы                  
                  foreach (var elem in module.Elements)
                  {
                     // Определение элемента в базе                                          
                     DataSet.F_S_ElementsRow elemRow = getElement(elem, ds);
                     // Привязка элемента к модулю  
                     var elemFMRow = getElemInFM(ds, flatModuleRow, elemRow, elem);

                     // Заполнение параметров элемента
                     setElemParams(ds, elemFMRow, elem);
                  }
               }
            }
            catch (Exception ex)
            {
               Inspector.AddError(ex.Message, icon: System.Drawing.SystemIcons.Error);
            }
         }

         if (!hasError)
         {
            // Обновление базы            
            var dsChanges = (DataSet)ds.GetChanges();
            
            //var conString = manager.Connection.ConnectionString;                          
            
            manager.UpdateAll(dsChanges);
            dsChanges.AcceptChanges();
            //1.Построение и заполнение каждого объекта DataTable класса DataSet данными из источника с помощью класса DataAdapter.
            //2.Изменение данных в отдельных объектах DataTable путем добавления, обновления или удаления объектов DataRow.
            //3.Вызов метода GetChanges для создания второго класса DataSet, отображающего только изменения данных.
            //4.Вызов метода Update класса DataAdapter путем передачи второго класса DataSet в качестве аргумента.
            //5.Вызов метода Merge для объединения изменений из второго класса DataSet с данными первого.
            //6.Вызов метода AcceptChanges для класса DataSet. В противном случае можно вызвать метод RejectChanges, чтобы отменить изменения.
         }
      }      

      private void fillDataset(DataSet ds, TableAdapterManager manager)
      {
         // Представление - параметры
         CategoryParametersTableAdapter adapterCatParamView = new CategoryParametersTableAdapter();
         adapterCatParamView.Fill(ds.CategoryParameters);         

         // Квартиры
         manager.F_R_FlatsTableAdapter = new F_R_FlatsTableAdapter();
         manager.F_R_FlatsTableAdapter.Fill(ds.F_R_Flats);

         // Модули
         manager.F_R_ModulesTableAdapter = new F_R_ModulesTableAdapter();
         manager.F_R_ModulesTableAdapter.Fill(ds.F_R_Modules);

         // Квартиры-Модули
         manager.F_nn_FlatModulesTableAdapter = new F_nn_FlatModulesTableAdapter();
         manager.F_nn_FlatModulesTableAdapter.Fill(ds.F_nn_FlatModules);

         // Семейства
         manager.F_S_FamilyInfosTableAdapter  = new F_S_FamilyInfosTableAdapter();
         manager.F_S_FamilyInfosTableAdapter.Fill(ds.F_S_FamilyInfos);

         // Елементы в модуле в квартире
         manager.F_nn_Elements_FlatModulesTableAdapter = new F_nn_Elements_FlatModulesTableAdapter();
         manager.F_nn_Elements_FlatModulesTableAdapter.Fill(ds.F_nn_Elements_FlatModules);

         // Елементы
         manager.F_S_ElementsTableAdapter  = new F_S_ElementsTableAdapter();
         manager.F_S_ElementsTableAdapter.Fill(ds.F_S_Elements);

         // Категории
         manager.F_S_CategoriesTableAdapter  = new F_S_CategoriesTableAdapter();
         manager.F_S_CategoriesTableAdapter.Fill(ds.F_S_Categories);

         // Параметры категории
         manager.F_nn_Category_ParametersTableAdapter  = new F_nn_Category_ParametersTableAdapter();
         manager.F_nn_Category_ParametersTableAdapter.Fill(ds.F_nn_Category_Parameters);

         // Параметры и значения
         manager.F_nn_ElementParam_ValueTableAdapter  = new F_nn_ElementParam_ValueTableAdapter();
         manager.F_nn_ElementParam_ValueTableAdapter.Fill(ds.F_nn_ElementParam_Value);

         // Параметры
         manager.F_S_ParametersTableAdapter = new F_S_ParametersTableAdapter();
         manager.F_S_ParametersTableAdapter.Fill(ds.F_S_Parameters);

         //// Добавление категории - Стены
         //var catWall = ds.F_S_Categories.AddF_S_CategoriesRow("Стены", "Wall");
         //// Добавление параметров
         //var parLP = ds.F_S_Parameters.AddF_S_ParametersRow("LocationPoint", "Point");
         //var parDir = ds.F_S_Parameters.AddF_S_ParametersRow("Direction", "Point");
         //var parLevel= ds.F_S_Parameters.AddF_S_ParametersRow("Level", "String");
         //var parLen = ds.F_S_Parameters.AddF_S_ParametersRow("Length", "Int");
         //var parH= ds.F_S_Parameters.AddF_S_ParametersRow("Height", "Int");
         //var parFN = ds.F_S_Parameters.AddF_S_ParametersRow("FamilyName", "String");
         //var parFSN = ds.F_S_Parameters.AddF_S_ParametersRow("FamilySymbolName", "String");

         //// Параметры стены
         //foreach (var paramRow in ds.F_S_Parameters)
         //{
         //   ds.F_nn_Category_Parameters.AddF_nn_Category_ParametersRow(catWall.ID_CATEGORY, parLP);
         //   ds.F_nn_Category_Parameters.AddF_nn_Category_ParametersRow(catWall.ID_CATEGORY, parDir);
         //   ds.F_nn_Category_Parameters.AddF_nn_Category_ParametersRow(catWall.ID_CATEGORY, parLevel);
         //   ds.F_nn_Category_Parameters.AddF_nn_Category_ParametersRow(catWall.ID_CATEGORY, parLen);
         //   ds.F_nn_Category_Parameters.AddF_nn_Category_ParametersRow(catWall.ID_CATEGORY, parH);
         //   ds.F_nn_Category_Parameters.AddF_nn_Category_ParametersRow(catWall.ID_CATEGORY, parFN);
         //   ds.F_nn_Category_Parameters.AddF_nn_Category_ParametersRow(catWall.ID_CATEGORY, parFSN);
         //}
      }

      private static DataSet.F_R_FlatsRow getFlat(DataSet ds, Apartment apart)
      {
         var flatRow = ds.F_R_Flats.SingleOrDefault(f => f.WORKNAME.Equals(apart.BlockName, StringComparison.OrdinalIgnoreCase));
         if (flatRow == null)
         {
            flatRow = ds.F_R_Flats.AddF_R_FlatsRow("", apart.BlockName);            
         }         
         return flatRow;
      }

      private DataSet.F_R_ModulesRow getModuleRow(Module module, DataSet ds)
      {
         var moduleRow = ds.F_R_Modules.SingleOrDefault(m => m.NAME_MODULE.Equals(module.BlockName, StringComparison.OrdinalIgnoreCase));
         if (moduleRow == null)
         {
            moduleRow = ds.F_R_Modules.AddF_R_ModulesRow(module.BlockName);            
         }
         return moduleRow;
      }

      private static DataSet.F_nn_FlatModulesRow getFlatModuleRow(DataSet ds, 
            DataSet.F_R_FlatsRow flatRow, DataSet.F_R_ModulesRow moduleRow, Module module)
      {
         var flatModuleRow = ds.F_nn_FlatModules.SingleOrDefault(fm => 
                                    fm.F_R_FlatsRow == flatRow &&
                                    fm.F_R_ModulesRow == moduleRow &&
                                    fm.LOCATION.Equals(module.LocationPoint, StringComparison.OrdinalIgnoreCase) &&
                                    fm.DIRECTION.Equals(module.Direction, StringComparison.OrdinalIgnoreCase));
         if (flatModuleRow == null)
         {
            flatModuleRow = ds.F_nn_FlatModules.AddF_nn_FlatModulesRow(flatRow, moduleRow, 0, module.LocationPoint, module.Direction);           
         }
         return flatModuleRow;
      }

      private DataSet.F_nn_Elements_FlatModulesRow getElemInFM(DataSet ds, DataSet.F_nn_FlatModulesRow flatModuleRow, DataSet.F_S_ElementsRow elemRow, Element elem)
      {
         var elemFMRow = ds.F_nn_Elements_FlatModules.SingleOrDefault(eFM => 
                              eFM.F_nn_FlatModulesRow == flatModuleRow &&
                              eFM.F_S_ElementsRow == elemRow &&
                              eFM.LOCATION_POINT.Equals(elem.LocationPoint, StringComparison.OrdinalIgnoreCase) &&
                              eFM.DIRECTION.Equals(elem.Direction, StringComparison.OrdinalIgnoreCase));
         if (elemFMRow == null)
         {
            elemFMRow = ds.F_nn_Elements_FlatModules.AddF_nn_Elements_FlatModulesRow(flatModuleRow, elemRow, elem.LocationPoint, elem.Direction);
         }
         return elemFMRow;
      }

      private void setElemParams(DataSet ds, DataSet.F_nn_Elements_FlatModulesRow elemFMRow, Element elem)
      {
         // параметиры у элемента для заполнения
         var catParamsView = ds.CategoryParameters.Where(cp => cp.NAME_RUS_CATEGORY.Equals(elem.TypeElement, StringComparison.OrdinalIgnoreCase));         

         foreach (var catParamView in catParamsView)
         {            
            var elemParam = elem.Parameters.Find(p => p.Name.Equals(catParamView.NAME_PARAMETER, StringComparison.OrdinalIgnoreCase));
            if (elemParam != null)
            {
               var elemParamRow = ds.F_nn_ElementParam_Value.SingleOrDefault(ep =>
                                       ep.ID_ELEMENT_IN_FM == elemFMRow.ID_ELEMENT_IN_FM &&
                                       ep.ID_CAT_PARAMETER == catParamView.ID_CAT_PARAMETER &&
                                       ep.PARAMETER_VALUE.Equals(elemParam.Value, StringComparison.OrdinalIgnoreCase));
               if (elemParamRow == null)
               {
                  var catParamRow = ds.F_nn_Category_Parameters.SingleOrDefault(cp => cp.ID_CAT_PARAMETER == catParamView.ID_CAT_PARAMETER);
                  ds.F_nn_ElementParam_Value.AddF_nn_ElementParam_ValueRow(elemFMRow, catParamRow, elemParam.Value);                  
               }
            }            
         }         
      }

      /// <summary>
      /// Получение или создание строки таблицы Элементов по блоку Елемента
      /// </summary>      
      private DataSet.F_S_ElementsRow getElement(Element elem, DataSet ds)
      {
         var category = getCategory(elem, ds);
         var famInfo = getFamilyInfo(elem, ds);
         var elemRow = ds.F_S_Elements.SingleOrDefault(e => e.F_S_CategoriesRow == category &&
                                     e.F_S_FamilyInfosRow == famInfo);
         if (elemRow == null)
         {
            // Создание новой записи элемента
            elemRow = ds.F_S_Elements.AddF_S_ElementsRow(famInfo, category);
         }
         return elemRow;
      }      

      /// <summary>
      /// Определение категории блока элемента
      /// </summary>      
      private DataSet.F_S_CategoriesRow getCategory(Element elem, DataSet ds)
      {
         try
         {
            return ds.F_S_Categories.Single(c => c.NAME_RUS_CATEGORY.Equals(elem.TypeElement, StringComparison.OrdinalIgnoreCase));
         }
         catch (Exception ex)
         {
            Inspector.AddError($"Для элемента {elem.BlockName} не найдена категория в базе {elem.TypeElement} - {ex.Message}.",
               elem.ExtentsInModel, elem.IdBlRefElement, icon: System.Drawing.SystemIcons.Error);
            throw;                        
         }         
      }

      /// <summary>
      /// Получениа или создания семейства
      /// </summary>      
      private DataSet.F_S_FamilyInfosRow getFamilyInfo(Element elem, DataSet ds)
      {
         try
         {
            var famInfo = ds.F_S_FamilyInfos.SingleOrDefault(f =>
                                       f.FAMILY_NAME.Equals(elem.FamilyName.Name, StringComparison.OrdinalIgnoreCase) &&
                                       f.FAMILY_SYMBOL.Equals(elem.FamilySymbolName.Name, StringComparison.OrdinalIgnoreCase)
                                    );
            if (famInfo == null)
            {
               // Добавление записи семейства
               famInfo = ds.F_S_FamilyInfos.AddF_S_FamilyInfosRow(elem.FamilyName.Value, elem.FamilySymbolName.Value);
            }
            return famInfo;
         }
         catch(Exception ex)
         {
            Inspector.AddError($"Для элемента {elem.BlockName} не найдено семейство в базе: '{elem.FamilyName}' - '{elem.FamilySymbolName}'. {ex.Message}.",
               elem.ExtentsInModel, elem.IdBlRefElement, icon: System.Drawing.SystemIcons.Error);
            throw;
         }         
      }
   }
}
